using System.Collections.Immutable;
using System.Data;
using System.Threading;
using System.Transactions;

namespace SavedTransactionScopes
{
    public class SavepointHandler
    {
        internal static AsyncLocal<ImmutableStack<SavepointHandler>> SavepointHandlers { get; } = new();

        private readonly SavepointInfo? _savepointInfo;

        public ISavepointExecutor? SavepointExecutor { private get; set; }
        
        public SavepointHandler(TransactionScopeOption scopeOption)
        {
            var stack = SavepointHandlers.Value ?? ImmutableStack<SavepointHandler>.Empty;
            
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (!stack.IsEmpty)
                {
                    var parent = stack.Peek();
                    var executor = parent.SavepointExecutor;
                    _savepointInfo = executor != null
                        ? new SavepointInfo(executor.Execute(SetSavepoint), executor)
                        : parent._savepointInfo;
                }
            }
            
            SavepointHandlers.Value = stack.Push(this);
        }
        
        public void Complete()
        {
            if (_savepointInfo != null)
                _savepointInfo.IsCompleted = true;
        }
        
        public void Dispose(TransactionScope transactionScope)
        {
            var stack = SavepointHandlers.Value;
            if (stack != null && !stack.IsEmpty)
                SavepointHandlers.Value = stack.Pop();

            if (_savepointInfo != null)
            {
                if (!_savepointInfo.IsCompleted)
                {
                    if (!_savepointInfo.IsRollbacked)
                    {
                        _savepointInfo.Executor.Execute(connection => RollbackToSavepoint(connection, _savepointInfo.Name));
                        _savepointInfo.IsRollbacked = true;
                    }
                    transactionScope.Complete();
                }
            }
        }
        
        private record SavepointInfo(string Name, ISavepointExecutor Executor)
        {
            public bool IsCompleted { get; set; }
            public bool IsRollbacked { get; set; }
        }
        
        private static string SetSavepoint(IDbConnection connection)
        {
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                    return ISavepointAdapter.Current.SetSavepoint(command);
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }

        private static void RollbackToSavepoint(IDbConnection connection, string savePointName)
        {
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand()) 
                    ISavepointAdapter.Current.RollbackToSavepoint(command, savePointName);
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }
    }
}