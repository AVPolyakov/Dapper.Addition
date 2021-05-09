using Xunit;

namespace PlainSql.PostgreSql.Tests
{
    [CollectionDefinition(nameof(FixtureCollection))]
    public class FixtureCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}