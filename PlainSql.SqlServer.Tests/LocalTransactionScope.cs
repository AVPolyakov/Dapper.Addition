using System;
using System.Transactions;

namespace PlainSql.SqlServer.Tests
{
    public class LocalTransactionScope: IDisposable
    {
        private readonly TransactionScope _transactionScope;
        
        public LocalTransactionScope()
        {
            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _transactionScope.Dispose();
    }
}