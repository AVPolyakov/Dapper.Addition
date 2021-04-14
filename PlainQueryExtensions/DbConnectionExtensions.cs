using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using PlainQuery;
using Dapper;

namespace PlainQueryExtensions
{
    public static partial class DbConnectionExtensions
    {
        public static async Task<IEnumerable<T>> QueryAsync<T>(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await CheckMapping<T>(cnn, query);

            return await cnn.QueryAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }
        
        public static async Task<T> QuerySingleAsync<T>(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await CheckMapping<T>(cnn, query);

            return await cnn.QuerySingleAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }
        
        public static async Task<T> QuerySingleOrDefaultAsync<T>(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await CheckMapping<T>(cnn, query);

            return await cnn.QuerySingleOrDefaultAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }

        public static async Task<T> QueryFirstAsync<T>(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await CheckMapping<T>(cnn, query);

            return await cnn.QueryFirstAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }
        
        public static async Task<T> QueryFirstOrDefaultAsync<T>(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            await CheckMapping<T>(cnn, query);

            return await cnn.QueryFirstOrDefaultAsync<T>(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }
        
        public static Task<int> ExecuteAsync(
            this DbConnection cnn,
            Query query,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            return cnn.ExecuteAsync(query.StringBuilder.ToString(), query.GetDynamicParameters(), transaction, commandTimeout, commandType);
        }
        
        private static DynamicParameters GetDynamicParameters(this Query query)
        {
            var dynamicParameters = new DynamicParameters();
            foreach (var parameter in query.Parameters)
                dynamicParameters.AddDynamicParams(parameter);
            return dynamicParameters;
        }
    }
}