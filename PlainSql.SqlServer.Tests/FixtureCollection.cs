using Xunit;

namespace PlainSql.SqlServer.Tests
{
    [CollectionDefinition(nameof(FixtureCollection))]
    public class FixtureCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}