using System;
using System.Threading.Tasks;
using Xunit;

namespace PlainSql.SqlServer.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class TransactionScopeFixture : IDisposable, IAsyncLifetime 
    {
        private readonly IDbExecutor _db;
        private readonly LocalTransactionScope _transactionScope;
        public TransactionAmbientData TransactionAmbientData { get; }
        
        public TransactionScopeFixture(DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;
            
            _transactionScope = LocalTransactionScope.CreateSaved(databaseFixture.SavepointExecutor);
            TransactionAmbientData = TransactionAmbientData.Current;
        }

        public void Dispose() => _transactionScope.Dispose();
        
        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 2, Name = "Client2"});
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}