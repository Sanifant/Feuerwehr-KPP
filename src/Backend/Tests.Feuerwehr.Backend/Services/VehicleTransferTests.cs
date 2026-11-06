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

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    /// <summary>
    /// Tests vehicle transfer behavior and its expected outcomes.
    /// </summary>
    public sealed class VehicleTransferTests
    {
        /// <summary>
        /// Verifies that import timestamps only actual status changes and rolls them back with an invalid batch.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ImportTimestampsOnlyActualStatusChangesAndRollsThemBackWithAnInvalidBatch()
        {
            await using Fixture fixture = new ();
            const string csv = "radioCallSign;fireDepartment;equipmentOfficerEmail;status\nFlorian 19-01;Feuerwehr A;owner@example.test;Betriebsbereit";
            using (MemoryStream stream = Csv(csv))
            {
                Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
            }

            DateTime initial = fixture.Clock.GetUtcNow().UtcDateTime;
            Assert.Equal(initial, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            using (MemoryStream stream = Csv(csv))
            {
                Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
            }

            Assert.Equal(initial, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            using (MemoryStream stream = Csv(csv.Replace("Betriebsbereit", "Wartung")))
            {
                Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
            }

            DateTime changedAt = fixture.Clock.GetUtcNow().UtcDateTime;
            Assert.Equal(changedAt, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            using (MemoryStream stream = Csv(csv.Replace("Betriebsbereit", "Abgemeldet") + "\nFlorian 19-02;Feuerwehr A;missing@example.test;Wartung"))
            {
                Assert.Equal(400, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
            }

            Assert.Equal(changedAt, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync()).StatusChangedAtUtc);
        }

        /// <summary>
        /// Verifies that CSV round trip resolves officer email and preserves vehicle identity bookings and quoted text.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task CsvRoundTripResolvesOfficerEmailAndPreservesVehicleIdentityBookingsAndQuotedText()
        {
            await using Fixture fixture = new ();
            Vehicle vehicle = fixture.Vehicle();
            vehicle.Name = "=SUM(1;2)\n\"Ãœbung\"";
            VehicleReservation booking = new ()
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicle.Id,
                CommandId = Guid.NewGuid(),
                From = new DateOnly(2026, 11, 1),
                To = new DateOnly(2026, 11, 1),
                Status = VehicleReservationStatus.Confirmed
            };
            _ = fixture.Db.VehicleReservations.Add(booking);
            _ = fixture.Db.VehicleBookingDays.Add(new VehicleBookingDay { VehicleId = vehicle.Id, ReservationId = booking.Id, Date = booking.From });
            _ = await fixture.Db.SaveChangesAsync();
            ServiceResult exported = await fixture.Service.Export(VehicleTransferKind.Vehicles, default);
            string csv = Encoding.UTF8.GetString(exported.Content!);
            Assert.Contains("equipmentOfficerEmail", csv);
            Assert.Contains("owner@example.test", csv);
            Assert.DoesNotContain("equipmentOfficerId", csv);
            Assert.Contains("'=SUM", csv);
            using MemoryStream stream = new (Encoding.UTF8.GetBytes(csv.Replace("owner@example.test", " OTHER@EXAMPLE.TEST ").Replace("Betriebsbereit", "Wartung")));
            ServiceResult imported = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
            VehicleImportResponse result = Assert.IsType<VehicleImportResponse>(imported.Value);
            Assert.Equal(200, imported.Code);
            Assert.Equal(0, result.CreatedCount);
            Assert.Equal(1, result.UpdatedCount);
            Vehicle stored = Assert.Single(await fixture.Db.Vehicles.AsNoTracking().ToListAsync());
            Assert.Equal(vehicle.Id, stored.Id);
            Assert.Equal("other", stored.EquipmentOfficerId);
            Assert.Equal(vehicle.Name, stored.Name);
            Assert.Equal(VehicleStatusDefaults.MaintenanceId, stored.VehicleStatusId);
            _ = Assert.Single(fixture.Db.VehicleReservations);
            _ = Assert.Single(fixture.Db.VehicleBookingDays);
        }

        /// <summary>
        /// Verifies that invalid officer rejects the whole file without creating users or partial vehicles.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("missing@example.test")]
        [InlineData("viewer@example.test")]
        [InlineData("inactive@example.test")]
        [InlineData("foreign@example.test")]
        public async Task InvalidOfficerRejectsTheWholeFileWithoutCreatingUsersOrPartialVehicles(string email)
        {
            await using Fixture fixture = new ();
            using MemoryStream stream = Csv("radioCallSign;fireDepartment;equipmentOfficerEmail\nFlorian 19-01;Feuerwehr A;owner@example.test\nFlorian 19-02;Feuerwehr A;" + email);
            int usersBefore = await fixture.Db.Users.CountAsync();
            ServiceResult result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
            Assert.Equal(400, result.Code);
            Assert.Contains(Assert.IsType<VehicleImportResponse>(result.Value).Errors, x => x.LineNumber == 3 && x.Description.Contains("Gerätewart"));
            Assert.Empty(fixture.Db.Vehicles);
            Assert.Equal(usersBefore, await fixture.Db.Users.CountAsync());
        }

        /// <summary>
        /// Verifies that invalid files have actionable errors and do not change data.
        /// </summary>
        /// <param name="csv">The readable CSV stream to validate and import.</param>
        /// <param name="expected">The expected result or value for the scenario.</param>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Theory]
        [InlineData("radioCallSign;radioCallSign;fireDepartment;equipmentOfficerEmail\nA;A;Feuerwehr A;owner@example.test", "mehrfach")]
        [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail\n\"Nicht geschlossen;Feuerwehr A;owner@example.test", "CSV-Zeile")]
        [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail;vehicleType\nA;Feuerwehr A;owner@example.test;Unbekannt", "Fahrzeugtyp")]
        [InlineData("radioCallSign;fireDepartment;equipmentOfficerEmail;status\nA;Feuerwehr A;owner@example.test;Unbekannt", "Status")]
        public async Task InvalidFilesHaveActionableErrorsAndDoNotChangeData(string csv, string expected)
        {
            await using Fixture fixture = new ();
            using MemoryStream stream = Csv(csv);
            ServiceResult result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
            Assert.Equal(400, result.Code);
            Assert.Contains(Assert.IsType<VehicleImportResponse>(result.Value).Errors, x => x.Description.Contains(expected));
            Assert.Empty(fixture.Db.Vehicles);
        }

        /// <summary>
        /// Verifies that officers only export assigned vehicles and cannot import updates for other officers.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task OfficersOnlyExportAssignedVehiclesAndCannotImportUpdatesForOtherOfficers()
        {
            await using Fixture fixture = new ();
            Vehicle owned = fixture.Vehicle();
            Vehicle other = fixture.Vehicle("Florian 19-02", "other");
            _ = await fixture.Db.SaveChangesAsync();
            fixture.As("owner", Roles.EquipmentOfficer);
            ServiceResult export = await fixture.Service.Export(VehicleTransferKind.Vehicles, default);
            string csv = Encoding.UTF8.GetString(export.Content!);
            Assert.Contains(owned.RadioCallSign, csv);
            Assert.DoesNotContain(other.RadioCallSign, csv);
            using MemoryStream stream = Csv($"id;radioCallSign;fireDepartment;equipmentOfficerEmail\n{other.Id};{other.RadioCallSign};Feuerwehr A;owner@example.test");
            Assert.Equal(403, (await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default)).Code);
            Assert.Equal("other", (await fixture.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == other.Id)).EquipmentOfficerId);
            using MemoryStream types = Csv("[{\"name\":\"ELW\"}]");
            Assert.Equal(403, (await fixture.Service.Import(VehicleTransferKind.Types, types, default)).Code);
            Assert.Equal(403, (await fixture.Service.Export(VehicleTransferKind.Types, default)).Code);
            fixture.As("viewer", Roles.Viewer);
            Assert.Equal(403, (await fixture.Service.Export(VehicleTransferKind.Vehicles, default)).Code);
        }

        /// <summary>
        /// Verifies that reimport without ids updates by department and call sign and missing columns preserve values.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task ReimportWithoutIdsUpdatesByDepartmentAndCallSignAndMissingColumnsPreserveValues()
        {
            await using Fixture fixture = new ();
            Vehicle vehicle = fixture.Vehicle();
            vehicle.Name = "Bestehender Name";
            vehicle.IsActive = false;
            vehicle.VehicleStatusId = VehicleStatusDefaults.DeregisteredId;
            _ = await fixture.Db.SaveChangesAsync();
            using MemoryStream stream = Csv($"radioCallSign;fireDepartment;equipmentOfficerEmail\n{vehicle.RadioCallSign};Feuerwehr A;owner@example.test");
            ServiceResult result = await fixture.Service.Import(VehicleTransferKind.Vehicles, stream, default);
            Assert.Equal(200, result.Code);
            Vehicle stored = Assert.Single(await fixture.Db.Vehicles.AsNoTracking().ToListAsync());
            Assert.Equal(vehicle.Id, stored.Id);
            Assert.Equal("Bestehender Name", stored.Name);
            Assert.False(stored.IsActive);
            Assert.Equal(VehicleStatusDefaults.DeregisteredId, stored.VehicleStatusId);
        }

        /// <summary>
        /// Verifies that vehicle types round trip and rename in place while duplicate names rollback the entire batch.
        /// </summary>
        /// <returns>A task that completes when the scenario and its assertions finish.</returns>
        [Fact]
        public async Task VehicleTypesRoundTripAndRenameInPlaceWhileDuplicateNamesRollbackTheEntireBatch()
        {
            await using Fixture fixture = new ();
            Vehicle vehicle = fixture.Vehicle();
            _ = await fixture.Db.SaveChangesAsync();
            ServiceResult exported = await fixture.Service.Export(VehicleTransferKind.Types, default);
            List<VehicleTypeImportDto> rows = JsonSerializer.Deserialize<List<VehicleTypeImportDto>>(exported.Content!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            VehicleTypeImportDto type = Assert.Single(rows);
            type.Name = "Mannschaftstransportwagen";
            using MemoryStream updated = new (JsonSerializer.SerializeToUtf8Bytes(rows));
            Assert.Equal(200, (await fixture.Service.Import(VehicleTransferKind.Types, updated, default)).Code);
            Assert.Equal(type.Id, (await fixture.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == vehicle.Id)).VehicleTypeId);
            Assert.Equal(type.Name, (await fixture.Db.Set<VehicleType>().SingleAsync()).Name);
            using MemoryStream invalid = Csv("[{\"name\":\"ELW\"},{\"name\":\"elw\"}]");
            Assert.Equal(400, (await fixture.Service.Import(VehicleTransferKind.Types, invalid, default)).Code);
            _ = Assert.Single(fixture.Db.Set<VehicleType>());
        }

        private static MemoryStream Csv(string text) => new (Encoding.UTF8.GetBytes(text));

        /// <summary>
        /// Creates isolated persistence, caller identity, and service dependencies for the surrounding test scenarios.
        /// </summary>
        private sealed class Fixture : IAsyncDisposable
        {
            /// <summary>
            /// Gets the database context owned by the fixture.
            /// </summary>
            public FeuerwehrDbContext Db { get; } = new (new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            /// <summary>
            /// Gets the application service under test.
            /// </summary>
            public VehicleTransferService Service { get; }

            /// <summary>
            /// Gets the controllable clock used by the fixture.
            /// </summary>
            public TestClock Clock { get; } = new ();

            private readonly HttpContextAccessor http = new () { HttpContext = new DefaultHttpContext() };
            private readonly Guid typeId = Guid.NewGuid();

            /// <summary>
            /// Initializes a new instance of the <see cref="Fixture" /> class with its required dependencies.
            /// </summary>
            public Fixture()
            {
                _ = this.Db.Database.EnsureCreated();
                this.Db.FireDepartments.AddRange(new FireDepartment { Id = 1, Name = "Feuerwehr A" }, new FireDepartment { Id = 2, Name = "Feuerwehr B" });
                _ = this.Db.Set<VehicleType>().Add(new VehicleType { Id = this.typeId, Name = "MTW", NormalizedName = "MTW" });
                _ = this.Db.Roles.Add(new IdentityRole { Id = "equipment-role", Name = Roles.EquipmentOfficer });
                foreach (string? id in new[] { "admin", "owner", "other", "viewer", "inactive", "foreign" })
                {
                    _ = this.Db.Users.Add(new ApplicationUser { Id = id, Email = id + "@example.test", UserName = id, IsActive = id != "inactive", FireDepartmentId = id == "foreign" ? 2 : 1 });
                    if (id is "owner" or "other" or "inactive" or "foreign")
                    {
                        _ = this.Db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = "equipment-role" });
                    }
                }

                _ = this.Db.SaveChanges();
                this.Service = new VehicleTransferService(this.Db, this.http, new UpperInvariantLookupNormalizer(), this.Clock);
                this.As("admin", Roles.Admin);
            }

            /// <summary>
            /// Changes the fixture's current caller identity and role for the next service operation.
            /// </summary>
            /// <param name="id">The identifier of the fixture to process.</param>
            /// <param name="role">The Identity role under test.</param>
            public void As(string id, string role) => this.http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "test"));

            /// <summary>
            /// Creates or retrieves the vehicle used by the booking scenario.
            /// </summary>
            /// <param name="callSign">The radio call sign identifying the fixture vehicle.</param>
            /// <param name="owner">The activity or controller owning the scanner operation.</param>
            /// <returns>The vehicle prepared for the current test scenario.</returns>
            public Vehicle Vehicle(string callSign = "Florian 19-01", string owner = "owner")
            {
                Vehicle vehicle = new ()
                { Id = Guid.NewGuid(), RadioCallSign = callSign, FireDepartmentId = 1, EquipmentOfficerId = owner, VehicleTypeId = this.typeId };
                _ = this.Db.Vehicles.Add(vehicle);
                return vehicle;
            }

            /// <summary>
            /// Releases the resources owned by this fixture component.
            /// </summary>
            /// <returns>A task that completes when the scenario and its assertions finish.</returns>
            public ValueTask DisposeAsync() => this.Db.DisposeAsync();
        }

        /// <summary>
        /// Supplies a controllable UTC time to tests.
        /// </summary>
        private sealed class TestClock : TimeProvider
        {
            /// <summary>
            /// Gets or sets the UTC instant returned by the test clock.
            /// </summary>
            public DateTimeOffset UtcNow { get; set; } = new (2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

            /// <summary>
            /// Returns the deterministic UTC instant configured for this test clock.
            /// </summary>
            /// <returns>The UTC instant configured by the test clock.</returns>
            public override DateTimeOffset GetUtcNow() => this.UtcNow;
        }
    }
}
