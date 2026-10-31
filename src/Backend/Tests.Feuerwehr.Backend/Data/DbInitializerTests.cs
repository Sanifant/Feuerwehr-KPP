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
    public sealed class DbInitializerTests : IDisposable
    {
        private const string AdminEmail = "admin@feuerwehr.local";
        private const string InitialPassword = "InitialSeedPassword123!";
        private readonly IConfigurationRoot _configuration;
        private readonly ServiceProvider _provider;
        private readonly UserManager<ApplicationUser> _users;
        private readonly CapturingLogger _logger = new();

        public DbInitializerTests()
        {
            _configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IConfiguration>(_configuration);
            services.AddSingleton<ILogger<Program>>(_logger);
            services.AddDbContext<FeuerwehrDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            }).AddRoles<IdentityRole>().AddEntityFrameworkStores<FeuerwehrDbContext>();
            _provider = services.BuildServiceProvider();
            _users = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("admin@example.org")]
        [InlineData("  admin@example.org  ")]
        /// <summary>
        /// Executes seed Data Uses Environment Credentials And Assigns All Admin Roles.
        /// </summary>
        /// <param name="configuredEmail">The configured Email.</param>
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
                _configuration["SeedAdmin:Password"] = environmentConfiguration["SeedAdmin:Password"];
                _configuration["SeedAdmin:Email"] = environmentConfiguration["SeedAdmin:Email"];

                await DbInitializer.SeedDataAsync(_provider);

                string expectedEmail = string.IsNullOrWhiteSpace(configuredEmail) ? AdminEmail : configuredEmail.Trim();
                ApplicationUser? admin = await _users.FindByEmailAsync(expectedEmail);
                Assert.NotNull(admin);
                Assert.Equal(expectedEmail, admin.Email);
                Assert.Equal(expectedEmail, admin.UserName);
                Assert.Single(await _users.Users.ToListAsync());
                Assert.True(await _users.CheckPasswordAsync(admin, InitialPassword));
                Assert.True(admin.EmailConfirmed);
                Assert.True(admin.IsActive);
                Assert.Equal(
                    new[] { Roles.Admin, Roles.IncidentDiaryEditor, Roles.SituationMapEditor }.Order(),
                    (await _users.GetRolesAsync(admin)).Order());
                Assert.Equal(6, await _provider.GetRequiredService<FeuerwehrDbContext>().DiaryCategories.CountAsync());
                Assert.DoesNotContain(InitialPassword, string.Join("\n", _logger.Messages));
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, null);
                Environment.SetEnvironmentVariable(emailKey, null);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        /// <summary>
        /// Executes seed Data Missing Password Fails Without Creating Admin.
        /// </summary>
        /// <param name="password">The password.</param>
        public async Task SeedData_MissingPasswordFailsWithoutCreatingAdmin(string? password)
        {
            _configuration["SeedAdmin:Password"] = password;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(_provider));

            Assert.Contains("SeedAdmin__Password", error.Message);
            Assert.Null(await _users.FindByEmailAsync(AdminEmail));
        }

        [Fact]
        /// <summary>
        /// Executes seed Data Invalid Password Fails Without Creating Admin Or Logging Password.
        /// </summary>
        public async Task SeedData_InvalidPasswordFailsWithoutCreatingAdminOrLoggingPassword()
        {
            const string invalidPassword = "weak-secret";
            _configuration["SeedAdmin:Password"] = invalidPassword;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(_provider));

            Assert.Contains("SeedAdmin__Password", error.Message);
            Assert.Contains("PasswordRequiresDigit", error.Message);
            Assert.Null(await _users.FindByEmailAsync(AdminEmail));
            Assert.DoesNotContain(invalidPassword, error.ToString());
            Assert.DoesNotContain(invalidPassword, string.Join("\n", _logger.Messages));
        }

        [Theory]
        [InlineData(AdminEmail, null)]
        [InlineData(AdminEmail, "DifferentSeedPassword456!")]
        [InlineData("admin@example.org", null)]
        [InlineData("admin@example.org", "DifferentSeedPassword456!")]
        /// <summary>
        /// Executes seed Data Existing Admin Keeps Password And Repairs Missing Roles.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="configuredPassword">The configured Password.</param>
        public async Task SeedData_ExistingAdminKeepsPasswordAndRepairsMissingRoles(string email, string? configuredPassword)
        {
            ApplicationUser admin = new ApplicationUser { UserName = email, Email = email };
            Assert.True((await _users.CreateAsync(admin, InitialPassword)).Succeeded);
            string? passwordHash = admin.PasswordHash;
            _configuration["SeedAdmin:Password"] = configuredPassword;
            _configuration["SeedAdmin:Email"] = email;

            await DbInitializer.SeedDataAsync(_provider);
            await DbInitializer.SeedDataAsync(_provider);

            Assert.Equal(passwordHash, admin.PasswordHash);
            Assert.True(await _users.CheckPasswordAsync(admin, InitialPassword));
            Assert.Equal(3, (await _users.GetRolesAsync(admin)).Count);
            Assert.Single(await _users.Users.ToListAsync());
            Assert.Equal(6, await _provider.GetRequiredService<FeuerwehrDbContext>().DiaryCategories.CountAsync());
            Assert.DoesNotContain(InitialPassword, string.Join("\n", _logger.Messages));
        }

        [Theory]
        [InlineData(Roles.SituationMapViewer)]
        [InlineData(Roles.SituationMapEditor)]
        [InlineData(Roles.IncidentDiaryViewer)]
        [InlineData(Roles.IncidentDiaryEditor)]
        /// <summary>
        /// Executes seed Data Existing Database Adds Module Roles And Allows User Role Updates.
        /// </summary>
        /// <param name="moduleRole">The module Role.</param>
        public async Task SeedData_ExistingDatabaseAddsModuleRolesAndAllowsUserRoleUpdates(string moduleRole)
        {
            RoleManager<IdentityRole> roles = _provider.GetRequiredService<RoleManager<IdentityRole>>();
            Dictionary<string, string> originalRoleIds = new Dictionary<string, string>();
            foreach (string? name in new[] { Roles.Admin, Roles.Commander, Roles.Firefighter, Roles.Viewer })
            {
                IdentityRole role = new IdentityRole(name);
                Assert.True((await roles.CreateAsync(role)).Succeeded);
                originalRoleIds.Add(name, role.Id);
            }

            ApplicationUser admin = new ApplicationUser { UserName = AdminEmail, Email = AdminEmail };
            Assert.True((await _users.CreateAsync(admin, InitialPassword)).Succeeded);
            Assert.True((await _users.AddToRoleAsync(admin, Roles.Admin)).Succeeded);
            ApplicationUser user = new ApplicationUser { UserName = "viewer@example.org", Email = "viewer@example.org" };
            Assert.True((await _users.CreateAsync(user)).Succeeded);
            Assert.True((await _users.AddToRoleAsync(user, Roles.Viewer)).Succeeded);
            Assert.False(await roles.RoleExistsAsync(moduleRole));

            await DbInitializer.SeedDataAsync(_provider);
            await DbInitializer.SeedDataAsync(_provider);

            Assert.Equal(Roles.AllRoles.Order(), (await roles.Roles.Select(role => role.Name!).ToListAsync()).Order());
            foreach ((string? name, string? id) in originalRoleIds)
            {
                Assert.Equal(id, (await roles.FindByNameAsync(name))!.Id);
            }
            Assert.Equal(new[] { Roles.Viewer }, await _users.GetRolesAsync(user));

            UsersController controller = new UsersController(new UsersService(
                _users,
                roles,
                Mock.Of<IUserInvitationService>(),
                NullLogger<UsersService>.Instance));
            IActionResult response = await controller.UpdateUser(user.Id, new UpdateUserRequest
            {
                Roles = [Roles.Viewer, moduleRole]
            });

            Assert.IsType<OkObjectResult>(response);
            Assert.Equal(new[] { Roles.Viewer, moduleRole }.Order(), (await _users.GetRolesAsync(user)).Order());
        }

        [Fact]
        /// <summary>
        /// Executes seed Data Invalid Email Fails Without Creating Admin.
        /// </summary>
        public async Task SeedData_InvalidEmailFailsWithoutCreatingAdmin()
        {
            _configuration["SeedAdmin:Email"] = "not-an-email";
            _configuration["SeedAdmin:Password"] = InitialPassword;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => DbInitializer.SeedDataAsync(_provider));

            Assert.Contains("SeedAdmin__Email", error.Message);
            Assert.Contains("InvalidEmail", error.Message);
            Assert.Empty(await _users.Users.ToListAsync());
            Assert.DoesNotContain(InitialPassword, string.Join("\n", _logger.Messages));
        }

        /// <summary>
        /// Executes dispose.
        /// </summary>
        public void Dispose()
        {
            _provider.Dispose();
            (_configuration as IDisposable)?.Dispose();
        }

        private sealed class CapturingLogger : ILogger<Program>
        {
            public List<string> Messages { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;

            /// <summary>
            /// Determines whether enabled.
            /// </summary>
            /// <param name="logLevel">The log Level.</param>
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception) + exception);
        }
    }

}
