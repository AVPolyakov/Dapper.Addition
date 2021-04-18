using Xunit;

namespace PlainQuery.SqlServerTests
{
    [CollectionDefinition(nameof(DatabaseCollection))]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}