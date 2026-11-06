using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure
{
    /// <summary>
    /// Starts PostgreSQL and Redis and hosts the API for integration tests.
    /// </summary>
    public sealed class IntegrationTestFixture : IAsyncLifetime
    {
        /// <summary>
        /// The the administrator email configured for test seeding.
        /// </summary>
        public const string SeedAdminEmail = "integration-admin@feuerwehr.local";
        /// <summary>
        /// The the administrator password configured for test seeding.
        /// </summary>
        public const string SeedAdminPassword = "IntegrationAdmin123";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("postgresdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
            .Build();

        private ApiWebApplicationFactory? _factory;

        /// <summary>
        /// Gets the HTTP client for the hosted test API.
        /// </summary>
        public HttpClient Client { get; private set; } = default!;

        /// <summary>
        /// Loads the Avalonia application resources and styles.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        public async Task InitializeAsync()
        {
            await Task.WhenAll(this._postgres.StartAsync(), this._redis.StartAsync());

            Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", this._postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("ConnectionStrings__cache", this._redis.GetConnectionString());
            Environment.SetEnvironmentVariable("SeedAdmin__Email", SeedAdminEmail);
            Environment.SetEnvironmentVariable("SeedAdmin__Password", SeedAdminPassword);
            Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "YourSuperSecretKeyForJWT_MinimumLength32Characters_ChangeInProduction!");

            this._factory = new ApiWebApplicationFactory(
                this._postgres.GetConnectionString(),
                this._redis.GetConnectionString(),
                SeedAdminEmail,
                SeedAdminPassword);

            this.Client = this._factory.CreateClient(new ()
            {
                AllowAutoRedirect = false,
            });
        }

        /// <summary>
        /// Signs in as the seeded administrator and retrieves an access token for integration requests.
        /// </summary>
        /// <returns>A task whose result is the formatted integration test fixture content.</returns>
        public async Task<string> LoginAsAdminAndGetAccessTokenAsync()
        {
            HttpResponseMessage response = await this.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = SeedAdminEmail,
                Password = SeedAdminPassword,
            });

            _ = response.EnsureSuccessStatusCode();
            LoginResponse? login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return login?.AccessToken ?? throw new InvalidOperationException("Login response did not contain an access token.");
        }

        /// <summary>
        /// Releases the resources owned by this integration test fixture component.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        public async Task DisposeAsync()
        {
            this.Client.Dispose();

            if (this._factory is not null)
            {
                await this._factory.DisposeAsync();
            }

            Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", null);
            Environment.SetEnvironmentVariable("ConnectionStrings__cache", null);
            Environment.SetEnvironmentVariable("SeedAdmin__Email", null);
            Environment.SetEnvironmentVariable("SeedAdmin__Password", null);
            Environment.SetEnvironmentVariable("JwtSettings__SecretKey", null);

            await this._redis.DisposeAsync();
            await this._postgres.DisposeAsync();
        }
    }
}
