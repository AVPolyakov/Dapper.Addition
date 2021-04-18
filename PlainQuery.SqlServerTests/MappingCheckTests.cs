using System;
using System.Threading.Tasks;
using Xunit;

namespace PlainQuery.SqlServerTests
{
    [Collection(nameof(DatabaseCollection))]
    public class MappingCheckTests
    {
        private static ConnectionHandler Db => DatabaseFixture.Db;

        public MappingCheckTests() => MappingCheckSettings.MappingCheckEnabled = true;

        [Fact]
        public async Task EmptyDestinationType_ExceptionThrown()
        {
            var query = new Query("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => query.ToList<PostInfo2>(Db));

            Assert.Equal(@"Property 'PostId' not found in destination type. You can copy list of properties to destination type PlainQuery.SqlServerTests.PostInfo2:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_ExceptionThrown()
        {
            var query = new Query("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => query.ToList<PostInfo3>(Db));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` in query. You can copy list of properties to destination type PlainQuery.SqlServerTests.PostInfo3:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_Nullable_ExceptionThrown()
        {
            var query = new Query("SELECT * FROM Table5s");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => query.ToList<Table5Info>(Db));

            Assert.Equal(@"Type of field 'CreationDate' does not match. Field type is 'DateTime' in destination and `DateTime?` in query. You can copy list of properties to destination type PlainQuery.SqlServerTests.Table5Info:
        public int Id { get; set; }
        public DateTime? CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task IgnoredProperty_Success()
        {
            var query = new Query("SELECT p.PostId, p.Text, p.CreationDate FROM Posts p");

            var list = await query.ToList<PostInfo4>(Db);
            
            Assert.NotNull(list);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_Insert_ExceptionThrown()
        {
            var post = new Namaspace2.Post {CreationDate = new DateTime(2014, 1, 1)};

            var exception = await Assert.ThrowsAsync<Exception>(
                () => Db.Insert<long>(post));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` in query. You can copy list of properties to destination type PlainQuery.SqlServerTests.Namaspace2.Post:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }        
    }
}