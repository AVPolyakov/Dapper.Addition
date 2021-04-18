using System.Data.Common;
using System.Threading.Tasks;

namespace PlainQueryExtensions
{
    public static class ConnectionHandlerExtensions
    {
        public static Task<TKey> Insert<TKey>(this IHandler<DbConnection> connectionHandler, object param) 
            => connectionHandler.Handle(connection => connection.Insert<TKey>(param));
        
        public static Task<int> Insert(this IHandler<DbConnection> connectionHandler, object param) 
            => connectionHandler.Handle(connection => connection.Insert(param));
        
        public static Task<int> Update(this IHandler<DbConnection> connectionHandler, object param) 
            => connectionHandler.Handle(connection => connection.Update(param));
        
        public static Task<int> Delete<T>(this IHandler<DbConnection> connectionHandler, object param) 
            => connectionHandler.Handle(connection => connection.Delete<T>(param));
        
        public static Task<T> GetByKey<T>(this IHandler<DbConnection> connectionHandler, object param) 
            => connectionHandler.Handle(connection => connection.GetByKey<T>(param));
    }
}