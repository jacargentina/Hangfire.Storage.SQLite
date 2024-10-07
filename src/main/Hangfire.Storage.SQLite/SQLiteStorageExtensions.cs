﻿using Hangfire.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hangfire.Storage.SQLite
{
    /// <summary>
    /// 
    /// </summary>
    public static class SQLiteStorageExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IGlobalConfiguration<SQLiteStorage> UseSQLiteStorage(
            [Hangfire.Annotations.NotNull] this IGlobalConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            
            var storage = new SQLiteStorage("Hangfire.db", new SQLiteStorageOptions());
            
            return configuration.UseStorage(storage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="nameOrConnectionString"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IGlobalConfiguration<SQLiteStorage> UseSQLiteStorage(
            [Hangfire.Annotations.NotNull] this IGlobalConfiguration configuration,
            [Hangfire.Annotations.NotNull] string nameOrConnectionString,
            SQLiteStorageOptions options = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (nameOrConnectionString == null) throw new ArgumentNullException(nameof(nameOrConnectionString));
            if (options == null) options = new SQLiteStorageOptions();
            
            var storage = new SQLiteStorage(nameOrConnectionString, options);
            
            return configuration.UseStorage(storage);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="connectionFactory">connection factory to use</param>
        /// <param name="options"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IGlobalConfiguration<SQLiteStorage> UseSQLiteStorage(
            [Hangfire.Annotations.NotNull] this IGlobalConfiguration configuration,
            [Hangfire.Annotations.NotNull] SQLiteDbConnectionFactory connectionFactory,
            SQLiteStorageOptions options = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (connectionFactory == null) throw new ArgumentNullException(nameof(connectionFactory));
            if (options == null) options = new SQLiteStorageOptions();

            var storage = new SQLiteStorage(connectionFactory, options);

            return configuration.UseStorage(storage);
        }
    }
}
