﻿using Hangfire.Server;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteStorage : JobStorage, IDisposable
    {
        private readonly string _databasePath;

        private readonly SQLiteDbConnectionFactory _dbConnectionFactory;

        private readonly SQLiteStorageOptions _storageOptions;

        private readonly Dictionary<string, bool> _features = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "Storage.ExtendedApi", false },
            { "Job.Queue", true },
            { "Connection.GetUtcDateTime", false },
            { "Connection.BatchedGetFirstByLowestScoreFromSet", false },
            { "Connection.GetSetContains", true },
            { "Connection.GetSetCount.Limited", false },
            { "BatchedGetFirstByLowestScoreFromSet", false },
            { "Transaction.AcquireDistributedLock", true },
            { "Transaction.CreateJob", true },
            { "Transaction.SetJobParameter", true },
            { "TransactionalAcknowledge:InMemoryFetchedJob", false },
            { "Monitoring.DeletedStateGraphs", false },
            { "Monitoring.AwaitingJobs", false }
        };

        private ConcurrentQueue<PooledHangfireDbContext> _dbContextPool = new ConcurrentQueue<PooledHangfireDbContext>();

        /// <summary>
        /// Queue providers collection
        /// </summary>
        public PersistentJobQueueProviderCollection QueueProviders { get; }

        /// <summary>
        /// Constructs Job Storage by database connection string
        /// </summary>
        /// <param name="databasePath">SQLite connection string</param>
        public SQLiteStorage(string databasePath)
            : this(databasePath, new SQLiteStorageOptions())
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="databasePath">SQLite connection string</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(string databasePath, SQLiteStorageOptions storageOptions)
            : this(new SQLiteDbConnectionFactory(() => new HfSqliteConnection(databasePath) { DefaultTimeout = 10 }), storageOptions)
        {
        }

        /// <summary>
        /// Constructs Job Storage by database connection string and options
        /// </summary>
        /// <param name="dbConnectionFactory">Factory that creates SQLite connections</param>
        /// <param name="storageOptions">Storage options</param>
        public SQLiteStorage(SQLiteDbConnectionFactory dbConnectionFactory, SQLiteStorageOptions storageOptions)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));

            var defaultQueueProvider = new SQLiteJobQueueProvider(_storageOptions);
            QueueProviders = new PersistentJobQueueProviderCollection(defaultQueueProvider);

            using (var dbContext = CreateAndOpenConnection())
            {
                _databasePath = dbContext.Database.Database;
                // Use this to initialize the database as soon as possible
                // in case of error, the user will immediately get an exception at startup
            }
        }

        public override IStorageConnection GetConnection()
        {
            CheckDisposed();
            var dbContext = CreateAndOpenConnection();
            return new HangfireSQLiteConnection(dbContext, _storageOptions, QueueProviders);
        }

        public override IMonitoringApi GetMonitoringApi()
        {
            CheckDisposed();
            return new SQLiteMonitoringApi(this, QueueProviders);
        }

        /// <summary>
        /// Opens connection to database
        /// </summary>
        /// <returns>Database context</returns>
        public HangfireDbContext CreateAndOpenConnection()
        {
            CheckDisposed();
            if (_dbContextPool.TryDequeue(out var dbContext))
            {
                return dbContext;
            }

            dbContext = new PooledHangfireDbContext(_dbConnectionFactory.Create(), ctx => EnqueueOrPhaseOut(ctx), _storageOptions.Prefix);
            dbContext.Init(_storageOptions);
            return dbContext;
        }

        private void EnqueueOrPhaseOut(PooledHangfireDbContext dbContext)
        {
            if (_disposed)
            {
                dbContext.PhaseOut = true;
                return;
            }

            if (_dbContextPool.Count < _storageOptions.PoolSize)
            {
                _dbContextPool.Enqueue(dbContext);
            }
            else
            {
                dbContext.PhaseOut = true;
            }
        }

        public override bool HasFeature(string featureId)
        {
            if (featureId == null) throw new ArgumentNullException(nameof(featureId));

            return _features.TryGetValue(featureId, out var isSupported)
                ? isSupported
                : base.HasFeature(featureId);
        }

        /// <summary>
        /// Returns text representation of the object
        /// </summary>
        public override string ToString()
        {
            return $"Database path: {_databasePath},  prefix: {_storageOptions.Prefix}";
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var dbContext in _dbContextPool)
            {
                dbContext.PhaseOut = true;
                dbContext.Dispose();
            }

            _dbContextPool = null;
            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                // This pattern is used to keep the IL-small and the cost for CheckDisposed low
                // by not having a throw new... statement inside the check function itself
                ThrowObjectDisposedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(SQLiteStorage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
#pragma warning disable 618
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this, _storageOptions.JobExpirationCheckInterval);
            yield return new CountersAggregator(this, _storageOptions.CountersAggregateInterval);
        }
    }
}