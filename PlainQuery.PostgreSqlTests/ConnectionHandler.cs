using System;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;

namespace PlainQuery.PostgreSqlTests
{
    public class ConnectionHandler : IHandler<DbConnection>
    {
        public ConnectionHandler(string connectionString) => ConnectionString = connectionString;

        public async Task<TResult> Handle<TResult>(Func<DbConnection, Task<TResult>> func)
        {
            await using (var connection = new NpgsqlConnection(ConnectionString))
                return await func(connection);
        }

        public string ConnectionString { get; }
    }
}