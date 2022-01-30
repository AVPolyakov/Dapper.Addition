using System;
using System.Threading.Tasks;
using Dapper.Addition.Shared.Tests.SavedTransactionScopes;
using SavedTransactionScopes;
using Xunit;

namespace Dapper.Addition.SqlServer.Tests.SavedTransactionScopes
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
        
        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 2, Name = "Name1"});
        }

        public Task DisposeAsync() => Task.CompletedTask;
        
        public void Dispose() => _transactionScope.Dispose();
    }
    
    [Collection(nameof(FixtureCollection))]
    public class TransactionScopeTests: IClassFixture<TransactionScopeFixture>, IDisposable
    {
        private readonly IDbExecutor _db;
        private readonly LocalTransactionScope _transactionScope;
        
        public TransactionScopeTests(TransactionScopeFixture fixture,
            DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;

            TransactionAmbientData.Current = fixture.TransactionAmbientData;
            
            _transactionScope = new LocalTransactionScope {SavepointExecutor = databaseFixture.SavepointExecutor};
        }
        
        public void Dispose() => _transactionScope.Dispose();

        [Fact]
        public async Task UpdateToName2_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name2", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                Assert.Equal("Name2", name);
            }
        }

        [Fact]
        public async Task UpdateToName3_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name3", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                Assert.Equal("Name3", name);
            }
        }
    }
}