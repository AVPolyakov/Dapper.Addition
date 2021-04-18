using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace PlainQuery.SqlServerTests
{
    public class DbExecutor : IDbExecutor<DbConnection>
    {
        public DbExecutor(string connectionString) => ConnectionString = connectionString;

        public async Task<TResult> ExecAsync<TResult>(Func<DbConnection, Task<TResult>> func)
        {
            await using (var connection = new SqlConnection(ConnectionString))
                return await func(connection);
        }

        public string ConnectionString { get; }
    }
}