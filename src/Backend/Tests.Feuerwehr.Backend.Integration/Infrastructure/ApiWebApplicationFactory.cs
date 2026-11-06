using De.Openelp.Feuerwehr.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration.Infrastructure
{
    /// <summary>
    /// Hosts the API with the database, cache, and seed-user configuration supplied by integration tests.
    /// </summary>
    public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _postgresConnectionString;
        private readonly string _redisConnectionString;
        private readonly string _seedAdminEmail;
        private readonly string _seedAdminPassword;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiWebApplicationFactory" /> class with its required dependencies.
        /// </summary>
        /// <param name="postgresConnectionString">The PostgreSQL connection string supplied by the test container.</param>
        /// <param name="redisConnectionString">The Redis connection string supplied by the test container.</param>
        /// <param name="seedAdminEmail">The administrator email supplied for this seeding scenario.</param>
        /// <param name="seedAdminPassword">The administrator password supplied for this seeding scenario.</param>
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

        /// <summary>
        /// Configures the test API host with fixture-specific infrastructure and seed credentials.
        /// </summary>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
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
