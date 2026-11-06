namespace De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure
{
    /// <summary>
    /// Groups integration tests that share the same application and container infrastructure.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
    {
        /// <summary>
        /// The the display name of the integration test collection.
        /// </summary>
        public const string Name = "BackendIntegration";
    }
}
