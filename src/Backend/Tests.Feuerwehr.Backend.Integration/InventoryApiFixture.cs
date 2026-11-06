using System.Net.Http.Headers;
using System.Net.Http.Json;
using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    /// <summary>
    /// Hosts the API with PostgreSQL, test users, and mail capture for inventory and vehicle integration scenarios.
    /// </summary>
    public sealed class InventoryApiFixture : IAsyncLifetime
    {
        /// <summary>
        /// Gets the PostgreSQL test container.
        /// </summary>
        public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();
        private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025))).Build();

        /// <summary>
        /// Gets the factory that creates the navigation page content.
        /// </summary>
        public WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program> Factory { get; private set; } = null!;

        /// <summary>
        /// Gets the fixture's current calendar date.
        /// </summary>
        public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

        /// <summary>
        /// Gets the mail-capture service URL exposed to integration tests.
        /// </summary>
        public Uri MailpitUrl => new($"http://{this._mailpit.Hostname}:{this._mailpit.GetMappedPublicPort(8025)}");

        private const string Password = "InventoryTest123456";

        /// <summary>
        /// Loads the Avalonia application resources and styles.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        public async Task InitializeAsync()
        {
            try
            {
                await Task.WhenAll(this.Postgres.StartAsync(), this._redis.StartAsync(), this._mailpit.StartAsync());
                this.Factory = new InventoryFactory(new Dictionary<string, string>
                {
                    ["ConnectionStrings:postgresdb"] = this.Postgres.GetConnectionString(),
                    ["ConnectionStrings:cache"] = this._redis.GetConnectionString(),
                    ["MAIL_HOST"] = this._mailpit.Hostname,
                    ["MAIL_PORT"] = this._mailpit.GetMappedPublicPort(1025).ToString(),
                    ["MAIL_SECURITY"] = "None",
                    ["MAIL_USERNAME"] = string.Empty,
                    ["MAIL_PASSWORD"] = string.Empty,
                    ["MAIL_FROM"] = "noreply@integration.test",
                    ["Frontend:BaseUrl"] = "https://frontend.integration.test",
                    ["SeedAdmin:Email"] = "admin@integration.test",
                    ["SeedAdmin:Password"] = Password,
                    ["JwtSettings:SecretKey"] = "inventory-integration-tests-only-signing-key-1234567890",
                    ["JwtSettings:Issuer"] = "inventory-tests",
                    ["JwtSettings:Audience"] = "inventory-tests",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty,
                });
                using HttpClient startup = this.Factory.CreateClient();
                foreach (string? role in new[] { Roles.EquipmentOfficer, Roles.InventoryViewer, Roles.Firefighter })
                {
                    await this.CreateUser(role, $"{role}@integration.test");
                }
            }
            catch
            {
                await this.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Creates a test user with the requested role and activation state.
        /// </summary>
        /// <param name="role">The Identity role under test.</param>
        /// <param name="email">The user's email address.</param>
        /// <param name="active">Active is enabled for this inventory API fixture.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        public async Task CreateUser(string role, string email, bool active = true)
        {
            using IServiceScope scope = this.Factory.Services.CreateScope();
            UserManager<ApplicationUser> users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            ApplicationUser user = new()
            { UserName = email, Email = email, FirstName = "Test", LastName = role, IsActive = active };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }

        /// <summary>
        /// Creates an HTTP client authenticated as the test user with the requested role.
        /// </summary>
        /// <param name="role">The Identity role under test.</param>
        /// <returns>A task whose result is the test API client authenticated with the requested role.</returns>
        public async Task<HttpClient> Client(string role)
        {
            HttpClient client = this.Factory.CreateClient();
            HttpResponseMessage login = await client.PostAsJsonAsync("/api/Auth/login", new LoginRequest { Email = role == Roles.Admin ? "admin@integration.test" : $"{role}@integration.test", Password = Password });
            _ = login.EnsureSuccessStatusCode();
            LoginResponse? tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
            return client;
        }

        /// <summary>
        /// Creates an equipment type with the requested inspection interval through the hosted API.
        /// </summary>
        /// <param name="client">The authenticated HTTP client used by the test scenario.</param>
        /// <param name="interval">The inspection interval in calendar months used in this scenario.</param>
        /// <returns>A task whose result is the created equipment type and its inspection plan.</returns>
        public async Task<EquipmentTypeDto> CreateType(HttpClient client, int interval = 12)
        {
            InventoryCategoryDto[]? categories = await client.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories");
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest
            { CategoryId = categories![0].Id, Name = $"Typ {Guid.NewGuid()}", InspectionInstructions = "Sicht- und Funktionsprüfung nach Herstellerangaben", InspectionIntervalMonths = interval });
            _ = response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EquipmentTypeDto>())!;
        }

        /// <summary>
        /// Creates equipment through the hosted API for use in the integration scenario.
        /// </summary>
        /// <param name="client">The authenticated HTTP client used by the test scenario.</param>
        /// <param name="type">The kind of inventory API fixture.</param>
        /// <returns>A task whose result is the equipment details, including the calculated next inspection date.</returns>
        public async Task<EquipmentDto> CreateEquipment(HttpClient client, Guid type)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest { EquipmentTypeId = type, SerialNumber = Guid.NewGuid().ToString(), Manufacturer = "Testhersteller" });
            _ = response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
        }

        /// <summary>
        /// Releases the resources owned by this inventory API fixture component.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        public async Task DisposeAsync()
        {
            if (this.Factory is not null)
            {
                await this.Factory.DisposeAsync();
            }

            await Task.WhenAll(this.Postgres.DisposeAsync().AsTask(), this._redis.DisposeAsync().AsTask(), this._mailpit.DisposeAsync().AsTask());
        }

        /// <summary>
        /// Overrides API configuration with the integration fixture's container connections and seed settings.
        /// </summary>
        /// <param name="settings">The resolved configuration settings used by this component.</param>
        private sealed class InventoryFactory(Dictionary<string, string> settings) : WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program>
        {
            /// <summary>
            /// Configures the test API host with fixture-specific infrastructure and seed credentials.
            /// </summary>
            /// <param name="builder">The builder whose services and host settings are configured.</param>
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                _ = builder.UseEnvironment("Testing");
                foreach (KeyValuePair<string, string> pair in settings)
                {
                    _ = builder.UseSetting(pair.Key, pair.Value);
                }
            }
        }
    }
}
