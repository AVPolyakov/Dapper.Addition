using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlainQuery;
using Xunit;

namespace PlainQueryExtensions.Tests
{
    [Collection(nameof(DatabaseCollection))]
    public class Tests
    {
        private static ConnectionHandler Db => DatabaseFixture.Db;

        public Tests() => MappingCheckSettings.MappingCheckEnabled = true;

        [Fact]
        public async Task Posts_Success()
        {
            var date = new DateTime(2015, 1, 1);
            
            var query = new Query(@"
SELECT p.PostId, p.Text, p.CreationDate
FROM Post p
WHERE p.CreationDate >= @date
ORDER BY p.PostId", new {date});

            var postInfos = await query.ToList<PostInfo>(Db);
            
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
            {
                var postInfos = await GetPosts(new DateTime(2015, 1, 1));
                Assert.Equal(2, postInfos.Count);
            }
            {
                var postInfos = await GetPosts(new DateTime(3015, 1, 1));
                Assert.Empty(postInfos);
            }
        }

        private static Task<List<PostInfo>> GetPosts(DateTime? date)
        {
            var query = new Query(@"
SELECT p.PostId, p.Text, p.CreationDate
FROM Post p
WHERE 1 = 1");
            if (date.HasValue)
                query.Append(@"
    AND p.CreationDate >= @date", new {date});

            return query.ToList<PostInfo>(Db);
        }
        
        [Fact]
        public async Task InsertUpdateDelete_Success()
        {
            int id;
            {
                var post = new Post {CreationDate = new DateTime(2014, 1, 1)};
                FillPost(post, new PostData {Text = "Test"});
                id = await Db.Insert<int>(post);
                Assert.Equal("Test", await new Query("SELECT Text FROM Post WHERE PostId = @id", new {id}).Single<string>(Db));
            }
            {
                var post = await Db.GetByKey<Post>(new {PostId = id});
                FillPost(post, new PostData {Text = "Test2"});
                await Db.Update(post);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Post WHERE PostId = @id", new {id}).Single<string>(Db));
            }
            {
                var rowCount = await Db.Delete<Post>(new {PostId = id});
                Assert.Equal(1, rowCount);
                Assert.Empty(await new Query("SELECT Text FROM Post WHERE PostId = @id", new {id}).ToList<string>(Db));
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

            var query = new Query();
            query.Append($@"
SELECT p.PostId, p.Text, p.CreationDate
FROM ({Post(query)}) p
WHERE p.CreationDate <= @toDate
ORDER BY p.PostId", new {toDate});

            var postInfos = await query.ToList<PostInfo>(Db);
            
            Assert.Equal(2, postInfos.Count);
        }

        private static Query Post(Query query)
        {
            var fromDate = new DateTime(2015, 1, 1);
            
            return query.Query(@"
SELECT * 
FROM Post p
WHERE p.CreationDate >= @fromDate
", new {fromDate});
        }

        [Fact]
        public async Task InsertUpdate_ComputedColumn1_Success()
        {
            int id;
            {
                var entity = new Table2{Text = "Test"};
                id = await Db.Insert<int>(entity);
                Assert.Equal("Test", await new Query("SELECT Text FROM Table2 WHERE Id = @id", new {id}).Single<string>(Db));
            }
            {
                var entity = await Db.GetByKey<Table2>(new {Id = id});  
                Assert.Equal(1, entity.ComputedColumn1);
                entity.Text = "Test2";
                await Db.Update(entity);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Table2 WHERE Id = @id", new {id}).Single<string>(Db));
            }
        }
        
        [Fact]
        public async Task InsertUpdate_CustomTableName_Success()
        {
            int id;
            {
                var entity = new Comment{Text = "Test"};
                id = await Db.Insert<int>(entity);
                Assert.Equal("Test", await new Query("SELECT Text FROM Comments WHERE Id = @id", new {id}).Single<string>(Db));
            }
            {
                var entity = await Db.GetByKey<Comment>(new {Id = id});
                entity.Text = "Test2";
                await Db.Update(entity);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Comments WHERE Id = @id", new {id}).Single<string>(Db));
            }
        }
    }
}