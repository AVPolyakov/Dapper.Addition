using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace PlainSql.SqlServer.Tests
{
    public class DbExecutor : IDbExecutor
    {
        private readonly string _connectionString;

        public DbExecutor(string connectionString) => _connectionString = connectionString;

        public async Task<TResult> ExecAsync<TResult>(Func<IDbConnection, Task<TResult>> func)
        {
            await using (var connection = new SqlConnection(_connectionString))
                return await func(connection);
        }        
    }
}