using System;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;

namespace PlainQuery.PostgreSqlTests
{
    public class DbExecutor : IDbExecutor<DbConnection>
    {
        public DbExecutor(string connectionString) => ConnectionString = connectionString;

        public async Task<TResult> ExecAsync<TResult>(Func<DbConnection, Task<TResult>> func)
        {
            await using (var connection = new NpgsqlConnection(ConnectionString))
                return await func(connection);
        }

        public string ConnectionString { get; }
    }
}