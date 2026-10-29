namespace de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "BackendIntegration";
}