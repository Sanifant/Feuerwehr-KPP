using System.Security.Claims;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace De.Openelp.Feuerwehr.Tests.Backend.Services
{
    public sealed class VehicleServiceTests
    {
        [Fact]
        /// <summary>
        /// Executes status Timestamp Tracks Initial Assignment And Real Transitions But Not Master Data Or Activation Edits.
        /// </summary>
        public async Task StatusTimestampTracksInitialAssignmentAndRealTransitionsButNotMasterDataOrActivationEdits()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            DateTime initial = fixture.Clock.GetUtcNow().UtcDateTime;
            Assert.Equal(initial, vehicle.StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            VehicleDto renamed = Assert.IsType<VehicleDto>((await fixture.Service.Save(vehicle.Id, new()
            {
                FireDepartmentId = 1, EquipmentOfficerId = "officer", RadioCallSign = vehicle.RadioCallSign,
                Name = "Neuer Name", ExpectedVersion = vehicle.Version
            }, default)).Value);
            Assert.Equal(initial, renamed.StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            VehicleDto changed = Assert.IsType<VehicleDto>((await fixture.Service.Save(vehicle.Id, new()
            {
                FireDepartmentId = 1, EquipmentOfficerId = "officer", RadioCallSign = vehicle.RadioCallSign,
                VehicleStatusId = VehicleStatusDefaults.MaintenanceId, ExpectedVersion = renamed.Version
            }, default)).Value);
            DateTime changedAt = fixture.Clock.GetUtcNow().UtcDateTime;
            Assert.Equal(changedAt, changed.StatusChangedAtUtc);
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            VehicleDto deactivated = Assert.IsType<VehicleDto>((await fixture.Service.SetActive(vehicle.Id, new() { IsActive = false, ExpectedVersion = changed.Version }, default)).Value);
            Assert.Equal(changedAt, deactivated.StatusChangedAtUtc);
            Vehicle stored = await fixture.Db.Vehicles.AsNoTracking().SingleAsync(x => x.Id == vehicle.Id);
            Assert.Equal(changedAt, stored.StatusChangedAtUtc);
        }

        [Fact]
        /// <summary>
        /// Executes legacy Vehicle Timestamp Stays Unknown Until Its Status Actually Changes.
        /// </summary>
        public async Task LegacyVehicleTimestampStaysUnknownUntilItsStatusActuallyChanges()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            (await fixture.Db.Vehicles.FindAsync(vehicle.Id))!.StatusChangedAtUtc = null;
            await fixture.Db.SaveChangesAsync();
            VehicleDto updated = Assert.IsType<VehicleDto>((await fixture.Service.Save(vehicle.Id, new()
            {
                FireDepartmentId = 1, EquipmentOfficerId = "officer", RadioCallSign = vehicle.RadioCallSign,
                Name = "Name geändert", ExpectedVersion = vehicle.Version
            }, default)).Value);
            Assert.Null(updated.StatusChangedAtUtc);
        }

        [Fact]
        /// <summary>
        /// Executes status Catalog Is Extensible And Maintenance Prevents New Bookings And Approval.
        /// </summary>
        public async Task StatusCatalogIsExtensibleAndMaintenancePreventsNewBookingsAndApproval()
        {
            await using Fixture fixture = new Fixture();
            List<VehicleStatusDto> statuses = Assert.IsType<List<VehicleStatusDto>>((await fixture.Service.Statuses(false, default)).Value);
            Assert.Equal(3, statuses.Count);
            Assert.Equal(new[] { "Abgemeldet", "Betriebsbereit", "Wartung" }, statuses.Select(x => x.Name).OrderBy(x => x).ToArray());
            Assert.Equal(403, (await fixture.Service.SaveStatus(null, new() { Name = "AuÃŸer Dienst" }, default)).Code);
            fixture.As("admin", Roles.Admin);
            VehicleStatusDto custom = Assert.IsType<VehicleStatusDto>((await fixture.Service.SaveStatus(null, new() { Name = "AuÃŸer Dienst", AllowsBooking = false }, default)).Value);
            Assert.Equal(409, (await fixture.Service.SaveStatus(null, new() { Name = "auÃŸer dienst" }, default)).Code);
            fixture.As("officer", Roles.EquipmentOfficer);
            VehicleDto vehicle = await fixture.Vehicle();
            fixture.As("member", Roles.Firefighter);
            VehicleReservationDto pending = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, fixture.Request(), false, default)).Value);
            fixture.As("officer", Roles.EquipmentOfficer);
            VehicleDto current = Assert.Single(Assert.IsType<List<VehicleDto>>((await fixture.Service.List(false, default)).Value));
            ServiceResult changed = await fixture.Service.Save(vehicle.Id, new() { FireDepartmentId = 1, RadioCallSign = current.RadioCallSign,
                EquipmentOfficerId = "officer", ExpectedVersion = current.Version, VehicleStatusId = VehicleStatusDefaults.MaintenanceId }, default);
            Assert.Equal("Wartung", Assert.IsType<VehicleDto>(changed.Value).VehicleStatusName);
            Assert.Equal(409, (await fixture.Service.Reserve(vehicle.Id, fixture.Request(), true, default)).Code);
            Assert.Equal(409, (await fixture.Service.Decide(pending.Id, new() { ExpectedVersion = pending.Version }, VehicleReservationStatus.Confirmed, default)).Code);
            List<VehicleAvailabilityDto> calendar = Assert.IsType<List<VehicleAvailabilityDto>>((await fixture.Service.Availability(fixture.Request().From, fixture.Request().To, default)).Value);
            Assert.All(Assert.Single(calendar).Days, day => Assert.Equal(VehicleDayStatus.Unavailable, day.Status));
            Assert.Empty(fixture.Db.VehicleBookingDays);
            Assert.Contains(Assert.IsType<List<VehicleStatusDto>>((await fixture.Service.Statuses(false, default)).Value), x => x.Id == custom.Id);
        }

        [Fact]
        /// <summary>
        /// Executes availability Can Be Filtered By Normalized Vehicle Type.
        /// </summary>
        public async Task AvailabilityCanBeFilteredByNormalizedVehicleType()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            VehicleType mtw = new VehicleType { Id = Guid.NewGuid(), Name = "MTW", NormalizedName = "MTW" };
            VehicleType elw = new VehicleType { Id = Guid.NewGuid(), Name = "ELW", NormalizedName = "ELW" };
            fixture.Db.Set<VehicleType>().AddRange(mtw, elw);
            (await fixture.Db.Vehicles.FindAsync(vehicle.Id))!.VehicleTypeId = mtw.Id;
            fixture.Db.Vehicles.Add(new Vehicle { Id = Guid.NewGuid(), FireDepartmentId = 1, EquipmentOfficerId = "officer", RadioCallSign = "ELW 01", VehicleTypeId = elw.Id });
            await fixture.Db.SaveChangesAsync();
            List<VehicleAvailabilityDto> filtered = Assert.IsType<List<VehicleAvailabilityDto>>((await fixture.Service.Availability(fixture.Request().From, fixture.Request().To, default, " mtw ")).Value);
            Assert.Equal(vehicle.Id, Assert.Single(filtered).Vehicle.Id);
        }

        [Fact]
        /// <summary>
        /// Executes vehicle Types Require Admin Reject Duplicates And Preserve Existing Assignments When Deactivated.
        /// </summary>
        public async Task VehicleTypesRequireAdmin_RejectDuplicatesAndPreserveExistingAssignmentsWhenDeactivated()
        {
            await using Fixture fixture = new Fixture();
            Assert.Equal(403, (await fixture.Service.SaveType(null, new() { Name = "MTW" }, default)).Code);
            fixture.As("admin", Roles.Admin);
            VehicleTypeDto type = Assert.IsType<VehicleTypeDto>((await fixture.Service.SaveType(null, new() { Name = " MTW " }, default)).Value);
            Assert.Equal("MTW", type.Name);
            Assert.Equal(409, (await fixture.Service.SaveType(null, new() { Name = "mtw" }, default)).Code);
            fixture.As("officer", Roles.EquipmentOfficer);
            VehicleDto vehicle = Assert.IsType<VehicleDto>((await fixture.Service.Save(null, new()
                { FireDepartmentId = 1, RadioCallSign = "Florian 19-01", EquipmentOfficerId = "officer", VehicleTypeId = type.Id }, default)).Value);
            Assert.Equal(type.Id, vehicle.VehicleTypeId);
            Assert.Equal(type.Name, vehicle.VehicleTypeName);
            fixture.As("admin", Roles.Admin);
            Assert.Equal(409, (await fixture.Service.SetTypeActive(type.Id, new() { ExpectedVersion = 99, IsActive = false }, default)).Code);
            VehicleTypeDto renamed = Assert.IsType<VehicleTypeDto>((await fixture.Service.SaveType(type.Id, new() { Name = "Mannschaftstransportwagen", ExpectedVersion = type.Version }, default)).Value);
            Assert.Equal(200, (await fixture.Service.SetTypeActive(type.Id, new() { ExpectedVersion = renamed.Version, IsActive = false }, default)).Code);
            Assert.Empty(Assert.IsType<List<VehicleTypeDto>>((await fixture.Service.Types(false, default)).Value));
            Assert.Single(Assert.IsType<List<VehicleTypeDto>>((await fixture.Service.Types(true, default)).Value));
            fixture.As("officer", Roles.EquipmentOfficer);
            Assert.Equal(403, (await fixture.Service.SetTypeActive(type.Id, new() { ExpectedVersion = 3, IsActive = true }, default)).Code);
            VehicleDto retained = Assert.IsType<VehicleDto>((await fixture.Service.Save(vehicle.Id, new()
            {
                FireDepartmentId = 1, RadioCallSign = vehicle.RadioCallSign, EquipmentOfficerId = "officer",
                VehicleTypeId = type.Id, ExpectedVersion = vehicle.Version
            }, default)).Value);
            Assert.Equal("Mannschaftstransportwagen", retained.VehicleTypeName);
            Assert.Equal(400, (await fixture.Service.Save(null, new()
                { FireDepartmentId = 1, RadioCallSign = "Florian 19-02", EquipmentOfficerId = "officer", VehicleTypeId = type.Id }, default)).Code);
            Assert.Equal(400, (await fixture.Service.Save(null, new()
                { FireDepartmentId = 1, RadioCallSign = "Florian 19-03", EquipmentOfficerId = "officer", VehicleTypeId = Guid.NewGuid() }, default)).Code);
        }

        [Theory]
        [InlineData(Roles.Commander, 200)]
        [InlineData(Roles.Firefighter, 200)]
        [InlineData(Roles.Viewer, 403)]
        [InlineData(Roles.InventoryViewer, 403)]
        [InlineData(Roles.Admin, 403)]
        /// <summary>
        /// Executes requests Require An Explicit Booking Role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="expected">The expected.</param>
        public async Task RequestsRequireAnExplicitBookingRole(string role, int expected)
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            fixture.As("member", role);
            ServiceResult result = await fixture.Service.Reserve(vehicle.Id, fixture.Request(), false, default);
            Assert.Equal(expected, result.Code);
            Assert.Empty(fixture.Db.VehicleBookingDays);
        }

        [Fact]
        /// <summary>
        /// Executes pending Requests Are Orange And Non Blocking Only Approval Claims All Days.
        /// </summary>
        public async Task PendingRequestsAreOrangeAndNonBlocking_OnlyApprovalClaimsAllDays()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            fixture.As("member", Roles.Firefighter);
            CreateVehicleReservationRequest request = fixture.Request("Fahrt zur Jugendfeuerwehr <Ãœbung>");
            VehicleReservationDto first = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, request, false, default)).Value);
            VehicleReservationDto second = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, fixture.Request(), false, default)).Value);
            Assert.Equal(VehicleReservationStatus.Pending, first.Status);
            VehicleMailMessage notification = await fixture.Db.VehicleMailMessages.SingleAsync(x => x.ReservationId == first.Id && x.Kind == "Requested");
            Assert.Contains("https://example.test/vehicles", notification.TextBody);
            Assert.DoesNotContain("vehicles/manage", notification.HtmlBody);
            Assert.Empty(fixture.Db.VehicleBookingDays);
            List<VehicleAvailabilityDto> calendar = Assert.IsType<List<VehicleAvailabilityDto>>((await fixture.Service.Availability(request.From, request.To, default)).Value);
            Assert.All(Assert.Single(calendar).Days, day => Assert.Equal(VehicleDayStatus.Pending, day.Status));
            Assert.Equal(3, calendar[0].Days.Count);

            fixture.As("officer", Roles.EquipmentOfficer);
            Assert.Equal(200, (await fixture.Service.Decide(first.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default)).Code);
            Assert.Equal(3, await fixture.Db.VehicleBookingDays.CountAsync());
            Assert.Equal(409, (await fixture.Service.Decide(second.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default)).Code);
            calendar = Assert.IsType<List<VehicleAvailabilityDto>>((await fixture.Service.Availability(request.From, request.To, default)).Value);
            Assert.All(calendar[0].Days, day => Assert.Equal(VehicleDayStatus.Booked, day.Status));
            VehicleMailMessage confirmation = await fixture.Db.VehicleMailMessages.SingleAsync(x => x.Kind == "Confirmed");
            Assert.Equal("member@example.test", confirmation.Recipient);
            Assert.Contains("Fahrt zur Jugendfeuerwehr <Ãœbung>", confirmation.TextBody);
            Assert.Contains("&lt;", confirmation.HtmlBody);
            Assert.Contains("Fahrt zur Jugendfeuerwehr <Ãœbung>", System.Net.WebUtility.HtmlDecode(confirmation.HtmlBody));
            Assert.DoesNotContain("<Ãœbung>", confirmation.HtmlBody);
        }

        [Fact]
        /// <summary>
        /// Executes retries Do Not Create Duplicate Requests Bookings Or Emails.
        /// </summary>
        public async Task RetriesDoNotCreateDuplicateRequestsBookingsOrEmails()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            fixture.As("member", Roles.Commander);
            CreateVehicleReservationRequest request = fixture.Request();
            VehicleReservationDto first = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, request, false, default)).Value);
            VehicleReservationDto retry = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, request, false, default)).Value);
            Assert.Equal(first.Id, retry.Id);
            Assert.Single(fixture.Db.VehicleReservations);
            Assert.Single(fixture.Db.VehicleMailMessages);
            request.Comment = "Andere Nutzlast";
            Assert.Equal(409, (await fixture.Service.Reserve(vehicle.Id, request, false, default)).Code);
            fixture.As("officer", Roles.EquipmentOfficer);
            await fixture.Service.Decide(first.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default);
            await fixture.Service.Decide(first.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default);
            Assert.Equal(3, fixture.Db.VehicleBookingDays.Count());
            Assert.Equal(2, fixture.Db.VehicleMailMessages.Count());
        }

        [Fact]
        /// <summary>
        /// Executes only Assigned Officer Can Decide Or Book Directly Admin Can Maintain.
        /// </summary>
        public async Task OnlyAssignedOfficerCanDecideOrBookDirectly_AdminCanMaintain()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            fixture.As("member", Roles.Firefighter);
            VehicleReservationDto pending = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, fixture.Request(), false, default)).Value);
            fixture.As("other", Roles.EquipmentOfficer);
            Assert.Equal(403, (await fixture.Service.Decide(pending.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default)).Code);
            Assert.Equal(403, (await fixture.Service.Reserve(vehicle.Id, fixture.Request(), true, default)).Code);
            Assert.Equal(403, (await fixture.Service.SetActive(vehicle.Id, new() { IsActive = false, ExpectedVersion = 2 }, default)).Code);
            fixture.As("admin", Roles.Admin);
            Assert.Equal(200, (await fixture.Service.SetActive(vehicle.Id, new() { IsActive = false, ExpectedVersion = 2 }, default)).Code);
            fixture.As("officer", Roles.EquipmentOfficer);
            Assert.Equal(409, (await fixture.Service.Decide(pending.Id, new() { ExpectedVersion = 1 }, VehicleReservationStatus.Confirmed, default)).Code);
            Assert.Single(fixture.Db.VehicleReservations);
            Assert.Empty(fixture.Db.VehicleBookingDays);
        }

        [Fact]
        /// <summary>
        /// Executes direct Booking Is Confirmed And Cancellation Releases Days Without Deleting History.
        /// </summary>
        public async Task DirectBookingIsConfirmed_AndCancellationReleasesDaysWithoutDeletingHistory()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            VehicleReservationDto reservation = Assert.IsType<VehicleReservationDto>((await fixture.Service.Reserve(vehicle.Id, fixture.Request(), true, default)).Value);
            Assert.Equal(VehicleReservationStatus.Confirmed, reservation.Status);
            Assert.True(reservation.IsDirectBooking);
            Assert.Equal(3, fixture.Db.VehicleBookingDays.Count());
            Assert.Equal(200, (await fixture.Service.Decide(reservation.Id, new() { ExpectedVersion = reservation.Version }, VehicleReservationStatus.Cancelled, default)).Code);
            Assert.Empty(fixture.Db.VehicleBookingDays);
            Assert.Single(fixture.Db.VehicleReservations);
            Assert.Equal(VehicleReservationStatus.Cancelled, fixture.Db.VehicleReservations.Single().Status);
            Assert.Contains(fixture.Db.VehicleEvents, x => x.Action == "Cancelled");
        }

        [Fact]
        /// <summary>
        /// Executes vehicle Data And Booking Dates Are Validated Officer Cannot Create For Another Department.
        /// </summary>
        public async Task VehicleDataAndBookingDatesAreValidated_OfficerCannotCreateForAnotherDepartment()
        {
            await using Fixture fixture = new Fixture();
            Assert.Equal(400, (await fixture.Service.Save(null, new() { FireDepartmentId = 1, EquipmentOfficerId = "officer", RadioCallSign = "   " }, default)).Code);
            Assert.Equal(403, (await fixture.Service.Save(null, new() { FireDepartmentId = 2, EquipmentOfficerId = "other", RadioCallSign = "Fremd" }, default)).Code);
            VehicleDto vehicle = await fixture.Vehicle();
            CreateVehicleReservationRequest backwards = fixture.Request(); backwards.To = backwards.From.AddDays(-1);
            Assert.Equal(400, (await fixture.Service.Reserve(vehicle.Id, backwards, true, default)).Code);
            CreateVehicleReservationRequest tooLong = fixture.Request(); tooLong.Comment = new string('x', 2001);
            Assert.Equal(400, (await fixture.Service.Reserve(vehicle.Id, tooLong, true, default)).Code);
            CreateVehicleReservationRequest past = fixture.Request(); past.From = past.To = new DateOnly(2026, 10, 29);
            Assert.Equal(400, (await fixture.Service.Reserve(vehicle.Id, past, true, default)).Code);
        }

        [Fact]
        /// <summary>
        /// Executes mail Failure Keeps Booking And Retries Later.
        /// </summary>
        public async Task MailFailureKeepsBookingAndRetriesLater()
        {
            await using Fixture fixture = new Fixture();
            VehicleDto vehicle = await fixture.Vehicle();
            await fixture.Service.Reserve(vehicle.Id, fixture.Request("Fahrt"), true, default);
            Mock<IHtmlMailService> mail = new Mock<IHtmlMailService>();
            mail.Setup(x => x.SendHtmlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new IOException("offline"));
            VehicleMailDispatcher worker = new VehicleMailDispatcher(fixture.Db, mail.Object, fixture.Clock, NullLogger<VehicleMailDispatcher>.Instance);
            await worker.Dispatch(default);
            Assert.Null(fixture.Db.VehicleMailMessages.Single().SentAtUtc);
            Assert.Equal(3, fixture.Db.VehicleBookingDays.Count());
            fixture.Clock.UtcNow = fixture.Clock.UtcNow.AddHours(1);
            mail.Setup(x => x.SendHtmlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            await worker.Dispatch(default);
            await worker.Dispatch(default);
            Assert.NotNull(fixture.Db.VehicleMailMessages.Single().SentAtUtc);
            mail.Verify(x => x.SendHtmlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        private sealed class Fixture : IAsyncDisposable
        {
            public FeuerwehrDbContext Db { get; } = new(new DbContextOptionsBuilder<FeuerwehrDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            private readonly HttpContextAccessor http = new() { HttpContext = new DefaultHttpContext() };
            public TestClock Clock { get; } = new();
            public VehicleService Service { get; }
            public Fixture()
            {
                Db.Database.EnsureCreated();
                Db.FireDepartments.AddRange(new FireDepartment { Id = 1, Name = "Feuerwehr A" }, new FireDepartment { Id = 2, Name = "Feuerwehr B" });
                Db.Roles.Add(new IdentityRole { Id = "equipment-role", Name = Roles.EquipmentOfficer });
                foreach (string? id in new[] { "officer", "other", "member", "admin" })
                    Db.Users.Add(new ApplicationUser { Id = id, Email = id + "@example.test", UserName = id, FirstName = id, IsActive = true, FireDepartmentId = id == "other" ? 2 : 1 });
                foreach (string? id in new[] { "officer", "other" }) Db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = "equipment-role" });
                Db.SaveChanges();
                Service = new VehicleService(Db, http, Clock, new VehicleMailComposer(new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string?> { ["Frontend:BaseUrl"] = "https://example.test" }).Build(), Clock));
                As("officer", Roles.EquipmentOfficer);
            }
            /// <summary>
            /// Executes as.
            /// </summary>
            /// <param name="id">The id.</param>
            public void As(string id, string role) => http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "test"));
            /// <summary>
            /// Executes vehicle.
            /// </summary>
            public async Task<VehicleDto> Vehicle() => Assert.IsType<VehicleDto>((await Service.Save(null, new()
                { FireDepartmentId = 1, RadioCallSign = "Florian 19-01", EquipmentOfficerId = "officer" }, default)).Value);
            /// <summary>
            /// Executes request.
            /// </summary>
            /// <param name="comment">The comment.</param>
            public CreateVehicleReservationRequest Request(string? comment = null) => new()
                { CommandId = Guid.NewGuid(), From = new DateOnly(2026, 10, 31), To = new DateOnly(2026, 11, 2), Comment = comment };
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

}
