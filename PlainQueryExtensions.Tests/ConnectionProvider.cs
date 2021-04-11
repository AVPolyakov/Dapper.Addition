using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace PlainQueryExtensions.Tests
{
    public class ConnectionProvider : IConnectionProvider
    {
        public ConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public Task<DbConnection> GetConnection() => Task.FromResult<DbConnection>(new SqlConnection(ConnectionString));

        public string ConnectionString { get; }
    }
}