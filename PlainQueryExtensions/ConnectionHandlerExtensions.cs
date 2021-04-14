using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static partial class ConnectionHandlerExtensions
    {
        public static Task<IEnumerable<T>> QueryAsync<T>(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.QueryAsync<T>(query));
        }
        
        public static Task<T> QuerySingleAsync<T>(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.QuerySingleAsync<T>(query));
        }
        
        public static Task<T> QuerySingleOrDefaultAsync<T>(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.QuerySingleOrDefaultAsync<T>(query));
        }
        
        public static Task<T> QueryFirstAsync<T>(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.QueryFirstAsync<T>(query));
        }
        
        public static Task<T> QueryFirstOrDefaultAsync<T>(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.QueryFirstOrDefaultAsync<T>(query));
        }
        
        public static Task<int> ExecuteAsync(this IHandler<DbConnection> connectionHandler, Query query)
        {
            return connectionHandler.Handle(connection => connection.ExecuteAsync(query));
        }
    }
}