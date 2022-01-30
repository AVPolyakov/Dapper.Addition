using System;
using System.Data;
using System.Data.SqlClient;
using SavepointHandlers;

namespace Dapper.Addition.SqlServer.Tests.SavepointHandlers
{
    public class SavepointExecutor : ISavepointExecutor
    {
        private readonly string _connectionString;

        public SavepointExecutor(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public TResult Execute<TResult>(Func<IDbConnection, TResult> func)
        {
            using (var connection = new SqlConnection(_connectionString))
                return func(connection);
        }
    }
}