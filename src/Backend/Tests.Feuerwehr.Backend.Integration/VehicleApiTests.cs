using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.Tests.Backend.Integration
{
    /// <summary>
    /// Executes vehicle Api Tests.
    /// </summary>
    /// <param name="fixture">The fixture.</param>
    public sealed class VehicleApiTests(InventoryApiFixture fixture) : IClassFixture<InventoryApiFixture>
    {
        [Fact]
        /// <summary>
        /// Executes import Export Uses Officer Email And Preserves Bookings With Real Postgres And Http Uploads.
        /// </summary>
        public async Task ImportExportUsesOfficerEmailAndPreservesBookingsWithRealPostgresAndHttpUploads()
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            VehicleDto vehicle = await CreateVehicle(officer);
            HttpResponseMessage booking = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = fixture.Today.AddDays(5), To = fixture.Today.AddDays(6) });
            booking.EnsureSuccessStatusCode();
            DateTime? initialStatusChange = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id).StatusChangedAtUtc;
            Assert.NotNull(initialStatusChange);
            byte[] types = await admin.GetByteArrayAsync("/api/vehicles/types/export");
            using (MultipartFormDataContent form = Upload(types, "fahrzeugtypen.json"))
                (await admin.PostAsync("/api/vehicles/types/import", form)).EnsureSuccessStatusCode();
            string csv = await admin.GetStringAsync("/api/vehicles/export");
            Assert.Contains("equipmentOfficerEmail", csv);
            Assert.Contains($"{Roles.EquipmentOfficer}@integration.test", csv);
            Assert.DoesNotContain(vehicle.EquipmentOfficerId, csv);
            using (MultipartFormDataContent form = Upload(Encoding.UTF8.GetBytes(csv.Replace($"{Roles.EquipmentOfficer}@integration.test", $"{Roles.EquipmentOfficer.ToUpperInvariant()}@INTEGRATION.TEST")), "fahrzeuge.csv"))
            {
                HttpResponseMessage imported = await admin.PostAsync("/api/vehicles/import", form);
                Assert.True(imported.IsSuccessStatusCode, await imported.Content.ReadAsStringAsync());
                Assert.True((await imported.Content.ReadFromJsonAsync<VehicleImportResponse>())!.UpdatedCount > 0);
            }
            VehicleDto stored = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles?includeInactive=true"))!.Single(x => x.Id == vehicle.Id);
            Assert.Equal(vehicle.EquipmentOfficerId, stored.EquipmentOfficerId);
            Assert.Equal(initialStatusChange, stored.StatusChangedAtUtc);
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            Assert.Equal(2, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
            Assert.Single(await db.VehicleReservations.Where(x => x.VehicleId == vehicle.Id).ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes invalid Email Rolls Back All Csv Rows And Returns Their Line Number.
        /// </summary>
        public async Task InvalidEmailRollsBackAllCsvRowsAndReturnsTheirLineNumber()
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            VehicleDto vehicle = await CreateVehicle(officer);
            string csv = $"id;radioCallSign;name;fireDepartment;equipmentOfficerEmail\n{vehicle.Id};{vehicle.RadioCallSign};Nicht speichern;MTW Integration;{Roles.EquipmentOfficer}@integration.test\n" +
                " ;Neues Fahrzeug;Fehler;MTW Integration;missing@integration.test";
            using MultipartFormDataContent form = Upload(Encoding.UTF8.GetBytes(csv), "fahrzeuge.csv");
            HttpResponseMessage response = await admin.PostAsync("/api/vehicles/import", form);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains((await response.Content.ReadFromJsonAsync<VehicleImportResponse>())!.Errors, x => x.LineNumber == 3);
            VehicleDto stored = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id);
            Assert.Null(stored.Name);
        }

        [Fact]
        /// <summary>
        /// Executes custom Statuses Are Admin Managed And Mtw Availability Excludes Other Vehicle Types.
        /// </summary>
        public async Task CustomStatusesAreAdminManagedAndMtwAvailabilityExcludesOtherVehicleTypes()
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            VehicleDto vehicle = await CreateVehicle(officer);
            string statusName = $"AuÃŸer Dienst {Guid.NewGuid():N}";
            Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/vehicles/statuses", new SaveVehicleStatusRequest { Name = statusName })).StatusCode);
            HttpResponseMessage response = await admin.PostAsJsonAsync("/api/vehicles/statuses", new SaveVehicleStatusRequest { Name = statusName, AllowsBooking = false });
            response.EnsureSuccessStatusCode();
            VehicleStatusDto status = (await response.Content.ReadFromJsonAsync<VehicleStatusDto>())!;
            VehicleTypeDto[] types = (await admin.GetFromJsonAsync<VehicleTypeDto[]>("/api/vehicles/types"))!;
            VehicleTypeDto? mtw = types.SingleOrDefault(x => x.Name == "MTW");
            if (mtw is null)
            {
                HttpResponseMessage created = await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = "MTW" });
                created.EnsureSuccessStatusCode(); mtw = (await created.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
            }
            HttpResponseMessage changed = await officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new SaveVehicleRequest
            {
                RadioCallSign = vehicle.RadioCallSign, FireDepartmentId = vehicle.FireDepartmentId, EquipmentOfficerId = vehicle.EquipmentOfficerId,
                ExpectedVersion = vehicle.Version, VehicleTypeId = mtw.Id, VehicleStatusId = status.Id
            });
            changed.EnsureSuccessStatusCode();
            VehicleDto updated = (await changed.Content.ReadFromJsonAsync<VehicleDto>())!;
            Assert.Equal(statusName, updated.VehicleStatusName); Assert.False(updated.AllowsBooking);
            Assert.NotNull(updated.StatusChangedAtUtc);
            DateOnly from = fixture.Today.AddDays(2);
            VehicleAvailabilityDto[] calendar = (await officer.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={from.AddDays(2):yyyy-MM-dd}&vehicleType=MTW"))!;
            Assert.All(calendar, x => Assert.Equal("MTW", x.Vehicle.VehicleTypeName));
            Assert.All(calendar.Single(x => x.Vehicle.Id == vehicle.Id).Days, day => Assert.Equal(VehicleDayStatus.Unavailable, day.Status));
            Assert.Equal(HttpStatusCode.Conflict, (await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = from, To = from })).StatusCode);
        }

        private static MultipartFormDataContent Upload(byte[] content, string name)
        {
            MultipartFormDataContent form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(content), "file", name); return form;
        }

        [Fact]
        /// <summary>
        /// Executes vehicle Types Are Admin Managed And Appear In Vehicle And Calendar Responses.
        /// </summary>
        public async Task VehicleTypesAreAdminManaged_AndAppearInVehicleAndCalendarResponses()
        {
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            string name = $"MTW {Guid.NewGuid():N}";
            Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name })).StatusCode);
            HttpResponseMessage response = await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name });
            response.EnsureSuccessStatusCode();
            VehicleTypeDto type = (await response.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name.ToLowerInvariant() })).StatusCode);
            VehicleDto vehicle = await CreateVehicle(officer);
            HttpResponseMessage assigned = await officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new SaveVehicleRequest
            {
                FireDepartmentId = vehicle.FireDepartmentId, EquipmentOfficerId = vehicle.EquipmentOfficerId,
                RadioCallSign = vehicle.RadioCallSign, ExpectedVersion = vehicle.Version, VehicleTypeId = type.Id
            });
            assigned.EnsureSuccessStatusCode();
            Assert.Equal(name, (await assigned.Content.ReadFromJsonAsync<VehicleDto>())!.VehicleTypeName);
            VehicleAvailabilityDto[]? calendar = await officer.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={fixture.Today:yyyy-MM-dd}&to={fixture.Today.AddDays(20):yyyy-MM-dd}");
            Assert.Equal(type.Id, calendar!.Single(x => x.Vehicle.Id == vehicle.Id).Vehicle.VehicleTypeId);
            HttpResponseMessage updated = await admin.PutAsJsonAsync($"/api/vehicles/types/{type.Id}", new SaveVehicleTypeRequest { Name = name + " neu", ExpectedVersion = type.Version });
            updated.EnsureSuccessStatusCode();
            VehicleTypeDto renamed = (await updated.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
            HttpResponseMessage deactivated = await admin.PutAsJsonAsync($"/api/vehicles/types/{type.Id}/active", new SetVehicleTypeActiveRequest { IsActive = false, ExpectedVersion = renamed.Version });
            deactivated.EnsureSuccessStatusCode();
            Assert.DoesNotContain((await officer.GetFromJsonAsync<VehicleTypeDto[]>("/api/vehicles/types"))!, x => x.Id == type.Id);
            Assert.Equal(name + " neu", (await officer.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id).VehicleTypeName);
        }

        [Fact]
        /// <summary>
        /// Executes concurrent Approvals Claim The Entire Range Exactly Once And Send Html With Inline Logo And Comment.
        /// </summary>
        public async Task ConcurrentApprovals_ClaimTheEntireRangeExactlyOnce_AndSendHtmlWithInlineLogoAndComment()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            using HttpClient member = await fixture.Client(Roles.Firefighter);
            VehicleDto vehicle = await CreateVehicle(officer);
            DateOnly from = fixture.Today.AddDays(3);
            DateOnly to = from.AddDays(3);
            string comment = "Jugendfeuerwehr <Ausflug> & Ausbildung";
            HttpResponseMessage[] requests = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations",
                new CreateVehicleReservationRequest { CommandId = Guid.NewGuid(), From = from, To = to, Comment = comment })));
            Assert.All(requests, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            VehicleReservationDto[] reservations = await Task.WhenAll(requests.Select(async response => (await response.Content.ReadFromJsonAsync<VehicleReservationDto>())!));
            VehicleAvailabilityDto[]? pending = await member.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            Assert.All(pending!.Single(x => x.Vehicle.Id == vehicle.Id).Days, x => Assert.Equal(VehicleDayStatus.Pending, x.Status));
            HttpResponseMessage[] decisions = await Task.WhenAll(reservations.Select(reservation => officer.PostAsJsonAsync(
                $"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = reservation.Version })));
            Assert.Single(decisions, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(decisions, x => x.StatusCode == HttpStatusCode.Conflict);

            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            List<VehicleBookingDay> days = await db.VehicleBookingDays.Where(x => x.VehicleId == vehicle.Id).ToListAsync();
            Assert.Equal(4, days.Count);
            Guid winner = Assert.Single(days.Select(x => x.ReservationId).Distinct());
            VehicleReservation stored = await db.VehicleReservations.SingleAsync(x => x.Id == winner);
            Assert.Equal(VehicleReservationStatus.Confirmed, stored.Status);
            Assert.Equal(1, await db.VehicleMailMessages.CountAsync(x => x.ReservationId == winner && x.Kind == "Confirmed"));
            VehicleAvailabilityDto[]? booked = await member.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            Assert.All(booked!.Single(x => x.Vehicle.Id == vehicle.Id).Days, x => Assert.Equal(VehicleDayStatus.Booked, x.Status));

            await scope.ServiceProvider.GetRequiredService<IVehicleMailDispatcher>().Dispatch(default);
            using HttpClient mail = new HttpClient { BaseAddress = fixture.MailpitUrl };
            JsonElement messages = await mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");
            JsonElement[] matching = messages.GetProperty("messages").EnumerateArray().Where(x =>
                x.GetProperty("Subject").GetString() == $"MTW-Buchung bestätigt: {vehicle.RadioCallSign}").ToArray();
            JsonElement delivered = Assert.Single(matching);
            JsonElement body = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{delivered.GetProperty("ID").GetString()}");
            Assert.Contains(comment, body.GetProperty("Text").GetString());
            string html = body.GetProperty("HTML").GetString()!;
            Assert.Contains("cid:feuerwehr-logo", html);
            Assert.Contains("&lt;Ausflug&gt;", html);
            Assert.DoesNotContain("<Ausflug>", html);
            Assert.NotEmpty(body.GetProperty("Inline").EnumerateArray());
            JsonElement[] notifications = messages.GetProperty("messages").EnumerateArray().Where(x =>
                x.GetProperty("Subject").GetString() == $"Neue MTW-Reservierungsanfrage: {vehicle.RadioCallSign}").ToArray();
            Assert.Equal(2, notifications.Length);
            foreach (JsonElement notification in notifications)
            {
                JsonElement notificationBody = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{notification.GetProperty("ID").GetString()}");
                Assert.Contains(comment, notificationBody.GetProperty("Text").GetString());
            }
        }

        [Fact]
        /// <summary>
        /// Executes parallel Direct Booking And Approval Cannot Partially Book Overlapping Ranges.
        /// </summary>
        public async Task ParallelDirectBookingAndApproval_CannotPartiallyBookOverlappingRanges()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            using HttpClient member = await fixture.Client(Roles.Firefighter);
            VehicleDto vehicle = await CreateVehicle(officer);
            DateOnly start = fixture.Today.AddDays(10);
            HttpResponseMessage request = await member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(2) });
            request.EnsureSuccessStatusCode();
            VehicleReservationDto reservation = (await request.Content.ReadFromJsonAsync<VehicleReservationDto>())!;
            HttpResponseMessage[] results = await Task.WhenAll(
                officer.PostAsJsonAsync($"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = 1 }),
                officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
                    { CommandId = Guid.NewGuid(), From = start.AddDays(1), To = start.AddDays(3) }));
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            List<VehicleBookingDay> days = await db.VehicleBookingDays.Where(x => x.VehicleId == vehicle.Id).ToListAsync();
            Assert.Equal(3, days.Count);
            Assert.Single(days.Select(x => x.ReservationId).Distinct());
        }

        [Fact]
        /// <summary>
        /// Executes vehicle Api Enforces Ownership And Roles And Preserves Bookings After Deactivation.
        /// </summary>
        public async Task VehicleApiEnforcesOwnershipAndRoles_AndPreservesBookingsAfterDeactivation()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            using HttpClient admin = await fixture.Client(Roles.Admin);
            using HttpClient member = await fixture.Client(Roles.Firefighter);
            using HttpClient viewer = await fixture.Client(Roles.InventoryViewer);
            VehicleDto vehicle = await CreateVehicle(officer);
            DateOnly start = fixture.Today.AddDays(20);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/Users/{vehicle.EquipmentOfficerId}/permanent")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = start, To = start })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest
                { IsActive = false, ExpectedVersion = vehicle.Version })).StatusCode);
            CreateVehicleReservationRequest command = new CreateVehicleReservationRequest { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(1) };
            HttpResponseMessage created = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", command);
            created.EnsureSuccessStatusCode();
            HttpResponseMessage replay = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", command);
            Assert.Equal((await created.Content.ReadFromJsonAsync<VehicleReservationDto>())!.Id, (await replay.Content.ReadFromJsonAsync<VehicleReservationDto>())!.Id);
            VehicleDto current = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id);
            HttpResponseMessage deactivated = await admin.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest { ExpectedVersion = current.Version, IsActive = false });
            deactivated.EnsureSuccessStatusCode();
            Assert.DoesNotContain((await member.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!, x => x.Id == vehicle.Id);
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            Assert.Equal(2, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
            Assert.Single(await db.VehicleReservations.Where(x => x.VehicleId == vehicle.Id).ToListAsync());
        }

        [Fact]
        /// <summary>
        /// Executes concurrent Deactivation And Approval Uses The Same Vehicle Lock.
        /// </summary>
        public async Task ConcurrentDeactivationAndApproval_UsesTheSameVehicleLock()
        {
            using HttpClient officer = await fixture.Client(Roles.EquipmentOfficer);
            using HttpClient member = await fixture.Client(Roles.Firefighter);
            VehicleDto vehicle = await CreateVehicle(officer);
            DateOnly start = fixture.Today.AddDays(30);
            HttpResponseMessage response = await member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(2) });
            response.EnsureSuccessStatusCode();
            VehicleReservationDto reservation = (await response.Content.ReadFromJsonAsync<VehicleReservationDto>())!;
            HttpResponseMessage[] results = await Task.WhenAll(
                officer.PostAsJsonAsync($"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = 1 }),
                officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest { ExpectedVersion = 2, IsActive = false }));
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            bool active = (await db.Vehicles.FindAsync(vehicle.Id))!.IsActive;
            Assert.Equal(active ? 3 : 0, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
        }

        private async Task<VehicleDto> CreateVehicle(HttpClient client)
        {
            using IServiceScope scope = fixture.Factory.Services.CreateScope();
            FeuerwehrDbContext db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            ApplicationUser officer = await db.Users.SingleAsync(x => x.Email == $"{Roles.EquipmentOfficer}@integration.test");
            if (!officer.FireDepartmentId.HasValue)
            {
                FireDepartment department = new FireDepartment { Name = "MTW Integration" };
                db.FireDepartments.Add(department); await db.SaveChangesAsync();
                officer.FireDepartmentId = department.Id; await db.SaveChangesAsync();
            }
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/vehicles", new SaveVehicleRequest
            { FireDepartmentId = officer.FireDepartmentId.Value, EquipmentOfficerId = officer.Id, RadioCallSign = $"Florian {Guid.NewGuid():N}" });
            Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
        }
    }

}
