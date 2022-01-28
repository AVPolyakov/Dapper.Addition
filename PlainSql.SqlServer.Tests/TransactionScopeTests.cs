using System;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace PlainSql.SqlServer.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class TransactionScopeTests: IClassFixture<TransactionScopeFixture>, IDisposable
    {
        private readonly IDbExecutor _db;
        private readonly TransactionScope _transactionScope;
        
        public TransactionScopeTests(TransactionScopeFixture fixture,
            DatabaseFixture databaseFixture)
        {
            Transaction.Current = fixture.Transaction;
            _transactionScope = TransactionScopeFactory.Create();
            _db = databaseFixture.Db;
        }
        
        public void Dispose() => _transactionScope.Dispose();

        [Fact]
        public async Task UpdateToV2_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                Assert.Equal("Client2", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Client2v2", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                Assert.Equal("Client2v2", name);
            }
        }

        [Fact]
        public async Task UpdateToV3_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                Assert.Equal("Client2", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Client2v3", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @id", new { id = 2 });
                Assert.Equal("Client2v3", name);
            }
        }
    }
}