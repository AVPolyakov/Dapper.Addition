using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static class QueryExtensions
    {
        public static async Task<List<T>> ToList<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            var enumerable = await connectionHandler.QueryAsync<T>(query);
            return enumerable.AsList();
        }
        
        public static async Task<T[]> ToArray<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            var enumerable = await connectionHandler.QueryAsync<T>(query);
            return enumerable.ToArray();
        }
        
        public static Task<T> Single<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.QuerySingleAsync<T>(query);
        }
        
        public static Task<T> SingleOrDefault<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.QuerySingleOrDefaultAsync<T>(query);
        }
        
        public static Task<T> First<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.QueryFirstAsync<T>(query);
        }
        
        public static Task<T> FirstOrDefault<T>(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.QueryFirstOrDefaultAsync<T>(query);
        }
        
        public static Task<int> Execute(this Query query, IHandler<DbConnection> connectionHandler)
        {
            return connectionHandler.ExecuteAsync(query);
        }
    }
}