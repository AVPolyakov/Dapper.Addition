using System;
using System.Data;
using Npgsql;
using SavedTransactionScopes;

namespace Dapper.Addition.PostgreSql.Tests.SavedTransactionScopes
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
            using (var connection = new NpgsqlConnection(_connectionString))
                return func(connection);
        }
    }
}