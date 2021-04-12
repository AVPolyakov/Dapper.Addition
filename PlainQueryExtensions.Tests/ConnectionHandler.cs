using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace PlainQueryExtensions.Tests
{
    public class ConnectionHandler : IHandler<DbConnection>
    {
        public ConnectionHandler(string connectionString) => ConnectionString = connectionString;

        public async Task<TResult> Handle<TResult>(Func<DbConnection, Task<TResult>> func)
        {
            await using (var connection = new SqlConnection(ConnectionString))
                return await func(connection);
        }

        public string ConnectionString { get; }
    }
}