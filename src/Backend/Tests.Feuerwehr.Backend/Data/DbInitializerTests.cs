using De.Openelp.Feuerwehr.Common.Models.Auth;
using De.Openelp.Feuerwehr.Server;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Controller;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Data
{
    /// <summary>
    /// Tests db initializer behavior and its expected outcomes.
    /// </summary>
    public sealed class DbInitializerTests : IDisposable
    {
        private const string AdminEmail = "admin@feuerwehr.local";
        private const string InitialPassword = "InitialSeedPassword123!";
        private readonly IConfigurationRoot _configuration;
        private readonly ServiceProvider _provider;
        private readonly UserManager<ApplicationUser> _users;
        private readonly CapturingLogger _logger = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="DbInitializerTests" /> class with its required dependencies.
        /// </summary>
        public DbInitializerTests()
        {
            this._configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            ServiceCollection services = new ();
            _ = services.AddLogging();
            _ = services.AddSingleton<IConfiguration>(this._configuration);
            _ = services.AddSingleton<ILogger<Program>>(this._logger);
            _ = services.AddDbContext<FeuerwehrDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            _ = services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FeuerwehrDbContext>();
            this._provider = services.BuildServiceProvider();
            this._users = this._provider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        /// <summary>
        /// Verifies that seed data uses environment credentials and assigns all admin roles.
        /// </summary>
        /// <param name="configuredEmail">The administrator email supplied for this seeding scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("admin@example.org")]
        [InlineData("  admin@example.org  ")]
        public async Task SeedData_UsesEnvironmentCredentialsAndAssignsAllAdminRoles(string? configuredEmail)
        {
            // A unique prefix avoids changing the real application's configuration or other tests.
            string prefix = $"DbInitializerTest_{Guid.NewGuid():N}_";
            string key = prefix + "SeedAdmin__Password";
            string emailKey = prefix + "SeedAdmin__Email";
            Environment.SetEnvironmentVariable(key, InitialPassword);
            Environment.SetEnvironmentVariable(emailKey, configuredEmail);
            try
            {
                IConfigurationRoot environmentConfiguration = new ConfigurationBuilder().AddEnvironmentVariables(prefix).Build();
                this._configuration["SeedAdmin:Password"] = environmentConfiguration["SeedAdmin:Password"];
                this._configuration["SeedAdmin:Email"] = environmentConfiguration["SeedAdmin:Email"];

                await DbInitializer.SeedDataAsync(this._provider);

                string expectedEmail = string.IsNullOrWhiteSpace(configuredEmail) ? AdminEmail : configuredEmail.Trim();
                ApplicationUser? admin = await this._users.FindByEmailAsync(expectedEmail);
                Assert.NotNull(admin);
                Assert.Equal(expectedEmail, admin.Email);
                Assert.Equal(expectedEmail, admin.UserName);
                _ = Assert.Single(await this._users.Users.ToListAsync());
                Assert.True(await this._users.CheckPasswordAsync(admin, InitialPassword));
                Assert.True(admin.EmailConfirmed);
                Assert.True(admin.IsActive);
                Assert.Equal(
                    new[] { Roles.Admin, Roles.IncidentDiaryEditor, Roles.SituationMapEditor }.Order(),
                    (await this._users.GetRolesAsync(admin)).Order());
                Assert.Equal(6, await this._provider.GetRequiredService<FeuerwehrDbContext>().DiaryCategories.CountAsync());
                Assert.DoesNotContain(InitialPassword, string.Join("\n", this._logger.Messages));
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, null);
                Environment.SetEnvironmentVariable(emailKey, null);
            }
        }

        /// <summary>
        /// Verifies that seed data missing password fails without creating admin.
        /// </summary>
        /// <param name="password">The password supplied for authentication.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SeedData_MissingPasswordFailsWithoutCreatingAdmin(string? password)
        {
            this._configuration["SeedAdmin:Password"] = password;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(this._provider));

            Assert.Contains("SeedAdmin__Password", error.Message);
            Assert.Null(await this._users.FindByEmailAsync(AdminEmail));
        }

        /// <summary>
        /// Verifies that seed data invalid password fails without creating admin or logging password.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task SeedData_InvalidPasswordFailsWithoutCreatingAdminOrLoggingPassword()
        {
            const string invalidPassword = "weak-secret";
            this._configuration["SeedAdmin:Password"] = invalidPassword;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(this._provider));

            Assert.Contains("SeedAdmin__Password", error.Message);
            Assert.Contains("PasswordRequiresDigit", error.Message);
            Assert.Null(await this._users.FindByEmailAsync(AdminEmail));
            Assert.DoesNotContain(invalidPassword, error.ToString());
            Assert.DoesNotContain(invalidPassword, string.Join("\n", this._logger.Messages));
        }

        /// <summary>
        /// Verifies that seed data existing admin keeps password and repairs missing roles.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="configuredPassword">The administrator password supplied for this seeding scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(AdminEmail, null)]
        [InlineData(AdminEmail, "DifferentSeedPassword456!")]
        [InlineData("admin@example.org", null)]
        [InlineData("admin@example.org", "DifferentSeedPassword456!")]
        public async Task SeedData_ExistingAdminKeepsPasswordAndRepairsMissingRoles(string email, string? configuredPassword)
        {
            ApplicationUser admin = new ()
            { UserName = email, Email = email };
            Assert.True((await this._users.CreateAsync(admin, InitialPassword)).Succeeded);
            string? passwordHash = admin.PasswordHash;
            this._configuration["SeedAdmin:Password"] = configuredPassword;
            this._configuration["SeedAdmin:Email"] = email;

            await DbInitializer.SeedDataAsync(this._provider);
            await DbInitializer.SeedDataAsync(this._provider);

            Assert.Equal(passwordHash, admin.PasswordHash);
            Assert.True(await this._users.CheckPasswordAsync(admin, InitialPassword));
            Assert.Equal(3, (await this._users.GetRolesAsync(admin)).Count);
            _ = Assert.Single(await this._users.Users.ToListAsync());
            Assert.Equal(6, await this._provider.GetRequiredService<FeuerwehrDbContext>().DiaryCategories.CountAsync());
            Assert.DoesNotContain(InitialPassword, string.Join("\n", this._logger.Messages));
        }

        /// <summary>
        /// Verifies that seed data existing database adds module roles and allows user role updates.
        /// </summary>
        /// <param name="moduleRole">The Identity role under test.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData(Roles.SituationMapViewer)]
        [InlineData(Roles.SituationMapEditor)]
        [InlineData(Roles.IncidentDiaryViewer)]
        [InlineData(Roles.IncidentDiaryEditor)]
        public async Task SeedData_ExistingDatabaseAddsModuleRolesAndAllowsUserRoleUpdates(string moduleRole)
        {
            RoleManager<IdentityRole> roles = this._provider.GetRequiredService<RoleManager<IdentityRole>>();
            Dictionary<string, string> originalRoleIds = new ();
            foreach (string? name in new[] { Roles.Admin, Roles.Commander, Roles.Firefighter, Roles.Viewer })
            {
                IdentityRole role = new (name);
                Assert.True((await roles.CreateAsync(role)).Succeeded);
                originalRoleIds.Add(name, role.Id);
            }

            ApplicationUser admin = new ()
            { UserName = AdminEmail, Email = AdminEmail };
            Assert.True((await this._users.CreateAsync(admin, InitialPassword)).Succeeded);
            Assert.True((await this._users.AddToRoleAsync(admin, Roles.Admin)).Succeeded);
            ApplicationUser user = new ()
            { UserName = "viewer@example.org", Email = "viewer@example.org" };
            Assert.True((await this._users.CreateAsync(user)).Succeeded);
            Assert.True((await this._users.AddToRoleAsync(user, Roles.Viewer)).Succeeded);
            Assert.False(await roles.RoleExistsAsync(moduleRole));

            await DbInitializer.SeedDataAsync(this._provider);
            await DbInitializer.SeedDataAsync(this._provider);

            Assert.Equal(Roles.AllRoles.Order(), (await roles.Roles.Select(role => role.Name!).ToListAsync()).Order());
            foreach ((string? name, string? id) in originalRoleIds)
            {
                Assert.Equal(id, (await roles.FindByNameAsync(name))!.Id);
            }

            Assert.Equal(new[] { Roles.Viewer }, await this._users.GetRolesAsync(user));

            UsersController controller = new (new UsersService(
                this._users,
                roles,
                Mock.Of<IUserInvitationService>(),
                NullLogger<UsersService>.Instance));
            IActionResult response = await controller.UpdateUser(user.Id, new UpdateUserRequest
            {
                Roles = [Roles.Viewer, moduleRole]
            });

            _ = Assert.IsType<OkObjectResult>(response);
            Assert.Equal(new[] { Roles.Viewer, moduleRole }.Order(), (await this._users.GetRolesAsync(user)).Order());
        }

        /// <summary>
        /// Verifies that seed data invalid email fails without creating admin.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task SeedData_InvalidEmailFailsWithoutCreatingAdmin()
        {
            this._configuration["SeedAdmin:Email"] = "not-an-email";
            this._configuration["SeedAdmin:Password"] = InitialPassword;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(this._provider));

            Assert.Contains("SeedAdmin__Email", error.Message);
            Assert.Contains("InvalidEmail", error.Message);
            Assert.Empty(await this._users.Users.ToListAsync());
            Assert.DoesNotContain(InitialPassword, string.Join("\n", this._logger.Messages));
        }

        /// <summary>
        /// Releases the resources owned by this db initializer component.
        /// </summary>
        public void Dispose()
        {
            this._provider.Dispose();
            (this._configuration as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Captures log entries so tests can verify diagnostic behavior.
        /// </summary>
        private sealed class CapturingLogger : ILogger<Program>
        {
            /// <summary>
            /// Gets the messages associated with this capturing logger.
            /// </summary>
            public List<string> Messages { get; } = [];

            /// <summary>
            /// Creates the logging scope used by the test logger.
            /// </summary>
            /// <typeparam name="TState">The state type used by this contract.</typeparam>
            /// <param name="state">The structured state associated with the captured log entry.</param>
            /// <returns>The logging scope, or null when scope tracking is not required.</returns>
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            /// <summary>
            /// Determines whether enabled.
            /// </summary>
            /// <param name="logLevel">The severity of the log entry captured by the test.</param>
            /// <returns>True if the requested capturing logger operation succeeds; otherwise, false.</returns>
            public bool IsEnabled(LogLevel logLevel) => true;

            /// <summary>
            /// Captures the formatted log entry for subsequent test assertions.
            /// </summary>
            /// <typeparam name="TState">The state type used by this contract.</typeparam>
            /// <param name="logLevel">The severity of the log entry captured by the test.</param>
            /// <param name="eventId">The identifier of the associated event.</param>
            /// <param name="state">The structured state associated with the captured log entry.</param>
            /// <param name="exception">The failure to include in the diagnostic entry, when present.</param>
            /// <param name="formatter">The formatter used to render the captured log message.</param>
            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) => this.Messages.Add(formatter(state, exception) + exception);
        }
    }
}
