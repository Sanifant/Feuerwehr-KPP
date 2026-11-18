namespace de.openelp.feuerwehr.Tests.Backend.Services.HydrantModules
{
    using System.Text.Json;
    using de.openelp.feuerwehr.Common.Models;
    using de.openelp.feuerwehr.Common.Models.HydrantInspections;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Hydrant;
    using de.openelp.feuerwehr.Server.Services;
    using de.openelp.feuerwehr.Server.Services.HydrantModules;
    using de.openelp.feuerwehr.Server.Services.IncidentModules;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Moq;

    public sealed class HydrantInspectionServiceTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new SqliteConnection("Data Source=:memory:");
        private readonly Mock<IUserContextAccessor> _user = new Mock<IUserContextAccessor>();
        private readonly Mock<TimeProvider> _clock = new Mock<TimeProvider>();
        private readonly DateTimeOffset _now = new DateTimeOffset(2026, 11, 18, 12, 30, 0, TimeSpan.Zero);
        private HydrantInspectionTestContext _db = null!;
        private HydrantService _service = null!;

        public async Task InitializeAsync()
        {
            await this._connection.OpenAsync();
            this._db = new HydrantInspectionTestContext(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseSqlite(this._connection).Options);
            await this._db.Database.EnsureCreatedAsync();
            this._db.Users.Add(new ApplicationUser { Id = "inspector", FirstName = "Ada", LastName = "Muster" });
            this._db.Hydrants.AddRange(new Hydrant { Id = 42, Status = HydrantStatus.Operational }, new Hydrant { Id = 77, Status = HydrantStatus.Blocked });
            await this._db.SaveChangesAsync();
            this._user.SetupGet(x => x.UserId).Returns("inspector");
            this._user.SetupGet(x => x.DisplayName).Returns("Untrusted claim display name");
            this._clock.Setup(x => x.GetUtcNow()).Returns(this._now);
            this._service = new HydrantService(this._db, this._user.Object, this._clock.Object);
        }

        [Theory]
        [InlineData(HydrantStatus.Operational)]
        [InlineData(HydrantStatus.Defective)]
        [InlineData(HydrantStatus.Blocked)]
        [InlineData(HydrantStatus.UnderMaintenance)]
        public async Task RecordsEachStatusIncludingAnUnchangedStatusAndAnOptionalCommentAsync(HydrantStatus status)
        {
            ServiceResult result = this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = status });
            Assert.Equal(201, result.Code);
            HydrantInspectionDto inspection = Assert.IsType<HydrantInspectionDto>(result.Value);
            Assert.NotEqual(Guid.Empty, inspection.Id);
            Assert.Equal(42, inspection.HydrantId);
            Assert.Equal(HydrantStatus.Operational, inspection.PreviousStatus);
            Assert.Equal(status, inspection.NewStatus);
            Assert.Equal("inspector", inspection.InspectorId);
            Assert.Equal("Ada Muster", inspection.InspectorName);
            Assert.Equal(this._now.UtcDateTime, inspection.InspectedAtUtc);
            Assert.Equal(new DateOnly(2026, 11, 18), inspection.InspectedOn);
            Assert.Null(inspection.Comment);
            this._db.ChangeTracker.Clear();
            Assert.Equal(status, (await this._db.Hydrants.FindAsync(42))!.Status);
            Assert.Equal(inspection.Id, (await this._db.HydrantInspections.SingleAsync()).Id);
        }

        [Fact]
        public void DoesNotAcceptClientSuppliedIdentityTimestampPreviousStatusPhotoOrHydrantId()
        {
            const string payload = "{\"newStatus\":2,\"comment\":\"Dichtung defekt\",\"id\":\"00000000-0000-0000-0000-000000000001\",\"hydrantId\":77,\"inspectorId\":\"other-user\",\"inspectorName\":\"Someone else\",\"inspectedAtUtc\":\"2000-01-01T00:00:00Z\",\"previousStatus\":3,\"photoStorageKey\":\"untrusted.jpg\",\"photoContentType\":\"image/jpeg\"}";
            CreateHydrantInspectionRequest request = JsonSerializer.Deserialize<CreateHydrantInspectionRequest>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            HydrantInspectionDto result = Assert.IsType<HydrantInspectionDto>(this._service.Inspect(42, request).Value);
            Assert.Equal(42, result.HydrantId);
            Assert.Equal("inspector", result.InspectorId);
            Assert.Equal("Ada Muster", result.InspectorName);
            Assert.Equal(this._now.UtcDateTime, result.InspectedAtUtc);
            Assert.Equal(HydrantStatus.Operational, result.PreviousStatus);
            Assert.Equal("Dichtung defekt", result.Comment);
            Assert.Null(result.PhotoStorageKey);
            Assert.Null(result.PhotoContentType);
            Assert.Equal(HydrantStatus.Blocked, this._db.Hydrants.Find(77)!.Status);
        }

        [Fact]
        public async Task PersistsA500CharacterCommentAndRejectsLongerCommentsOrUnknownStatusesAsync()
        {
            Assert.Equal(201, this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Operational, Comment = new string('a', 500) }).Code);
            Assert.Equal(400, this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Defective, Comment = new string('a', 501) }).Code);
            foreach (int invalidStatus in new[] { 0, 99 })
            {
                Assert.Equal(400, this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = (HydrantStatus)invalidStatus }).Code);
            }

            Assert.Equal(HydrantStatus.Operational, this._db.Hydrants.Find(42)!.Status);
            Assert.Single(await this._db.HydrantInspections.ToListAsync());
        }

        [Fact]
        public void HandlesUnknownHydrantsAndMissingUsersWithoutSaving()
        {
            Assert.Equal(404, this._service.Inspect(999, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Operational }).Code);
            Assert.Equal(404, this._service.GetInspections(999).Code);
            this._user.SetupGet(x => x.UserId).Returns("missing");
            Assert.Equal(401, this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Defective }).Code);
            Assert.Empty(this._db.HydrantInspections);
        }

        [Fact]
        public void ReturnsOnlyTheSelectedHydrantHistoryNewestFirst()
        {
            HydrantInspectionDto first = Assert.IsType<HydrantInspectionDto>(this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Defective }).Value);
            this._clock.Setup(x => x.GetUtcNow()).Returns(this._now.AddHours(1));
            HydrantInspectionDto second = Assert.IsType<HydrantInspectionDto>(this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.UnderMaintenance }).Value);
            this._service.Inspect(77, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Operational });
            List<HydrantInspectionDto> history = Assert.IsType<List<HydrantInspectionDto>>(this._service.GetInspections(42).Value);
            Assert.Equal(new[] { second.Id, first.Id }, history.Select(x => x.Id));
            Assert.Equal(HydrantStatus.Defective, second.PreviousStatus);
        }

        [Fact]
        public async Task RollsBackTheInspectionIfUpdatingTheHydrantFailsAsync()
        {
            await this._db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_hydrant_status BEFORE UPDATE OF Status ON Hydrants BEGIN SELECT RAISE(ABORT, 'Simulated status update failure'); END;");
            Assert.Throws<DbUpdateException>(() => this._service.Inspect(42, new CreateHydrantInspectionRequest { NewStatus = HydrantStatus.Defective }));
            this._db.ChangeTracker.Clear();
            Assert.Equal(HydrantStatus.Operational, (await this._db.Hydrants.FindAsync(42))!.Status);
            Assert.Empty(await this._db.HydrantInspections.ToListAsync());
        }

        public async Task DisposeAsync()
        {
            await this._db.DisposeAsync();
            await this._connection.DisposeAsync();
        }
    }
}
