using System;
using System.Threading.Tasks;
using PlainQuery;
using Xunit;

namespace PlainQueryExtensions.Tests
{
    [Collection(nameof(DatabaseCollection))]
    public class MappingCheckTests
    {
        private static ConnectionProvider Db => DatabaseFixture.Db;

        public MappingCheckTests() => global::PlainQueryExtensions.QueryExtensions.MappingCheckEnabled = true;
        
        [Fact]
        public async Task EmptyDestinationType_ExceptionThrown()
        {
            var query = new Query("SELECT p.PostId, p.Text, p.CreationDate FROM Post p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => query.ToList<PostInfo2>(Db));

            Assert.Equal(@"Count of fields does not match. Query has 3 fields. Destination type has 0 fields. You can copy list of properties to destination type:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_ExceptionThrown()
        {
            var query = new Query("SELECT p.PostId, p.Text, p.CreationDate FROM Post p");

            var exception = await Assert.ThrowsAsync<Exception>(
                () => query.ToList<PostInfo3>(Db));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` with AllowDbNull='False' in query. You can copy list of properties to destination type:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }
        
        [Fact]
        public async Task FieldTypeMismatch_Insert_ExceptionThrown()
        {
            var post = new Namaspace2.Post {CreationDate = new DateTime(2014, 1, 1)};

            var exception = await Assert.ThrowsAsync<Exception>(
                () => Db.Insert<long>(post));

            Assert.Equal(@"Type of field 'PostId' does not match. Field type is 'long' in destination and `int` with AllowDbNull='False' in query. You can copy list of properties to destination type:
        public int PostId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }",
                exception.Message);
        }        
    }
}