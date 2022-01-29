using System.Threading.Tasks;
using Dapper.Addition.Shared.Tests.SavedTransactionScopes;
using SavedTransactionScopes;
using Xunit;

namespace Dapper.Addition.SqlServer.Tests.SavedTransactionScopes
{
    [Collection(nameof(FixtureCollection))]
    public class SimpleTransactionScopeTests
    {
        private readonly IDbExecutor _db;
        private readonly ISavepointExecutor _savepointExecutor;

        public SimpleTransactionScopeTests(DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;
            _savepointExecutor = databaseFixture.SavepointExecutor;
        }
        
        [Fact]
        public async Task SavedScope_Success()
        {
            using (new LocalTransactionScope {SavepointExecutor = _savepointExecutor}) //scope1
            {
                await _db.InsertAsync(new Client {Id = 2, Name = "Client"});

                using (new LocalTransactionScope()) //scope2
                {
                    {
                        var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                        Assert.Equal("Client", name);
                    }

                    await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Client.v2", Id = 2 });

                    {
                        var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                        Assert.Equal("Client.v2", name);
                    }
                } //rollback scope2

                {
                    var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                    Assert.Equal("Client", name);
                }
                
                using (var scope3 = new LocalTransactionScope()) //scope3
                {
                    {
                        var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                        Assert.Equal("Client", name);
                    }

                    await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Client.v3", Id = 2 });

                    {
                        var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                        Assert.Equal("Client.v3", name);
                    }
                    
                    scope3.Complete();
                }
                
                {
                    var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                    Assert.Equal("Client.v3", name);
                }
            }
        }
    }
}