using Xunit;

namespace Dapper.Addition.PostgreSql.Tests
{
    [CollectionDefinition(nameof(FixtureCollection))]
    public class FixtureCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}