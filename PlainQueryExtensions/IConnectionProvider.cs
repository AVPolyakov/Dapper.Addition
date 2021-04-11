using System.Data.Common;
using System.Threading.Tasks;

namespace PlainQueryExtensions
{
    public interface IConnectionProvider
    {
        Task<DbConnection> GetConnection();
        string ConnectionString { get; }
    }
}