using System;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace Hangfire.Storage.SQLite.Test.Utils
{
    public static class ConnectionUtils
    {
        public static SQLiteStorage CreateStorage()
        {
            var storageOptions = new SQLiteStorageOptions();

            return CreateStorage(storageOptions);
        }

        public static SQLiteStorage CreateStorage(SQLiteStorageOptions storageOptions)
        {
            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = $"hangfire_{Guid.NewGuid():n}",
                Cache = SqliteCacheMode.Shared
            }.ToString();
            return new SQLiteStorage(new SQLiteDbConnectionFactory(() =>
                    new HfSqliteConnection(connectionString)),
                storageOptions);
        }

        /// <summary>
        /// Only use this if you have a single thread.
        /// For multi-threaded tests, use <see cref="CreateStorage()"/> directly and
        /// then call <see cref="SQLiteStorage.CreateAndOpenConnection"/> per Thread.
        /// </summary>
        public static HangfireDbContext CreateConnection()
        {
            return CreateStorage().CreateAndOpenConnection();
        }
    }
}