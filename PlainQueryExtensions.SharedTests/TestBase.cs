using System;
using System.Transactions;

namespace PlainQueryExtensions.SharedTests
{
    public class TestBase: IDisposable
    {
        private readonly TransactionScope _transactionScope;

        public TestBase()
        {
            MappingCheckSettings.MappingCheckEnabled = true;
            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
        }

        public void Dispose() => _transactionScope?.Dispose();
    }
}