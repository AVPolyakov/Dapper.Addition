using System.Transactions;

namespace PlainSql.SqlServer.Tests
{
    public static class TransactionScopeFactory
    {
        public static TransactionScope Create()
        {
            return new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}