using System.Collections.Immutable;
using System.Transactions;

namespace SavepointHandlers
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
            get => new(Transaction.Current, SavepointHandler.SavepointHandlers);
            set
            {
                Transaction.Current = value._transaction;
                SavepointHandler.SavepointHandlers = value._savepointHandlers;
            }
        }
    }
}