using System.Security.Claims;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services;

public sealed class VehicleTransferTests
{
    [Fact]
    /// <summary>
    /// Executes import Timestamps Only Actual Status Changes And Rolls Them Back With An Invalid Batch.
    /// </summary>
    public async Task ImportTimestampsOnlyActualStatusChangesAndRollsThemBackWithAnInvalidBatch()
    {
        await using var fixture = new Fixture();
        const string csv = "radioCallSign;fireDepartment;equipmentOfficerEmail;status\nFlorian 19-01;Feuerwehr A;owner@example.test;Betriebsbereit";
        using (var stream = Csv(csv)) Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
        var initial = fixture.Clock.GetUtcNow().UtcDateTime;
        Assert.Equal(initial, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
        using (var stream = Csv(csv)) Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
        Assert.Equal(initial, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
        using (var stream = Csv(csv.Replace("Betriebsbereit", "Wartung"))) Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
        var changedAt = fixture.Clock.GetUtcNow().UtcDateTime;
        Assert.Equal(changedAt, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
        fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
        using (var stream = Csv(csv.Replace("Betriebsbereit", "Abgemeldet") + "\nFlorian 19-02;Feuerwehr A;missing@example.test;Wartung"))
            Assert.Equal(400, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
        Assert.Equal(changedAt, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
    }

    [Fact]
    /// <summary>
    /// Executes csv Round Trip Resolves Officer Email And Preserves Vehicle Identity Bookings And Quoted Text.
    /// </summary>
    public async Task CsvRoundTripResolvesOfficerEmailAndPreservesVehicleIdentityBookingsAndQuotedText()
    {
        await using var fixture = new Fixture();
        var vehicle = fixture.Vehicle();
        vehicle.Name = "=SUM(1;2)\n\"Ãœbung\"";
        var booking = new VehicleReservation { Id = Guid.NewGuid(), VehicleId = vehicle.Id, CommandId = Guid.NewGuid(),
            From = new DateOnly(2026, 11, 1), To = new DateOnly(2026, 11, 1), Status = VehicleReservationStatus.Confirmed };
        fixture.Db.VehicleReservations.Add(booking);
        fixture.Db.VehicleBookingDays.Add(new VehicleBookingDay { VehicleId = vehicle.Id, ReservationId = booking.Id, Date = booking.From });
        await fixture.Db.SaveChangesAsync();
        var exported = await fixture.Service.Export(VehicleTransferKind.Vehicles, default);
        var csv = Encoding.UTF8.GetString(exported.Content!);
        Assert.Contains("equipmentOfficerEmail", csv);
        Assert.Contains("owner@example.test", csv);
        Assert.DoesNotContain("equipmentOfficerId", csv);
        Assert.Contains("'=SUM", csv);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.Replace("owner@example.test", " OTHER@EXAMPLE.TEST ").Replace("Betriebsbereit", "Wartung")));
        var imported = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
        var result = Assert.IsType<VehicleImportResponse>(imported.Value);
        Assert.Equal(200, imported.Code); Assert.Equal(0, result.CreatedCount); Assert.Equal(1, result.UpdatedCount);
        var stored = Assert.Single(await fixture.Db.Vehicles.AsNoTracking().ToListAsync());
        Assert.Equal(vehicle.Id, stored.Id);
        Assert.Equal("other", stored.EquipmentOfficerId);
        Assert.Equal(vehicle.Name, stored.Name);
        Assert.Equal(VehicleStatusDefaults.MaintenanceId, stored.VehicleStatusId);
        Assert.Single(fixture.Db.VehicleReservations);
        Assert.Single(fixture.Db.VehicleBookingDays);
    }

    [Theory]
    [InlineData("missing@example.test")]
    [InlineData("viewer@example.test")]
    [InlineData("inactive@example.test")]
    [InlineData("foreign@example.test")]
    /// <summary>
    /// Executes invalid Officer Rejects The Whole File Without Creating Users Or Partial Vehicles.
    /// </summary>
    /// <param name="email">The email.</param>
    public async Task InvalidOfficerRejectsTheWholeFileWithoutCreatingUsersOrPartialVehicles(string email)
    {
        await using var fixture = new Fixture();
        using var stream = Csv("radioCallSign;fireDepartment;equipmentOfficerEmail\nFlorian 19-01;Feuerwehr A;owner@example.test\nFlorian 19-02;Feuerwehr A;" + email);
        var usersBefore = await fixture.Db.Users.CountAsync();
        var result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
        Assert.Equal(400, result.Code);
        Assert.Contains(Assert.IsType<VehicleImportResponse>(result.Value).Errors, x => x.LineNumber == 3 && x.Description.Contains("Gerätewart"));
        Assert.Empty(fixture.Db.Vehicles);
        Assert.Equal(usersBefore, await fixture.Db.Users.CountAsync());
    }

    [Theory]
    [InlineData("radioCallSign;radioCallSign;fireDepartment;equipmentOfficerEmail\nA;A;Feuerwehr A;owner@example.test", "mehrfach")]
    [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail\n\"Nicht geschlossen;Feuerwehr A;owner@example.test", "CSV-Zeile")]
    [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail;vehicleType\nA;Feuerwehr A;owner@example.test;Unbekannt", "Fahrzeugtyp")]
    [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail;status\nA;Feuerwehr A;owner@example.test;Unbekannt", "Status")]
    /// <summary>
    /// Executes invalid Files Have Actionable Errors And Do Not Change Data.
    /// </summary>
    /// <param name="csv">The csv.</param>
    /// <param name="expected">The expected.</param>
    public async Task InvalidFilesHaveActionableErrorsAndDoNotChangeData(string csv, string expected)
    {
        await using var fixture = new Fixture(); using var stream = Csv(csv);
        var result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
        Assert.Equal(400, result.Code);
        Assert.Contains(Assert.IsType<VehicleImportResponse>(result.Value).Errors, x => x.Description.Contains(expected));
        Assert.Empty(fixture.Db.Vehicles);
    }

    [Fact]
    /// <summary>
    /// Executes officers Only Export Assigned Vehicles And Cannot Import Updates For Other Officers.
    /// </summary>
    public async Task OfficersOnlyExportAssignedVehiclesAndCannotImportUpdatesForOtherOfficers()
    {
        await using var fixture = new Fixture();
        var owned = fixture.Vehicle();
        var other = fixture.Vehicle("Florian 19-02", "other");
        await fixture.Db.SaveChangesAsync();
        fixture.As("owner", Roles.EquipmentOfficer);
        var export = await fixture.Service.Export(VehicleTransferKind.Vehicles, default);
        var csv = Encoding.UTF8.GetString(export.Content!);
        Assert.Contains(owned.RadioCallSign, csv); Assert.DoesNotContain(other.RadioCallSign, csv);
        using var stream = Csv($"id;radioCallSign;fireDepartment;equipmentOfficerEmail\n{other.Id};{other.RadioCallSign};Feuerwehr A;owner@example.test");
        Assert.Equal(403, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
        Assert.Equal("other", (await fixture.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == other.Id)).EquipmentOfficerId);
        using var types = Csv("[{\"name\":\"ELW\"}]");
        Assert.Equal(403, (await fixture.Service.Import(VehicleTransferKind.Types, types, default)).Code);
        Assert.Equal(403, (await fixture.Service.Export(VehicleTransferKind.Types, default)).Code);
        fixture.As("viewer", Roles.Viewer);
        Assert.Equal(403, (await fixture.Service.Export(VehicleTransferKind.Vehicles, default)).Code);
    }

    [Fact]
    /// <summary>
    /// Executes reimport Without Ids Updates By Department And Call Sign And Missing Columns Preserve Values.
    /// </summary>
    public async Task ReimportWithoutIdsUpdatesByDepartmentAndCallSignAndMissingColumnsPreserveValues()
    {
        await using var fixture = new Fixture();
        var vehicle = fixture.Vehicle(); vehicle.Name = "Bestehender Name"; vehicle.IsActive = false;
        vehicle.VehicleStatusId = VehicleStatusDefaults.DeregisteredId;
        await fixture.Db.SaveChangesAsync();
        using var stream = Csv($"radioCallSign;fireDepartment;equipmentOfficerEmail\n{vehicle.RadioCallSign};Feuerwehr A;owner@example.test");
        var result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
        Assert.Equal(200, result.Code);
        var stored = Assert.Single(await fixture.Db.Vehicles.AsNoTracking().ToListAsync());
        Assert.Equal(vehicle.Id, stored.Id); Assert.Equal("Bestehender Name", stored.Name); Assert.False(stored.IsActive);
        Assert.Equal(VehicleStatusDefaults.DeregisteredId, stored.VehicleStatusId);
    }

    [Fact]
    /// <summary>
    /// Executes vehicle Types Round Trip And Rename In Place While Duplicate Names Rollback The Entire Batch.
    /// </summary>
    public async Task VehicleTypesRoundTripAndRenameInPlaceWhileDuplicateNamesRollbackTheEntireBatch()
    {
        await using var fixture = new Fixture();
        var vehicle = fixture.Vehicle(); await fixture.Db.SaveChangesAsync();
        var exported = await fixture.Service.Export(VehicleTransferKind.Types, default);
        var rows = JsonSerializer.Deserialize<List<VehicleTypeImportDto>>(exported.Content!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var type = Assert.Single(rows); type.Name = "Mannschaftstransportwagen";
        using var updated = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(rows));
        Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Types, updated, default)).Code);
        Assert.Equal(type.Id, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == vehicle.Id)).VehicleTypeId);
        Assert.Equal(type.Name, (await fixture.Db.Set<VehicleType>().SingleAsync()).Name);
        using var invalid = Csv("[{\"name\":\"ELW\"},{\"name\":\"elw\"}]");
        Assert.Equal(400, (await fixture.Service.Import(VehicleTransferKind.Types, invalid, default)).Code);
        Assert.Single(fixture.Db.Set<VehicleType>());
    }

    private static MemoryStream Csv(string text) => new(Encoding.UTF8.GetBytes(text));
    private sealed class Fixture : IAsyncDisposable
    {
        public FeuerwehrDbContext Db { get; } = new(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public VehicleTransferService Service { get; }
        public TestClock Clock { get; } = new();
        private readonly HttpContextAccessor http = new() { HttpContext = new DefaultHttpContext() };
        private readonly Guid typeId = Guid.NewGuid();
        public Fixture()
        {
            Db.Database.EnsureCreated();
            Db.FireDepartments.AddRange(new FireDepartment { Id = 1, Name = "Feuerwehr A" }, new FireDepartment { Id = 2, Name = "Feuerwehr B" });
            Db.Set<VehicleType>().Add(new VehicleType { Id = typeId, Name = "MTW", NormalizedName = "MTW" });
            Db.Roles.Add(new IdentityRole { Id = "equipment-role", Name = Roles.EquipmentOfficer });
            foreach (var id in new[] { "admin", "owner", "other", "viewer", "inactive", "foreign" })
            {
                Db.Users.Add(new ApplicationUser { Id = id, Email = id + "@example.test", UserName = id, IsActive = id != "inactive", FireDepartmentId = id == "foreign" ? 2 : 1 });
                if (id is "owner" or "other" or "inactive" or "foreign") Db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = "equipment-role" });
            }
            Db.SaveChanges();
            Service = new VehicleTransferService(Db, http, new UpperInvariantLookupNormalizer(), Clock);
            As("admin", Roles.Admin);
        }
        /// <summary>
        /// Executes as.
        /// </summary>
        /// <param name="id">The id.</param>
        public void As(string id, string role) => http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "test"));
        /// <summary>
        /// Executes vehicle.
        /// </summary>
        /// <param name="callSign">The call Sign.</param>
        /// <param name="owner">The owner.</param>
        public Vehicle Vehicle(string callSign = "Florian 19-01", string owner = "owner")
        {
            var vehicle = new Vehicle { Id = Guid.NewGuid(), RadioCallSign = callSign, FireDepartmentId = 1, EquipmentOfficerId = owner, VehicleTypeId = typeId };
            Db.Vehicles.Add(vehicle); return vehicle;
        }
        /// <summary>
        /// Executes dispose.
        /// </summary>
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
        /// <summary>
        /// Gets the utc Now.
        /// </summary>
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}

