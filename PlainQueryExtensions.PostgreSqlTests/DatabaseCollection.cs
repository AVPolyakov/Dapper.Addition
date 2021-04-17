using Xunit;

namespace PlainQueryExtensions.PostgreSqlTests
{
    [CollectionDefinition(nameof(DatabaseCollection))]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}