using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace PlainQuery
{
    public static partial class QueryExtensions
    {
        public static async Task<List<T>> ToList<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            var enumerable = await connection.QueryAsync<T>(query.ToString(), query.GetDynamicParameters());
            return enumerable.AsList();
        }
        
        public static async Task<T[]> ToArray<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            var enumerable = await connection.QueryAsync<T>(query.ToString(), query.GetDynamicParameters());
            return enumerable.ToArray();
        }
        
        public static async Task<T> Single<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QuerySingleAsync<T>(query.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> SingleOrDefault<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QuerySingleOrDefaultAsync<T>(query.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> First<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QueryFirstAsync<T>(query.ToString(), query.GetDynamicParameters());
        }
        
        public static async Task<T> FirstOrDefault<T>(this Query query, DbConnection connection)
        {
            await query.CheckMapping<T>(connection);
            
            return await connection.QueryFirstOrDefaultAsync<T>(query.ToString(), query.GetDynamicParameters());
        }
        
        public static Task<int> Execute(this Query query, DbConnection connection)
        {
            return connection.ExecuteAsync(query.ToString(), query.GetDynamicParameters());
        }
        
        public static Task<List<T>> ToList<T>(this Query query, IDbExecutor<DbConnection> executor) 
            => executor.ExecAsync(query.ToList<T>);

        public static Task<T[]> ToArray<T>(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.ToArray<T>);
        
        public static Task<T> Single<T>(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.Single<T>);
        
        public static Task<T> SingleOrDefault<T>(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.SingleOrDefault<T>);
        
        public static Task<T> First<T>(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.First<T>);
        
        public static Task<T> FirstOrDefault<T>(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.FirstOrDefault<T>);
        
        public static Task<int> Execute(this Query query, IDbExecutor<DbConnection> executor)
            => executor.ExecAsync(query.Execute);

        public static DynamicParameters GetDynamicParameters(this Query query)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in query.Parameters)
                dynamicParameters.AddDynamicParams(parameter);
            return dynamicParameters;
        }
    }
}