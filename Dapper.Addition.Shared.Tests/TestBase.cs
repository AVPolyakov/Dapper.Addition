using System;
using System.Transactions;

namespace Dapper.Addition.Shared.Tests
{
    public class TestBase: IDisposable
    {
        private readonly TransactionScope _transactionScope;

        public TestBase()
        {
            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _transactionScope?.Dispose();
    }
}