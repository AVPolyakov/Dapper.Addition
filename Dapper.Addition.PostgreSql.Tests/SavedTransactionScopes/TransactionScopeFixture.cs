using System;
using System.Threading.Tasks;
using Dapper.Addition.Shared.Tests.SavedTransactionScopes;
using SavedTransactionScopes;
using Xunit;

namespace Dapper.Addition.PostgreSql.Tests.SavedTransactionScopes
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
            
            _transactionScope = new LocalTransactionScope {SavepointExecutor = databaseFixture.SavepointExecutor};
            TransactionAmbientData = TransactionAmbientData.Current;
        }

        public void Dispose() => _transactionScope.Dispose();
        
        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 2, Name = "Client"});
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}