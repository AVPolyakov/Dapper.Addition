using System.Collections.Immutable;
using System.Transactions;

namespace SavedTransactionScopes
{
    public class TransactionAmbientData
    {
        private readonly Transaction? _transaction;
        private readonly ImmutableStack<SavepointHandler>? _savepointHandlers;

        private TransactionAmbientData(Transaction? transaction, ImmutableStack<SavepointHandler>? savepointHandlers)
        {
            _transaction = transaction;
            _savepointHandlers = savepointHandlers;
        }

        public static TransactionAmbientData Current
        {
            get => new(Transaction.Current, SavepointHandler.SavepointHandlers.Value);
            set
            {
                Transaction.Current = value._transaction;
                SavepointHandler.SavepointHandlers.Value = value._savepointHandlers!;
            }
        }
    }
}