using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using DbUp;
using Xunit;

namespace PlainQuery.SqlServerTests
{
    public class DatabaseFixture: IAsyncLifetime
    {
        private const string DatabaseName = "PlainQuery";
        public static readonly DbExecutor Db = new(@$"Data Source=(local)\SQL2014;Initial Catalog={DatabaseName};Integrated Security=True");
        
        public async Task InitializeAsync()
        {
            var db = new DbExecutor(new SqlConnectionStringBuilder(Db.ConnectionString) {InitialCatalog = "master"}.ConnectionString);
            await new Query(@$"
IF EXISTS ( SELECT * FROM sys.databases WHERE name = '{DatabaseName}' )
    DROP DATABASE [{DatabaseName}]

CREATE DATABASE [{DatabaseName}]
").Execute(db);
            
            var upgrader = DeployChanges.To
                .SqlDatabase(Db.ConnectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .WithTransactionPerScript()
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
                throw new Exception("Database upgrade failed", result.Error);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}