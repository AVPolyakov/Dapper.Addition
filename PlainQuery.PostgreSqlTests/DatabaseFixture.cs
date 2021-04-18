using System;
using System.Reflection;
using System.Threading.Tasks;
using DbUp;
using Npgsql;
using Xunit;

namespace PlainQuery.PostgreSqlTests
{
    public class DatabaseFixture: IAsyncLifetime
    {
        private const string DatabaseName = "plain_query";
        public static readonly ConnectionHandler Db = new($@"Server=127.0.0.1;Port=5432;Database={DatabaseName};User Id=postgres;Password=qwe123456;");
        
        public async Task InitializeAsync()
        {
            var connectionString = new NpgsqlConnectionStringBuilder(Db.ConnectionString) {Database = "postgres"}.ConnectionString;
            var db = new ConnectionHandler(connectionString);
            
            var singleOrDefault = await new Query(@$"
SELECT datname
FROM pg_catalog.pg_database 
WHERE datname = '{DatabaseName}'").SingleOrDefault<string?>(db);

            if (singleOrDefault != null)
                await new Query($"DROP DATABASE {DatabaseName}").Execute(db);

            await new Query($"CREATE DATABASE {DatabaseName}").Execute(db);

            var upgrader = DeployChanges.To
                .PostgresqlDatabase(Db.ConnectionString)
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