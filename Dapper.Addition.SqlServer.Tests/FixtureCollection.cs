using Xunit;

namespace Dapper.Addition.SqlServer.Tests
{
    [CollectionDefinition(nameof(FixtureCollection))]
    public class FixtureCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}