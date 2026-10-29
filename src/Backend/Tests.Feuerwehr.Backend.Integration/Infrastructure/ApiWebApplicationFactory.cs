using de.openelp.feuerwehr.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace de.openelp.feuerwehr.Tests.Backend.Integration.Infrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;
    private readonly string _redisConnectionString;
    private readonly string _seedAdminEmail;
    private readonly string _seedAdminPassword;

    public ApiWebApplicationFactory(
        string postgresConnectionString,
        string redisConnectionString,
        string seedAdminEmail,
        string seedAdminPassword)
    {
        _postgresConnectionString = postgresConnectionString;
        _redisConnectionString = redisConnectionString;
        _seedAdminEmail = seedAdminEmail;
        _seedAdminPassword = seedAdminPassword;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:postgresdb"] = _postgresConnectionString,
                ["ConnectionStrings:cache"] = _redisConnectionString,
                ["SeedAdmin:Email"] = _seedAdminEmail,
                ["SeedAdmin:Password"] = _seedAdminPassword,
                ["JwtSettings:SecretKey"] = "YourSuperSecretKeyForJWT_MinimumLength32Characters_ChangeInProduction!"
            });
        });
    }
}