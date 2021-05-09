using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using PlainSql.Shared.Tests;
using Xunit;

namespace PlainSql.SqlServer.Tests
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
SELECT p.PostId, p.Text, p.CreationDate
FROM Posts p
WHERE p.CreationDate >= @date
ORDER BY p.PostId", new {date});

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
SELECT p.PostId, p.Text, p.CreationDate
FROM Posts p
WHERE 1 = 1");
            if (date.HasValue)
                sql.Append(@"
    AND p.CreationDate >= @date", new {date});

            return _db.QueryListAsync<PostInfo>(sql);
        }
        
        [Fact]
        public async Task InsertUpdateDelete_Success()
        {
            int id;
            {
                var post = new Post {CreationDate = new DateTime(2014, 1, 1)};
                FillPost(post, new PostData {Text = "Test"});
                id = await _db.Insert<int>(post);
                Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Posts WHERE PostId = @id", new {id})));
            }
            {
                var post = await _db.GetByKey<Post>(new {PostId = id});
                FillPost(post, new PostData {Text = "Test2"});
                await _db.Update(post);
                Assert.Equal("Test2", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Posts WHERE PostId = @id", new {id})));
            }
            {
                var rowCount = await _db.Delete<Post>(new {PostId = id});
                Assert.Equal(1, rowCount);
                Assert.Empty(await _db.QueryListAsync<string>(new Sql("SELECT Text FROM Posts WHERE PostId = @id", new {id})));
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
SELECT p.PostId, p.Text, p.CreationDate
FROM ({Post(sql)}) p
WHERE p.CreationDate <= @toDate
ORDER BY p.PostId", new {toDate});

            var postInfos = await _db.QueryListAsync<PostInfo>(sql);
            
            Assert.Equal(2, postInfos.Count);
        }

        private static Sql Post(Sql sql)
        {
            var fromDate = new DateTime(2015, 1, 1);
            
            return sql.Sql(@"
SELECT * 
FROM Posts p
WHERE p.CreationDate >= @fromDate
", new {fromDate});
        }
        
        [Fact]
        public async Task InsertUpdate_ReadOnlyColumn1_Success()
        {
            int id;
            {
                var entity = new Table2{Text = "Test"};
                id = await _db.Insert<int>(entity);
                Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Table2s WHERE Id = @id", new {id})));
            }
            {
                var entity = await _db.GetByKey<Table2>(new {Id = id});  
                Assert.Equal(1, entity.ReadOnlyColumn1);
                entity.Text = "Test2";
                await _db.Update(entity);
                Assert.Equal("Test2", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Table2s WHERE Id = @id", new {id})));
            }
        }
        
        [Fact]
        public async Task InsertUpdate_CustomTableName_Success()
        {
            int id;
            {
                var entity = new Comment{Text = "Test"};
                id = await _db.Insert<int>(entity);
                Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Comment2s WHERE Id = @id", new {id})));
            }
            {
                var entity = await _db.GetByKey<Comment>(new {Id = id});
                entity.Text = "Test2";
                await _db.Update(entity);
                Assert.Equal("Test2", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Comment2s WHERE Id = @id", new {id})));
            }
        }
        
        [Fact]
        public async Task Insert_Success()
        {
            var id = 5;
            var entity = new Table3 {Id = id, Text = "Test"};
            await _db.Insert(entity);
            Assert.Equal("Test", await _db.QuerySingleAsync<string>(new Sql("SELECT Text FROM Table3s WHERE Id = @id", new {id})));

            var rowCount = await _db.Delete<Table3>(new {Id = id});
            Assert.Equal(1, rowCount);
        }
        
        [Fact]
        public async Task TVP_Success()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Code");
            dataTable.Columns.Add("Name");

            for (var i = 0; i < 5; i++) 
                dataTable.Rows.Add("Code_" + i, "Name_" + i);

            var sql = new Sql(@"
SELECT *
FROM @Customers",
                new
                {
                    Customers = dataTable.AsTableValuedParameter("TVP_Customer")
                });
            var customers = await _db.QueryListAsync<Customer>(sql);
            Assert.Equal(5, customers.Count);
            Assert.Equal("Code_4", customers[4].Code);
        }
    }
}