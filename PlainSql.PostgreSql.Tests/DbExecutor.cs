using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;

namespace PlainSql.PostgreSql.Tests
{
    public class DbExecutor : IDbExecutor
    {
        private readonly string _connectionString;

        public DbExecutor(string connectionString) => _connectionString = connectionString;

        public async Task<TResult> ExecuteAsync<TResult>(Func<IDbConnection, Task<TResult>> func)
        {
            await using (var connection = new NpgsqlConnection(_connectionString))
                return await func(connection);
        }        
    }
}