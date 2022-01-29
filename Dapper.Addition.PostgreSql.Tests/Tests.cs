using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper.Addition.Shared.Tests;
using Xunit;

namespace Dapper.Addition.PostgreSql.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class Tests: TestBase
    {
        private readonly IDbExecutor _db;

        public Tests(DatabaseFixture databaseFixture) => _db = databaseFixture.Db;

        [Fact]
        public async Task Posts_Success()
        {
            var date = new DateTime(2015, 1, 1);
            
            var sql = new Sql(@"
SELECT p.post_id, p.text, p.creation_date
FROM posts p
WHERE p.creation_date >= @date
ORDER BY p.post_id", new {date});

            var postInfos = await _db.QueryListAsync<PostInfo>(sql);
            
            Assert.Equal(2, postInfos.Count);

            Assert.NotEqual(default, postInfos[0].PostId);
            Assert.NotEqual(default, postInfos[1].PostId);
            
            Assert.Equal("Test1", postInfos[0].Text);
            Assert.Null(postInfos[1].Text);
            
            Assert.Equal(new DateTime(2021, 01, 14), postInfos[0].CreationDate);
            Assert.Equal(new DateTime(2021, 02, 15), postInfos[1].CreationDate);
        }
        
        [Fact]
        public async Task Posts_DynamicSql_Success()
        {
            var postInfos = await GetPosts(new DateTime(2015, 1, 1));
            
            Assert.Equal(2, postInfos.Count);
        }

        private Task<List<PostInfo>> GetPosts(DateTime? date)
        {
            var sql = new Sql(@"
SELECT p.post_id, p.text, p.creation_date
FROM posts p
WHERE 1 = 1");
            if (date.HasValue)
                sql.Append(@"
    AND p.creation_date >= @date", new {date});

            return _db.QueryListAsync<PostInfo>(sql);
        }
        
        [Fact]
        public async Task InsertUpdateDelete_Success()
        {
            int id;
            {
                var post = new Post {CreationDate = new DateTime(2014, 1, 1)};
                FillPost(post, new PostData {Text = "Test"});
                id = await _db.InsertAsync<int>(post);
                Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT text FROM posts WHERE post_id = @id", new {id})));
            }
            {
                var post = await _db.GetByKeyAsync<Post>(new {PostId = id});
                FillPost(post, new PostData {Text = "Test2"});
                await _db.UpdateAsync(post);
                Assert.Equal("Test2", await _db.QuerySingleAsync<string>(new Sql("SELECT text FROM posts WHERE post_id = @id", new {id})));
            }
            {
                var rowCount = await _db.DeleteAsync<Post>(new {PostId = id});
                Assert.Equal(1, rowCount);
                Assert.Empty(await _db.QueryListAsync<string>(new Sql("SELECT text FROM posts WHERE post_id = @id", new {id})));
            }
        }
        
        private static void FillPost(Post post, PostData postData)
        {
            post.Text = postData.Text;
        }
        
        [Fact]
        public async Task Subquery_Success()
        {
            var toDate = new DateTime(2050, 1, 1);

            var sql = new Sql();
            sql.Append($@"
SELECT p.post_id, p.text, p.creation_date
FROM ({Post(sql)}) p
WHERE p.creation_date <= @toDate
ORDER BY p.post_id", new {toDate});

            var postInfos = await _db.QueryListAsync<PostInfo>(sql);
            
            Assert.Equal(2, postInfos.Count);
        }

        private static Sql Post(Sql sql)
        {
            var fromDate = new DateTime(2015, 1, 1);
            
            return sql.Sql(@"
SELECT * 
FROM posts p
WHERE p.creation_date >= @fromDate
", new {fromDate});
        }
        
        [Fact]
        public async Task InsertUpdate_CustomTableName_Success()
        {
            int id;
            {
                var entity = new Comment{Text = "Test"};
                id = await _db.InsertAsync<int>(entity);
                Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM comment2s WHERE Id = @id", new {id})));
            }
            {
                var entity = await _db.GetByKeyAsync<Comment>(new {Id = id});
                entity.Text = "Test2";
                await _db.UpdateAsync(entity);
                Assert.Equal("Test2", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM comment2s WHERE Id = @id", new {id})));
            }
        }
        
        [Fact]
        public async Task Insert_Success()
        {
            var id = 5;
            var entity = new Table3 {Id = id, Text = "Test"};
            await _db.InsertAsync(entity);
            Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM table3s WHERE Id = @id", new {id})));

            var rowCount = await _db.DeleteAsync<Table3>(new {Id = id});
            Assert.Equal(1, rowCount);
        }
    }
}