using System;
using System.Threading.Tasks;
using Xunit;

namespace PlainSql.SqlServer.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class MappingCheckTests
    {
        private readonly IDbExecutor _db;

        public MappingCheckTests(DatabaseFixture databaseFixture) => _db = databaseFixture.Db;
        
        [Fact]
        public async Task EmptyDestinationType_ExceptionThrown()
        {
            var sql = new Sql("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => _db.QueryListAsync<PostInfo2>(sql));

            Assert.Equal(@"Property 'PostId' not found in destination type. You can copy list of properties to destination type PlainSql.SqlServer.Tests.PostInfo2:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_ExceptionThrown()
        {
            var sql = new Sql("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => _db.QueryListAsync<PostInfo3>(sql));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` in query. You can copy list of properties to destination type PlainSql.SqlServer.Tests.PostInfo3:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_Nullable_ExceptionThrown()
        {
            var sql = new Sql("SELECT * FROM Table5s");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => _db.QueryListAsync<Table5Info>(sql));

            Assert.Equal(@"Type of field 'CreationDate' does not match. Field type is 'DateTime' in destination and `DateTime?` in query. You can copy list of properties to destination type PlainSql.SqlServer.Tests.Table5Info:
        public int Id { get; set; }
        public DateTime? CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task IgnoredProperty_Success()
        {
            var sql = new Sql("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var list = await _db.QueryListAsync<PostInfo4>(sql);
            
            Assert.NotNull(list);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_Insert_ExceptionThrown()
        {
            var post = new Namaspace2.Post {CreationDate = new DateTime(2014, 1, 1)};

            var exception = await Assert.ThrowsAsync<Exception>(
                () => _db.InsertAsync<long>(post));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` in query. You can copy list of properties to destination type PlainSql.SqlServer.Tests.Namaspace2.Post:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }        
    }
}