namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using System.Reflection;
    using System.Text.Json;
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Authorization;
    using de.openelp.feuerwehr.Server.Controller.Settings;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Settings;
    using de.openelp.feuerwehr.Server.Services.IncidentModules;
    using de.openelp.feuerwehr.Server.Services.Settings;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;

    public sealed class SettingsServiceTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new SqliteConnection("Data Source=:memory:");
        private readonly Mock<IUserContextAccessor> _user = new Mock<IUserContextAccessor>();
        private SettingsTestContext _db = null!;
        private SettingsService _service = null!;
        private ServiceProvider _services = null!;
        private SettingsCatalog _catalog = null!;

        public async Task InitializeAsync()
        {
            await this._connection.OpenAsync();
            this._db = new SettingsTestContext(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseSqlite(this._connection).Options);
            await this._db.Database.EnsureCreatedAsync();
            this._db.Users.AddRange(
                new ApplicationUser { Id = "admin-1", FirstName = "Ada", LastName = "Muster" },
                new ApplicationUser { Id = "admin-2", FirstName = "Max", LastName = "Beispiel" });
            await this._db.SaveChangesAsync();
            this._user.SetupGet(x => x.IsAdmin).Returns(true);
            this._user.SetupGet(x => x.UserId).Returns("admin-1");
            this._catalog = new SettingsCatalog([typeof(TestSettingsModule)]);
            ServiceCollection services = new ServiceCollection();
            foreach (Type validator in this._catalog.Definitions.Select(x => x.ValidatorType))
            {
                services.AddScoped(validator);
            }

            this._services = services.BuildServiceProvider();
            this._service = new SettingsService(this._db, this._user.Object, this._catalog, this._services);
        }

        [Fact]
        public async Task CreatesUpdatesListsAndDeletesGlobalSettingsWithEditorMetadataAsync()
        {
            Assert.Empty(Assert.IsType<List<SettingDto>>((await this._service.GetAsync(default)).Value));
            DateTime before = DateTime.UtcNow;
            SettingDto created = Assert.IsType<SettingDto>((await this._service.CreateAsync(new CreateSettingRequest
            {
                Key = "TEST_EMAIL", Value = "hydranten@example.org",
            }, default)).Value);
            Assert.Equal("admin-1", created.ModifiedBy);
            Assert.Equal("Ada Muster", created.ModifiedByName);
            Assert.InRange(created.ModifiedOn, before, DateTime.UtcNow);

            this._user.SetupGet(x => x.UserId).Returns("admin-2");
            SettingDto updated = Assert.IsType<SettingDto>((await this._service.UpdateAsync(created.Id,
                new UpdateSettingRequest { Value = "pruefung@example.org" }, default)).Value);
            Assert.Equal("admin-2", updated.ModifiedBy);
            Assert.Equal("Max Beispiel", updated.ModifiedByName);
            Assert.True(updated.ModifiedOn >= created.ModifiedOn);
            this._db.ChangeTracker.Clear();
            SettingDto listed = Assert.Single(Assert.IsType<List<SettingDto>>((await this._service.GetAsync(default)).Value));
            Assert.Equal("pruefung@example.org", listed.Value);
            Assert.Equal(updated, listed);
            Assert.Equal(204, (await this._service.DeleteAsync(created.Id, default)).Code);
            Assert.Empty(await this._db.Settings.ToListAsync());
        }

        [Theory]
        [InlineData(SettingType.Text, "")]
        [InlineData(SettingType.Text, "   ")]
        [InlineData(SettingType.Number, "0")]
        [InlineData(SettingType.Number, "000")]
        [InlineData(SettingType.Number, "-1")]
        [InlineData(SettingType.Number, "+1")]
        [InlineData(SettingType.Number, "1.5")]
        [InlineData(SettingType.Number, "1,5")]
        [InlineData(SettingType.Number, "1e3")]
        [InlineData(SettingType.Number, " 1")]
        [InlineData(SettingType.Email, "invalid")]
        [InlineData(SettingType.Email, "one@example.org,two@example.org")]
        [InlineData(SettingType.Email, "one@example.org;two@example.org")]
        [InlineData(SettingType.Email, "Name <one@example.org>")]
        [InlineData((SettingType)99, "value")]
        public async Task RejectsInvalidValuesWithoutPersistingAsync(SettingType type, string value)
        {
            Assert.Equal(400, (await this._service.CreateAsync(new CreateSettingRequest { Key = KeyFor(type), Value = value }, default)).Code);
            Assert.Empty(await this._db.Settings.ToListAsync());
        }

        [Theory]
        [InlineData(SettingType.Text, "Ein Text")]
        [InlineData(SettingType.Number, "1")]
        [InlineData(SettingType.Number, "999999999999999999999999999999999999999999999999999")]
        [InlineData(SettingType.Email, "one@example.org")]
        public async Task AcceptsSupportedTypedValuesAsync(SettingType type, string value)
        {
            Assert.Equal(201, (await this._service.CreateAsync(new CreateSettingRequest { Key = KeyFor(type), Value = value }, default)).Code);
        }

        [Fact]
        public async Task RejectsUnknownParametersAndIgnoresClientSuppliedTypeAsync()
        {
            Assert.Equal(400, (await this._service.CreateAsync(new CreateSettingRequest { Key = "UNKNOWN", Value = "1" }, default)).Code);
            CreateSettingRequest request = JsonSerializer.Deserialize<CreateSettingRequest>(
                "{\"key\":\"TEST_EMAIL\",\"value\":\"not an email\",\"type\":0}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(400, (await this._service.CreateAsync(request, default)).Code);
        }

        [Fact]
        public async Task RejectsInvalidUpdatesAndKeepsNameAndTypeImmutableAsync()
        {
            SettingDto created = Assert.IsType<SettingDto>((await this._service.CreateAsync(new CreateSettingRequest
            { Key = "TEST_NUMBER", Value = "1" }, default)).Value);
            Assert.Equal(400, (await this._service.UpdateAsync(created.Id, new UpdateSettingRequest { Value = "0" }, default)).Code);
            UpdateSettingRequest request = JsonSerializer.Deserialize<UpdateSettingRequest>(
                "{\"value\":\"2\",\"name\":\"Umbenannt\",\"type\":0,\"modifiedBy\":\"admin-2\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(200, (await this._service.UpdateAsync(created.Id, request, default)).Code);
            Setting stored = await this._db.Settings.SingleAsync();
            Assert.Equal("TEST_NUMBER", stored.Name);
            Assert.Equal(SettingType.Number, stored.Type);
            Assert.Equal("2", stored.Value);
            Assert.Equal("admin-1", stored.ModifiedBy);
        }

        [Fact]
        public async Task RejectsDuplicatesAndMissingNamesAndHandlesMissingIdsAsync()
        {
            CreateSettingRequest request = new CreateSettingRequest { Key = "TEST_TEXT", Value = "Wert" };
            Assert.Equal(201, (await this._service.CreateAsync(request, default)).Code);
            Assert.Equal(409, (await this._service.CreateAsync(request, default)).Code);
            request.Key = " ";
            Assert.Equal(400, (await this._service.CreateAsync(request, default)).Code);
            Assert.Equal(404, (await this._service.UpdateAsync(Guid.NewGuid(), new UpdateSettingRequest { Value = "Wert" }, default)).Code);
            Assert.Equal(404, (await this._service.DeleteAsync(Guid.NewGuid(), default)).Code);
        }

        [Fact]
        public async Task DatabaseEnforcesUniqueNamesAndKeepsSettingsWhenAnEditorIsDeletedAsync()
        {
            Assert.Equal(201, (await this._service.CreateAsync(new CreateSettingRequest
            { Key = "TEST_TEXT", Value = "Wert" }, default)).Code);
            this._db.Settings.Add(new Setting { Id = Guid.NewGuid(), Name = "TEST_TEXT", Value = "Duplikat", ModifiedOn = DateTime.UtcNow });
            await Assert.ThrowsAsync<DbUpdateException>(() => this._db.SaveChangesAsync());
            this._db.ChangeTracker.Clear();
            ApplicationUser editor = await this._db.Users.SingleAsync(x => x.Id == "admin-1");
            this._db.Users.Remove(editor);
            await this._db.SaveChangesAsync();
            this._db.ChangeTracker.Clear();
            Assert.Null((await this._db.Settings.SingleAsync()).ModifiedBy);
        }

        [Fact]
        public async Task DeniesEveryServiceOperationToNonAdminsAndProtectsEveryEndpointAsync()
        {
            this._user.SetupGet(x => x.IsAdmin).Returns(false);
            Assert.Equal(403, (await this._service.GetAsync(default)).Code);
            Assert.Equal(403, (await this._service.GetDefinitionsAsync(default)).Code);
            Assert.Equal(403, (await this._service.CreateAsync(new CreateSettingRequest(), default)).Code);
            Assert.Equal(403, (await this._service.UpdateAsync(Guid.NewGuid(), new UpdateSettingRequest(), default)).Code);
            Assert.Equal(403, (await this._service.DeleteAsync(Guid.NewGuid(), default)).Code);
            Assert.Equal(PolicyNames.RequireAdmin, typeof(SettingsController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        }

        public async Task DisposeAsync()
        {
            await this._services.DisposeAsync();
            await this._db.DisposeAsync();
            await this._connection.DisposeAsync();
        }

        [Fact]
        public async Task CatalogProvidesDisplayMetadataAndIgnoresLegacyRowsAsync()
        {
            List<SettingDefinitionDto> definitions = Assert.IsType<List<SettingDefinitionDto>>((await this._service.GetDefinitionsAsync(default)).Value);
            Assert.Equal(3, definitions.Count);
            SettingDefinitionDto email = Assert.Single(definitions.Where(x => x.Key == "TEST_EMAIL"));
            Assert.True(email.Mandatory);
            Assert.Equal("Testmodul", email.ModuleName);
            this._db.Settings.Add(new Setting { Id = Guid.NewGuid(), Name = "Legacy", Value = "old", ModifiedBy = "admin-1", ModifiedOn = DateTime.UtcNow });
            await this._db.SaveChangesAsync();
            Assert.Empty(Assert.IsType<List<SettingDto>>((await this._service.GetAsync(default)).Value));
        }

        [Fact]
        public async Task MissingOrInvalidMandatoryValuesBlockTheModuleAndDeletingBlocksItAgainAsync()
        {
            SettingsRuntime runtime = new SettingsRuntime(this._db, this._catalog, this._services);
            Assert.False(Assert.Single(await runtime.GetModuleStatusesAsync()).IsAvailable);
            Assert.Null(await runtime.GetValueAsync("TEST_EMAIL"));
            SettingDto created = Assert.IsType<SettingDto>((await this._service.CreateAsync(new CreateSettingRequest
            { Key = "TEST_EMAIL", Value = "one@example.org" }, default)).Value);
            Assert.True(Assert.Single(await runtime.GetModuleStatusesAsync()).IsAvailable);
            Assert.Equal("one@example.org", await runtime.GetValueAsync("TEST_EMAIL"));
            Setting stored = await this._db.Settings.SingleAsync();
            stored.Value = "invalid";
            await this._db.SaveChangesAsync();
            Assert.False(Assert.Single(await runtime.GetModuleStatusesAsync()).IsAvailable);
            Assert.Null(await runtime.GetValueAsync("TEST_EMAIL"));
            Assert.Equal(204, (await this._service.DeleteAsync(created.Id, default)).Code);
            Assert.False(Assert.Single(await runtime.GetModuleStatusesAsync()).IsAvailable);
            Assert.DoesNotContain("one@example.org", JsonSerializer.Serialize(await runtime.GetModuleStatusesAsync()));
        }

        [Fact]
        public async Task ImportsEnvironmentValuesWithLockedInstallerAndNeverOverwritesPersistedValuesAsync()
        {
            Mock<ISettingsEnvironment> environment = new Mock<ISettingsEnvironment>();
            environment.Setup(x => x.GetValue("TEST_EMAIL")).Returns("environment@example.org");
            SettingsEnvironmentInitializer initializer = this.Initializer(environment.Object);
            DateTime before = DateTime.UtcNow;
            await initializer.InitializeAsync();
            Setting stored = await this._db.Settings.SingleAsync();
            Assert.Equal("environment@example.org", stored.Value);
            Assert.Equal(SettingsEnvironmentInitializer.InstallerId, stored.ModifiedBy);
            Assert.InRange(stored.ModifiedOn, before, DateTime.UtcNow);
            ApplicationUser installer = await this._db.Users.SingleAsync(x => x.Id == SettingsEnvironmentInitializer.InstallerId);
            Assert.Equal("Installer", installer.FullName);
            Assert.False(installer.IsActive);
            Assert.True(installer.LockoutEnabled);
            Assert.True(installer.LockoutEnd > DateTimeOffset.UtcNow);
            Assert.Null(installer.PasswordHash);
            Assert.Empty(await this._db.UserRoles.Where(x => x.UserId == installer.Id).ToListAsync());

            await this._service.UpdateAsync(stored.Id, new UpdateSettingRequest { Value = "admin@example.org" }, default);
            DateTime modified = stored.ModifiedOn;
            environment.Invocations.Clear();
            environment.Setup(x => x.GetValue("TEST_EMAIL")).Returns("changed@example.org");
            await initializer.InitializeAsync();
            environment.Verify(x => x.GetValue("TEST_EMAIL"), Times.Never);
            Assert.Equal("admin@example.org", stored.Value);
            Assert.Equal("admin-1", stored.ModifiedBy);
            Assert.Equal(modified, stored.ModifiedOn);

            await this._service.DeleteAsync(stored.Id, default);
            Assert.Empty(await this._db.Settings.ToListAsync());
            await initializer.InitializeAsync();
            Setting restored = await this._db.Settings.SingleAsync();
            Assert.Equal("changed@example.org", restored.Value);
            Assert.Equal(installer.Id, restored.ModifiedBy);
            Assert.Equal(1, await this._db.Users.CountAsync(x => x.Id == installer.Id));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("one@example.org,two@example.org")]
        public async Task MissingOrInvalidEnvironmentDoesNotCreateValuesOrInstallerAndDoesNotFailStartupAsync(string? value)
        {
            Mock<ISettingsEnvironment> environment = new Mock<ISettingsEnvironment>();
            environment.Setup(x => x.GetValue("TEST_EMAIL")).Returns(value);
            await this.Initializer(environment.Object).InitializeAsync();
            Assert.Empty(await this._db.Settings.ToListAsync());
            Assert.False(await this._db.Users.AnyAsync(x => x.Id == SettingsEnvironmentInitializer.InstallerId));
        }

        private static string KeyFor(SettingType type)
        {
            return type switch { SettingType.Email => "TEST_EMAIL", SettingType.Number => "TEST_NUMBER", SettingType.Text => "TEST_TEXT", _ => "UNKNOWN" };
        }

        [Fact]
        public async Task EmptyCatalogNeedsNoParametersAndDoesNotImportAnythingAsync()
        {
            SettingsCatalog catalog = new SettingsCatalog([]);
            SettingsService service = new SettingsService(this._db, this._user.Object, catalog, this._services);
            Assert.Empty(Assert.IsType<List<SettingDto>>((await service.GetAsync(default)).Value));
            Assert.Empty(Assert.IsType<List<SettingDefinitionDto>>((await service.GetDefinitionsAsync(default)).Value));
            Assert.Empty(await new SettingsRuntime(this._db, catalog, this._services).GetModuleStatusesAsync());
            Mock<ISettingsEnvironment> environment = new Mock<ISettingsEnvironment>(MockBehavior.Strict);
            await new SettingsEnvironmentInitializer(this._db, catalog, environment.Object, this._services,
                NullLogger<SettingsEnvironmentInitializer>.Instance).InitializeAsync();
            Assert.False(await this._db.Users.AnyAsync(x => x.Id == SettingsEnvironmentInitializer.InstallerId));
        }

        [Fact]
        public async Task UsesOnlyTheDeclaredValidatorNotATypeBasedFallbackAsync()
        {
            SettingsCatalog catalog = new SettingsCatalog([typeof(CustomValidatorSettingsModule)]);
            await using ServiceProvider services = new ServiceCollection().AddScoped<CustomSettingValidator>().BuildServiceProvider();
            SettingsService service = new SettingsService(this._db, this._user.Object, catalog, services);
            Assert.Equal(400, (await service.CreateAsync(new CreateSettingRequest { Key = "TEST_CUSTOM", Value = "valid@example.org" }, default)).Code);
            Assert.Equal(201, (await service.CreateAsync(new CreateSettingRequest { Key = "TEST_CUSTOM", Value = "approved" }, default)).Code);
            SettingsRuntime runtime = new SettingsRuntime(this._db, catalog, services);
            Assert.Equal("approved", await runtime.GetValueAsync("TEST_CUSTOM"));
            Assert.True(Assert.Single(await runtime.GetModuleStatusesAsync()).IsAvailable);
        }

        [Fact]
        public async Task InvalidEnvironmentDoesNotPreventOtherValidParametersFromBeingImportedAsync()
        {
            Mock<ISettingsEnvironment> environment = new Mock<ISettingsEnvironment>();
            environment.Setup(x => x.GetValue("TEST_EMAIL")).Returns("invalid");
            environment.Setup(x => x.GetValue("TEST_NUMBER")).Returns("12");
            await this.Initializer(environment.Object).InitializeAsync();
            Setting stored = await this._db.Settings.SingleAsync();
            Assert.Equal("TEST_NUMBER", stored.Name);
            Assert.Equal("12", stored.Value);
        }

        private SettingsEnvironmentInitializer Initializer(ISettingsEnvironment environment)
        {
            return new SettingsEnvironmentInitializer(this._db, this._catalog, environment, this._services, NullLogger<SettingsEnvironmentInitializer>.Instance);
        }
    }
}
