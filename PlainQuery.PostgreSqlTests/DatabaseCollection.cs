using Xunit;

namespace PlainQuery.PostgreSqlTests
{
    [CollectionDefinition(nameof(DatabaseCollection))]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}