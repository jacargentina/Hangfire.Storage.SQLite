using System;

namespace Hangfire.Storage.SQLite
{
    public class SQLiteDbConnectionFactory
    {
        private readonly Func<HfSqliteConnection> _getConnection;

        public SQLiteDbConnectionFactory(Func<HfSqliteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public HfSqliteConnection Create()
        {
            return _getConnection();
        }
    }
}