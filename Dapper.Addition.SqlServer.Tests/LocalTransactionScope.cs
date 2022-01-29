using System;
using System.Collections.Immutable;
using System.Data;
using System.Threading;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace Dapper.Addition.SqlServer.Tests
{
    public class LocalTransactionScope: IDisposable
    {
        internal static AsyncLocal<ImmutableStack<LocalTransactionScope>> LocalTransactionScopes { get; } = new();

        private readonly ISavepointExecutor? _savepointExecutor;
        private readonly SavepointInfo? _savepointInfo;
        private readonly TransactionScope _transactionScope;

        public LocalTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            ISavepointExecutor? savepointExecutor = null)
        {
            _savepointExecutor = savepointExecutor;
            
            var stack = LocalTransactionScopes.Value ?? ImmutableStack<LocalTransactionScope>.Empty;
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (!stack.IsEmpty)
                {
                    var parentTransactionScope = stack.Peek();
                    var executor = parentTransactionScope._savepointExecutor;
                    _savepointInfo = executor != null
                        ? new SavepointInfo(executor.Execute(SetSavepoint), executor)
                        : parentTransactionScope._savepointInfo;
                }
            }
            LocalTransactionScopes.Value = stack.Push(this);
            
            _transactionScope = new TransactionScope(
                scopeOption,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
        }
        
        public void Complete()
        {
            if (_savepointInfo != null)
                _savepointInfo.ScopeIsCompleted = true;
            
            _transactionScope.Complete();
        }

        public void Dispose()
        {
            var stack = LocalTransactionScopes.Value;
            if (stack != null && !stack.IsEmpty)
                LocalTransactionScopes.Value = stack.Pop();

            if (_savepointInfo != null)
            {
                if (!_savepointInfo.ScopeIsCompleted)
                {
                    if (!_savepointInfo.IsRollbacked)
                    {
                        _savepointInfo.Executor.Execute(connection => RollbackToSavepoint(connection, _savepointInfo.Name));
                        _savepointInfo.IsRollbacked = true;
                    }
                    _transactionScope.Complete();
                }
            }

            _transactionScope.Dispose();
        }

        private record SavepointInfo(string Name, ISavepointExecutor Executor)
        {
            public bool ScopeIsCompleted { get; set; }
            public bool IsRollbacked { get; set; }
        }

        private static string SetSavepoint(IDbConnection connection)
        {
            string savePointName;
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    savePointName = Guid.NewGuid().ToString("N");
                    command.CommandText = $"SAVE TRANSACTION @SavePointName";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@SavePointName";
                    parameter.Value = savePointName;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
            return savePointName;
        }

        private static void RollbackToSavepoint(IDbConnection connection, string savePointName)
        {
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"ROLLBACK TRANSACTION @SavePointName";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@SavePointName";
                    parameter.Value = savePointName;
                    command.Parameters.Add(parameter);
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }

        public static LocalTransactionScope CreateSaved(ISavepointExecutor savepointExecutor)
        {
            return new LocalTransactionScope(savepointExecutor: savepointExecutor);
        }
    }
}