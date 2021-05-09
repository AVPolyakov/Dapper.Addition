using System.Data;
using System.Threading.Tasks;

namespace PlainSql
{
    public static partial class DbExecutorExtensions
    {
        public static Task<TKey> InsertAsync<TKey>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecuteAsync(connection => connection.InsertAsync<TKey>(param));
        
        public static Task<int> InsertAsync(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecuteAsync(connection => connection.InsertAsync(param));
        
        public static Task<int> UpdateAsync(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecuteAsync(connection => connection.UpdateAsync(param));
        
        public static Task<int> DeleteAsync<T>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecuteAsync(connection => connection.DeleteAsync<T>(param));
        
        public static Task<T> GetByKeyAsync<T>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecuteAsync(connection => connection.GetByKeyAsync<T>(param));
    }
}