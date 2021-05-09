using System.Data;
using System.Threading.Tasks;

namespace PlainSql
{
    public static partial class DbExecutorExtensions
    {
        public static Task<TKey> Insert<TKey>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Insert<TKey>(param));
        
        public static Task<int> Insert(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Insert(param));
        
        public static Task<int> Update(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Update(param));
        
        public static Task<int> Delete<T>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.Delete<T>(param));
        
        public static Task<T> GetByKey<T>(this IDbExecutor<IDbConnection> executor, object param) 
            => executor.ExecAsync(connection => connection.GetByKey<T>(param));
    }
}