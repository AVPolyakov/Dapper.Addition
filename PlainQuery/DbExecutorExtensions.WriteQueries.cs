using System.Data.Common;
using System.Threading.Tasks;

namespace PlainQuery
{
    public static class DbExecutorExtensions
    {
        public static Task<TKey> Insert<TKey>(this IDbExecutor<DbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Insert<TKey>(param));
        
        public static Task<int> Insert(this IDbExecutor<DbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Insert(param));
        
        public static Task<int> Update(this IDbExecutor<DbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Update(param));
        
        public static Task<int> Delete<T>(this IDbExecutor<DbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Delete<T>(param));
        
        public static Task<T> GetByKey<T>(this IDbExecutor<DbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.GetByKey<T>(param));
    }
}