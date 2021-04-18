using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PlainQuery;

namespace PlainQueryExtensions
{
    public static partial class QueryExtensions
    {
        public static async Task<List<T>> ToList<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            var enumerable = await connection.QueryAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
            return enumerable.AsList();
        }
        
        public static async Task<T[]> ToArray<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            var enumerable = await connection.QueryAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
            return enumerable.ToArray();
        }
        
        public static async Task<T> Single<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QuerySingleAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> SingleOrDefault<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QuerySingleOrDefaultAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> First<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QueryFirstAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> FirstOrDefault<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QueryFirstOrDefaultAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters());
        }
        
        public static Task<int> Execute(this Query query, DbConnection connection)
        {
            return connection.ExecuteAsync(query.StringBuilder.ToString(), query.GetDynamicParameters());
        }
        
        public static Task<List<T>> ToList<T>(this Query query, IHandler<DbConnection> connectionHandler) 
            => connectionHandler.Handle(query.ToList<T>);

        public static Task<T[]> ToArray<T>(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.ToArray<T>);
        
        public static Task<T> Single<T>(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.Single<T>);
        
        public static Task<T> SingleOrDefault<T>(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.SingleOrDefault<T>);
        
        public static Task<T> First<T>(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.First<T>);
        
        public static Task<T> FirstOrDefault<T>(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.FirstOrDefault<T>);
        
        public static Task<int> Execute(this Query query, IHandler<DbConnection> connectionHandler)
            => connectionHandler.Handle(query.Execute);
        
        private static DynamicParameters GetDynamicParameters(this Query query)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in query.Parameters)
                dynamicParameters.AddDynamicParams(parameter);
            return dynamicParameters;
        }
    }
}