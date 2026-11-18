namespace de.openelp.feuerwehr.Tests.Backend.Services.HydrantModules
{
    using de.openelp.feuerwehr.Common.Models;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Migrations;
    using de.openelp.feuerwehr.Server.Models.Hydrant;
    using Microsoft.Data.Sqlite;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Migrations.Operations;

    public sealed class HydrantInspectionModelTests : IAsyncLifetime
    {
        private readonly SqliteConnection _connection = new SqliteConnection("Data Source=:memory:");
        private HydrantInspectionTestContext _db = null!;

        public async Task InitializeAsync()
        {
            await this._connection.OpenAsync();
            this._db = new HydrantInspectionTestContext(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseSqlite(this._connection).Options);
            await this._db.Database.EnsureCreatedAsync();
            this._db.Hydrants.Add(new Hydrant { Id = 42, Status = HydrantStatus.Operational });
            await this._db.SaveChangesAsync();
        }

        [Fact]
        public async Task PersistsBothStatusesAndACommentOfExactly500CharactersAsync()
        {
            HydrantInspection inspection = Inspection();
            inspection.Comment = new string('a', 500);
            this._db.HydrantInspections.Add(inspection);
            await this._db.SaveChangesAsync();
            this._db.ChangeTracker.Clear();
            HydrantInspection stored = await this._db.HydrantInspections.Include(x => x.Hydrant).SingleAsync();
            Assert.Equal(42, stored.Hydrant.Id);
            Assert.Equal(HydrantStatus.Operational, stored.PreviousStatus);
            Assert.Equal(HydrantStatus.Defective, stored.NewStatus);
            Assert.Equal(500, stored.Comment!.Length);
            Assert.Null(stored.PhotoStorageKey);
            Assert.DoesNotContain(this._db.Model.FindEntityType(typeof(HydrantInspection))!.GetProperties(), property => property.IsShadowProperty());
        }

        [Fact]
        public async Task RejectsAnOverlongCommentAsync()
        {
            HydrantInspection inspection = Inspection();
            inspection.Comment = new string('a', 501);
            this._db.HydrantInspections.Add(inspection);
            await Assert.ThrowsAsync<DbUpdateException>(() => this._db.SaveChangesAsync());
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 0)]
        [InlineData(99, 2)]
        [InlineData(1, 99)]
        public async Task RejectsMissingOrUnknownStatusesAsync(int previousStatus, int newStatus)
        {
            HydrantInspection inspection = Inspection();
            inspection.PreviousStatus = (HydrantStatus)previousStatus;
            inspection.NewStatus = (HydrantStatus)newStatus;
            this._db.HydrantInspections.Add(inspection);
            await Assert.ThrowsAsync<DbUpdateException>(() => this._db.SaveChangesAsync());
        }

        [Fact]
        public async Task PersistsOptionalJpegMetadataWithoutImageBytesAsync()
        {
            HydrantInspection inspection = Inspection();
            inspection.PhotoStorageKey = $"{Guid.NewGuid():N}.jpg";
            inspection.PhotoContentType = "image/jpeg";
            this._db.HydrantInspections.Add(inspection);
            await this._db.SaveChangesAsync();
            this._db.ChangeTracker.Clear();
            HydrantInspection stored = await this._db.HydrantInspections.SingleAsync();
            Assert.Equal(inspection.PhotoStorageKey, stored.PhotoStorageKey);
            Assert.Equal("image/jpeg", stored.PhotoContentType);
        }

        [Theory]
        [InlineData(null, "image/jpeg")]
        [InlineData("photo.jpg", null)]
        [InlineData("photo.png", "image/png")]
        [InlineData("", "image/jpeg")]
        public async Task RejectsIncompleteOrNonJpegPhotoMetadataAsync(string? key, string? contentType)
        {
            HydrantInspection inspection = Inspection();
            inspection.PhotoStorageKey = key;
            inspection.PhotoContentType = contentType;
            this._db.HydrantInspections.Add(inspection);
            await Assert.ThrowsAsync<DbUpdateException>(() => this._db.SaveChangesAsync());
        }

        [Fact]
        public async Task EnforcesTheIntegerHydrantForeignKeyAsync()
        {
            HydrantInspection inspection = Inspection();
            inspection.HydrantId = 999;
            this._db.HydrantInspections.Add(inspection);
            await Assert.ThrowsAsync<DbUpdateException>(() => this._db.SaveChangesAsync());
        }

        [Fact]
        public void MigrationRenamesStatusAndPreservesTheRealHydrantReference()
        {
            ConfigureHydrantInspections migration = new ConfigureHydrantInspections();
            Assert.Contains(migration.UpOperations.OfType<RenameColumnOperation>(), operation => operation.Name == "Status" && operation.NewName == "NewStatus");
            Assert.Contains(migration.UpOperations.OfType<RenameColumnOperation>(), operation => operation.Name == "HydrantId1" && operation.NewName == "HydrantId");
            AddColumnOperation previousStatus = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>().Where(operation => operation.Name == "PreviousStatus"));
            Assert.False(previousStatus.IsNullable);
            Assert.Null(previousStatus.DefaultValue);
            Assert.DoesNotContain(migration.UpOperations.OfType<AlterColumnOperation>(), operation => operation.Name == "HydrantId");
        }

        public async Task DisposeAsync()
        {
            await this._db.DisposeAsync();
            await this._connection.DisposeAsync();
        }

        private static HydrantInspection Inspection()
        {
            return new HydrantInspection
            {
                Id = Guid.NewGuid(), HydrantId = 42, InspectorId = "inspector", InspectorName = "Max Muster",
                InspectedOn = new DateOnly(2026, 11, 18), InspectedAtUtc = DateTime.UtcNow,
                PreviousStatus = HydrantStatus.Operational, NewStatus = HydrantStatus.Defective,
            };
        }
    }
}
