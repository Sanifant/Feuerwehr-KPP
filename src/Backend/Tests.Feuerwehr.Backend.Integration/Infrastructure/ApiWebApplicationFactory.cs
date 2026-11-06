using De.Openelp.Feuerwehr.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure
{
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
            this._postgresConnectionString = postgresConnectionString;
            this._redisConnectionString = redisConnectionString;
            this._seedAdminEmail = seedAdminEmail;
            this._seedAdminPassword = seedAdminPassword;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment(Environments.Development);

            _ = builder.ConfigureAppConfiguration((_, config) =>
            {
                _ = config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:postgresdb"] = this._postgresConnectionString,
                    ["ConnectionStrings:cache"] = this._redisConnectionString,
                    ["SeedAdmin:Email"] = this._seedAdminEmail,
                    ["SeedAdmin:Password"] = this._seedAdminPassword,
                    ["JwtSettings:SecretKey"] = "YourSuperSecretKeyForJWT_MinimumLength32Characters_ChangeInProduction!"
                });
            });
        }
    }
}
