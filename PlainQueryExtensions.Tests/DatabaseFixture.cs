using System;
using System.Data.SqlClient;
using System.Reflection;
using DbUp;

namespace PlainQueryExtensions.Tests
{
    public class DatabaseFixture
    {
        private const string DatabaseName = "PlainQueryExtensions";
        public static readonly ConnectionHandler Db = new(@$"Data Source=(local)\SQL2014;Initial Catalog={DatabaseName};Integrated Security=True");
        
        public DatabaseFixture()
        {
            DbUp();
        }
        
        private void DbUp()
        {
            using (var connection = new SqlConnection(new SqlConnectionStringBuilder(Db.ConnectionString) {InitialCatalog = "master"}.ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @$"
IF EXISTS ( SELECT * FROM sys.databases WHERE name = '{DatabaseName}' )
    DROP DATABASE [{DatabaseName}]

CREATE DATABASE [{DatabaseName}]
";
                    command.ExecuteNonQuery();
                }
            }
            
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
    }
}