using System.Net.Http.Json;
using de.openelp.feuerwehr.Common.Models.Auth;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    public const string SeedAdminEmail = "integration-admin@feuerwehr.local";
    public const string SeedAdminPassword = "IntegrationAdmin123";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("postgresdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    private ApiWebApplicationFactory? _factory;

    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__cache", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("SeedAdmin__Email", SeedAdminEmail);
        Environment.SetEnvironmentVariable("SeedAdmin__Password", SeedAdminPassword);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "YourSuperSecretKeyForJWT_MinimumLength32Characters_ChangeInProduction!");

        _factory = new ApiWebApplicationFactory(
            _postgres.GetConnectionString(),
            _redis.GetConnectionString(),
            SeedAdminEmail,
            SeedAdminPassword);

        Client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
        });
    }

    public async Task<string> LoginAsAdminAndGetAccessTokenAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = SeedAdminEmail,
            Password = SeedAdminPassword,
        });

        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login?.AccessToken ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__postgresdb", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__cache", null);
        Environment.SetEnvironmentVariable("SeedAdmin__Email", null);
        Environment.SetEnvironmentVariable("SeedAdmin__Password", null);
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", null);

        await _redis.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
