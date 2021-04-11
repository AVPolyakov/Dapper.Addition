using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace PlainQueryExtensions
{
    internal static class DbConnectionExtensions
    {
        public static async Task OpenIfClosed(this DbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
                await connection.OpenAsync();
        }
    }
}