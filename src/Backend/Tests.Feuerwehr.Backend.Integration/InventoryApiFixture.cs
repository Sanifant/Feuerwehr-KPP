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
    public sealed class InventoryApiFixture : IAsyncLifetime
    {
        public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();
        private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8025))).Build();

        public WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program> Factory { get; private set; } = null!;

        public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

        public Uri MailpitUrl => new($"http://{this._mailpit.Hostname}:{this._mailpit.GetMappedPublicPort(8025)}");

        private const string Password = "InventoryTest123456";

        /// <summary>
        /// Initializes the operation.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Creates the user.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="email">The email.</param>
        /// <param name="active">The active.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Executes client.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
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
        /// Creates the type.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="interval">The interval.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<EquipmentTypeDto> CreateType(HttpClient client, int interval = 12)
        {
            InventoryCategoryDto[]? categories = await client.GetFromJsonAsync<InventoryCategoryDto[]>("/api/inventory/categories");
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory/types", new SaveEquipmentTypeRequest
            { CategoryId = categories![0].Id, Name = $"Typ {Guid.NewGuid()}", InspectionInstructions = "Sicht- und Funktionsprüfung nach Herstellerangaben", InspectionIntervalMonths = interval });
            _ = response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EquipmentTypeDto>())!;
        }

        /// <summary>
        /// Creates the equipment.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="type">The type.</param>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task<EquipmentDto> CreateEquipment(HttpClient client, Guid type)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/inventory", new SaveEquipmentRequest { EquipmentTypeId = type, SerialNumber = Guid.NewGuid().ToString(), Manufacturer = "Testhersteller" });
            _ = response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
        }

        /// <summary>
        /// Executes dispose.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public async Task DisposeAsync()
        {
            if (this.Factory is not null)
            {
                await this.Factory.DisposeAsync();
            }

            await Task.WhenAll(this.Postgres.DisposeAsync().AsTask(), this._redis.DisposeAsync().AsTask(), this._mailpit.DisposeAsync().AsTask());
        }

        private sealed class InventoryFactory(Dictionary<string, string> settings) : WebApplicationFactory<De.Openelp.Feuerwehr.Server.Program>
        {
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
