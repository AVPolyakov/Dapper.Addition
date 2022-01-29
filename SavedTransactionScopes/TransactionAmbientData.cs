using System.Collections.Immutable;
using System.Transactions;

namespace SavedTransactionScopes
{
    public class TransactionAmbientData
    {
        private readonly Transaction? _transaction;
        private readonly ImmutableStack<LocalTransactionScope>? _localTransactionScopes;

        private TransactionAmbientData(Transaction? transaction, ImmutableStack<LocalTransactionScope>? localTransactionScopes)
        {
            _transaction = transaction;
            _localTransactionScopes = localTransactionScopes;
        }

        public static TransactionAmbientData Current
        {
            get => new(Transaction.Current, LocalTransactionScope.LocalTransactionScopes.Value);
            set
            {
                Transaction.Current = value._transaction;
                LocalTransactionScope.LocalTransactionScopes.Value = value._localTransactionScopes!;
            }
        }
    }
}