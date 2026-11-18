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
    using Moq;

    public sealed class SettingsServiceTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new SqliteConnection("Data Source=:memory:");
        private readonly Mock<IUserContextAccessor> _user = new Mock<IUserContextAccessor>();
        private SettingsTestContext _db = null!;
        private SettingsService _service = null!;

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
            this._service = new SettingsService(this._db, this._user.Object);
        }

        [Fact]
        public async Task CreatesUpdatesListsAndDeletesGlobalSettingsWithEditorMetadataAsync()
        {
            Assert.Empty(Assert.IsType<List<SettingDto>>((await this._service.GetAsync(default)).Value));
            DateTime before = DateTime.UtcNow;
            SettingDto created = Assert.IsType<SettingDto>((await this._service.CreateAsync(new CreateSettingRequest
            {
                Name = "Hydrant Benachrichtigung", Type = SettingType.Email, Value = "hydranten@example.org",
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
            Assert.Equal(400, (await this._service.CreateAsync(new CreateSettingRequest { Name = "Parameter", Type = type, Value = value }, default)).Code);
            Assert.Empty(await this._db.Settings.ToListAsync());
        }

        [Theory]
        [InlineData(SettingType.Text, "Ein Text")]
        [InlineData(SettingType.Number, "1")]
        [InlineData(SettingType.Number, "999999999999999999999999999999999999999999999999999")]
        [InlineData(SettingType.Email, "one@example.org")]
        public async Task AcceptsSupportedTypedValuesAsync(SettingType type, string value)
        {
            Assert.Equal(201, (await this._service.CreateAsync(new CreateSettingRequest { Name = "Parameter", Type = type, Value = value }, default)).Code);
        }

        [Fact]
        public async Task RequiresEmailTypeForHydrantNotificationsAsync()
        {
            foreach (SettingType type in new[] { SettingType.Text, SettingType.Number })
            {
                Assert.Equal(400, (await this._service.CreateAsync(new CreateSettingRequest
                { Name = "Hydrant Benachrichtigung", Type = type, Value = "1" }, default)).Code);
            }
        }

        [Fact]
        public async Task RejectsInvalidUpdatesAndKeepsNameAndTypeImmutableAsync()
        {
            SettingDto created = Assert.IsType<SettingDto>((await this._service.CreateAsync(new CreateSettingRequest
            { Name = "Anzahl", Type = SettingType.Number, Value = "1" }, default)).Value);
            Assert.Equal(400, (await this._service.UpdateAsync(created.Id, new UpdateSettingRequest { Value = "0" }, default)).Code);
            UpdateSettingRequest request = JsonSerializer.Deserialize<UpdateSettingRequest>(
                "{\"value\":\"2\",\"name\":\"Umbenannt\",\"type\":0,\"modifiedBy\":\"admin-2\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal(200, (await this._service.UpdateAsync(created.Id, request, default)).Code);
            Setting stored = await this._db.Settings.SingleAsync();
            Assert.Equal("Anzahl", stored.Name);
            Assert.Equal(SettingType.Number, stored.Type);
            Assert.Equal("2", stored.Value);
            Assert.Equal("admin-1", stored.ModifiedBy);
        }

        [Fact]
        public async Task RejectsDuplicatesAndMissingNamesAndHandlesMissingIdsAsync()
        {
            CreateSettingRequest request = new CreateSettingRequest { Name = "Parameter", Type = SettingType.Text, Value = "Wert" };
            Assert.Equal(201, (await this._service.CreateAsync(request, default)).Code);
            Assert.Equal(409, (await this._service.CreateAsync(request, default)).Code);
            request.Name = " ";
            Assert.Equal(400, (await this._service.CreateAsync(request, default)).Code);
            Assert.Equal(404, (await this._service.UpdateAsync(Guid.NewGuid(), new UpdateSettingRequest { Value = "Wert" }, default)).Code);
            Assert.Equal(404, (await this._service.DeleteAsync(Guid.NewGuid(), default)).Code);
        }

        [Fact]
        public async Task DatabaseEnforcesUniqueNamesAndKeepsSettingsWhenAnEditorIsDeletedAsync()
        {
            Assert.Equal(201, (await this._service.CreateAsync(new CreateSettingRequest
            { Name = "Parameter", Type = SettingType.Text, Value = "Wert" }, default)).Code);
            this._db.Settings.Add(new Setting { Id = Guid.NewGuid(), Name = "Parameter", Value = "Duplikat", ModifiedOn = DateTime.UtcNow });
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
            Assert.Equal(403, (await this._service.CreateAsync(new CreateSettingRequest(), default)).Code);
            Assert.Equal(403, (await this._service.UpdateAsync(Guid.NewGuid(), new UpdateSettingRequest(), default)).Code);
            Assert.Equal(403, (await this._service.DeleteAsync(Guid.NewGuid(), default)).Code);
            Assert.Equal(PolicyNames.RequireAdmin, typeof(SettingsController).GetCustomAttribute<AuthorizeAttribute>()?.Policy);
        }

        public async Task DisposeAsync()
        {
            await this._db.DisposeAsync();
            await this._connection.DisposeAsync();
        }
    }
}
