using System;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace PlainSql.SqlServer.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class TransactionScopeFixture : IDisposable, IAsyncLifetime 
    {
        private readonly IDbExecutor _db;
        private readonly TransactionScope _transactionScope;
        public Transaction? Transaction { get; }
        
        public TransactionScopeFixture(DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;
            _transactionScope = TransactionScopeFactory.Create();
            Transaction = Transaction.Current;
        }
        
        public void Dispose() => _transactionScope.Dispose();
        
        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 2, Name = "Client2"});
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}