using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using PlainQuery.SharedTests;
using Xunit;

namespace PlainQuery.SqlServerTests
{
    [Collection(nameof(DatabaseCollection))]
    public class Tests: TestBase
    {
        private static DbExecutor Db => DatabaseFixture.Db;

        public Tests() => MappingCheckSettings.MappingCheckEnabled = true;

        [Fact]
        public async Task Posts_Success()
        {
            var date = new DateTime(2015, 1, 1);
            
            var query = new Query(@"
SELECT p.PostId, p.Text, p.CreationDate
FROM Posts p
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
FROM Posts p
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
                Assert.Equal("Test", await new Query("SELECT Text FROM Posts WHERE PostId = @id", new {id}).Single<string>(Db));
            }
            {
                var post = await Db.GetByKey<Post>(new {PostId = id});
                FillPost(post, new PostData {Text = "Test2"});
                await Db.Update(post);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Posts WHERE PostId = @id", new {id}).Single<string>(Db));
            }
            {
                var rowCount = await Db.Delete<Post>(new {PostId = id});
                Assert.Equal(1, rowCount);
                Assert.Empty(await new Query("SELECT Text FROM Posts WHERE PostId = @id", new {id}).ToList<string>(Db));
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
FROM Posts p
WHERE p.CreationDate >= @fromDate
", new {fromDate});
        }

        [Fact]
        public async Task ScalarType_Success()
        {
            var single = await new Query("SELECT @A1 AS A1",
                    new
                    {
                        A1 = "Test3"
                    })
                .Single<string>(Db);
            
            Assert.Equal("Test3", single);
        }        
        
        [Fact]
        public async Task Enum_Success()
        {
            Enum1? a2 = Enum1.Item2;
            Enum1? a3 = null;
            Enum2? a5 = Enum2.Item2;
            Enum2? a6 = null;
            
            var record1 = await new Query(@"
SELECT 
    @A1 AS A1,
    @A2 AS A2,
    @A3 AS A3,
    @A4 AS A4,
    @A5 AS A5,
    @A6 AS A6
",
                    new
                    {
                        A1 = Enum1.Item2,
                        A2 = a2,
                        A3 = a3,
                        A4 = Enum2.Item2,
                        A5 = a5,
                        A6 = a6,
                    })
                .Single<Record1>(Db);
            
            Assert.Equal(Enum1.Item2, record1.A1);
            Assert.Equal(a2, record1.A2);
            Assert.Equal(a3, record1.A3);
            Assert.Equal(Enum2.Item2, record1.A4);
            Assert.Equal(a5, record1.A5);
            Assert.Equal(a6, record1.A6);
        }
        
        [Fact]
        public async Task InsertUpdate_ReadOnlyColumn1_Success()
        {
            int id;
            {
                var entity = new Table2{Text = "Test"};
                id = await Db.Insert<int>(entity);
                Assert.Equal("Test", await new Query("SELECT Text FROM Table2s WHERE Id = @id", new {id}).Single<string>(Db));
            }
            {
                var entity = await Db.GetByKey<Table2>(new {Id = id});  
                Assert.Equal(1, entity.ReadOnlyColumn1);
                entity.Text = "Test2";
                await Db.Update(entity);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Table2s WHERE Id = @id", new {id}).Single<string>(Db));
            }
        }
        
        [Fact]
        public async Task InsertUpdate_CustomTableName_Success()
        {
            int id;
            {
                var entity = new Comment{Text = "Test"};
                id = await Db.Insert<int>(entity);
                Assert.Equal("Test", await new Query("SELECT Text FROM Comment2s WHERE Id = @id", new {id}).Single<string>(Db));
            }
            {
                var entity = await Db.GetByKey<Comment>(new {Id = id});
                entity.Text = "Test2";
                await Db.Update(entity);
                Assert.Equal("Test2", await new Query("SELECT Text FROM Comment2s WHERE Id = @id", new {id}).Single<string>(Db));
            }
        }
        
        [Fact]
        public async Task Insert_Success()
        {
            var id = 5;
            var entity = new Table3 {Id = id, Text = "Test"};
            await Db.Insert(entity);
            Assert.Equal("Test", await new Query("SELECT Text FROM Table3s WHERE Id = @id", new {id}).Single<string>(Db));

            var rowCount = await Db.Delete<Table3>(new {Id = id});
            Assert.Equal(1, rowCount);
        }
        
        [Fact]
        public async Task MatchNamesWithUnderscores_Success()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            Assert.Equal("Test1", (await new Query("SELECT * FROM Table4s WHERE Id = @id", new {id = 1}).Single<Table4>(Db)).FirstName);
            
            await Db.Delete<Table4>(new {Id = 2});
            
            var entity = new Table4 {Id = 2, FirstName = "Test2"};
            await Db.Insert(entity);
            Assert.Equal("Test2", (await new Query("SELECT Id, first_name FROM Table4s WHERE Id = @id", new {id = 2}).Single<Table4>(Db)).FirstName);

            DefaultTypeMap.MatchNamesWithUnderscores = false;
        }


        [Fact]
        public async Task TVP_Success()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("Code");
            dataTable.Columns.Add("Name");

            for (var i = 0; i < 5; i++) 
                dataTable.Rows.Add("Code_" + i, "Name_" + i);

            var query = new Query(@"
SELECT *
FROM @Customers",
                new
                {
                    Customers = dataTable.AsTableValuedParameter("TVP_Customer")
                });
            var customers = await query.ToList<Customer>(Db);
            Assert.Equal(5, customers.Count);
            Assert.Equal("Code_4", customers[4].Code);
        }
    }
}