using Xunit;

namespace PlainQueryExtensions.SqlServerTests
{
    [CollectionDefinition(nameof(DatabaseCollection))]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}