using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using de.openelp.feuerwehr.Common.Models.Vehicles;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace de.openelp.feuerwehr.Tests.Backend.Integration;

public sealed class VehicleApiTests(InventoryApiFixture fixture) : IClassFixture<InventoryApiFixture>
{
    [Fact]
    public async Task ImportExportUsesOfficerEmailAndPreservesBookingsWithRealPostgresAndHttpUploads()
    {
        using var admin = await fixture.Client(Roles.Admin);
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        var vehicle = await CreateVehicle(officer);
        var booking = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
            { CommandId = Guid.NewGuid(), From = fixture.Today.AddDays(5), To = fixture.Today.AddDays(6) });
        booking.EnsureSuccessStatusCode();
        var initialStatusChange = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id).StatusChangedAtUtc;
        Assert.NotNull(initialStatusChange);
        var types = await admin.GetByteArrayAsync("/api/vehicles/types/export");
        using (var form = Upload(types, "fahrzeugtypen.json"))
            (await admin.PostAsync("/api/vehicles/types/import", form)).EnsureSuccessStatusCode();
        var csv = await admin.GetStringAsync("/api/vehicles/export");
        Assert.Contains("equipmentOfficerEmail", csv);
        Assert.Contains($"{Roles.EquipmentOfficer}@integration.test", csv);
        Assert.DoesNotContain(vehicle.EquipmentOfficerId, csv);
        using (var form = Upload(Encoding.UTF8.GetBytes(csv.Replace($"{Roles.EquipmentOfficer}@integration.test", $"{Roles.EquipmentOfficer.ToUpperInvariant()}@INTEGRATION.TEST")), "fahrzeuge.csv"))
        {
            var imported = await admin.PostAsync("/api/vehicles/import", form);
            Assert.True(imported.IsSuccessStatusCode, await imported.Content.ReadAsStringAsync());
            Assert.True((await imported.Content.ReadFromJsonAsync<VehicleImportResponse>())!.UpdatedCount > 0);
        }
        var stored = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles?includeInactive=true"))!.Single(x => x.Id == vehicle.Id);
        Assert.Equal(vehicle.EquipmentOfficerId, stored.EquipmentOfficerId);
        Assert.Equal(initialStatusChange, stored.StatusChangedAtUtc);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        Assert.Equal(2, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
        Assert.Single(await db.VehicleReservations.Where(x => x.VehicleId == vehicle.Id).ToListAsync());
    }

    [Fact]
    public async Task InvalidEmailRollsBackAllCsvRowsAndReturnsTheirLineNumber()
    {
        using var admin = await fixture.Client(Roles.Admin);
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        var vehicle = await CreateVehicle(officer);
        var csv = $"id;radioCallSign;name;fireDepartment;equipmentOfficerEmail\n{vehicle.Id};{vehicle.RadioCallSign};Nicht speichern;MTW Integration;{Roles.EquipmentOfficer}@integration.test\n" +
            " ;Neues Fahrzeug;Fehler;MTW Integration;missing@integration.test";
        using var form = Upload(Encoding.UTF8.GetBytes(csv), "fahrzeuge.csv");
        var response = await admin.PostAsync("/api/vehicles/import", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains((await response.Content.ReadFromJsonAsync<VehicleImportResponse>())!.Errors, x => x.LineNumber == 3);
        var stored = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id);
        Assert.Null(stored.Name);
    }

    [Fact]
    public async Task CustomStatusesAreAdminManagedAndMtwAvailabilityExcludesOtherVehicleTypes()
    {
        using var admin = await fixture.Client(Roles.Admin);
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        var vehicle = await CreateVehicle(officer);
        var statusName = $"Außer Dienst {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/vehicles/statuses", new SaveVehicleStatusRequest { Name = statusName })).StatusCode);
        var response = await admin.PostAsJsonAsync("/api/vehicles/statuses", new SaveVehicleStatusRequest { Name = statusName, AllowsBooking = false });
        response.EnsureSuccessStatusCode();
        var status = (await response.Content.ReadFromJsonAsync<VehicleStatusDto>())!;
        var types = (await admin.GetFromJsonAsync<VehicleTypeDto[]>("/api/vehicles/types"))!;
        var mtw = types.SingleOrDefault(x => x.Name == "MTW");
        if (mtw is null)
        {
            var created = await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = "MTW" });
            created.EnsureSuccessStatusCode(); mtw = (await created.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
        }
        var changed = await officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new SaveVehicleRequest
        {
            RadioCallSign = vehicle.RadioCallSign, FireDepartmentId = vehicle.FireDepartmentId, EquipmentOfficerId = vehicle.EquipmentOfficerId,
            ExpectedVersion = vehicle.Version, VehicleTypeId = mtw.Id, VehicleStatusId = status.Id
        });
        changed.EnsureSuccessStatusCode();
        var updated = (await changed.Content.ReadFromJsonAsync<VehicleDto>())!;
        Assert.Equal(statusName, updated.VehicleStatusName); Assert.False(updated.AllowsBooking);
        Assert.NotNull(updated.StatusChangedAtUtc);
        var from = fixture.Today.AddDays(2);
        var calendar = (await officer.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={from.AddDays(2):yyyy-MM-dd}&vehicleType=MTW"))!;
        Assert.All(calendar, x => Assert.Equal("MTW", x.Vehicle.VehicleTypeName));
        Assert.All(calendar.Single(x => x.Vehicle.Id == vehicle.Id).Days, day => Assert.Equal(VehicleDayStatus.Unavailable, day.Status));
        Assert.Equal(HttpStatusCode.Conflict, (await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
            { CommandId = Guid.NewGuid(), From = from, To = from })).StatusCode);
    }

    private static MultipartFormDataContent Upload(byte[] content, string name)
    {
        var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(content), "file", name); return form;
    }

    [Fact]
    public async Task VehicleTypesAreAdminManaged_AndAppearInVehicleAndCalendarResponses()
    {
        using var admin = await fixture.Client(Roles.Admin);
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        var name = $"MTW {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Forbidden, (await officer.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name })).StatusCode);
        var response = await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name });
        response.EnsureSuccessStatusCode();
        var type = (await response.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/vehicles/types", new SaveVehicleTypeRequest { Name = name.ToLowerInvariant() })).StatusCode);
        var vehicle = await CreateVehicle(officer);
        var assigned = await officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}", new SaveVehicleRequest
        {
            FireDepartmentId = vehicle.FireDepartmentId, EquipmentOfficerId = vehicle.EquipmentOfficerId,
            RadioCallSign = vehicle.RadioCallSign, ExpectedVersion = vehicle.Version, VehicleTypeId = type.Id
        });
        assigned.EnsureSuccessStatusCode();
        Assert.Equal(name, (await assigned.Content.ReadFromJsonAsync<VehicleDto>())!.VehicleTypeName);
        var calendar = await officer.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={fixture.Today:yyyy-MM-dd}&to={fixture.Today.AddDays(20):yyyy-MM-dd}");
        Assert.Equal(type.Id, calendar!.Single(x => x.Vehicle.Id == vehicle.Id).Vehicle.VehicleTypeId);
        var updated = await admin.PutAsJsonAsync($"/api/vehicles/types/{type.Id}", new SaveVehicleTypeRequest { Name = name + " neu", ExpectedVersion = type.Version });
        updated.EnsureSuccessStatusCode();
        var renamed = (await updated.Content.ReadFromJsonAsync<VehicleTypeDto>())!;
        var deactivated = await admin.PutAsJsonAsync($"/api/vehicles/types/{type.Id}/active", new SetVehicleTypeActiveRequest { IsActive = false, ExpectedVersion = renamed.Version });
        deactivated.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await officer.GetFromJsonAsync<VehicleTypeDto[]>("/api/vehicles/types"))!, x => x.Id == type.Id);
        Assert.Equal(name + " neu", (await officer.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id).VehicleTypeName);
    }

    [Fact]
    public async Task ConcurrentApprovals_ClaimTheEntireRangeExactlyOnce_AndSendHtmlWithInlineLogoAndComment()
    {
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        using var member = await fixture.Client(Roles.Firefighter);
        var vehicle = await CreateVehicle(officer);
        var from = fixture.Today.AddDays(3);
        var to = from.AddDays(3);
        var comment = "Jugendfeuerwehr <Ausflug> & Ausbildung";
        var requests = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations",
            new CreateVehicleReservationRequest { CommandId = Guid.NewGuid(), From = from, To = to, Comment = comment })));
        Assert.All(requests, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        var reservations = await Task.WhenAll(requests.Select(async response => (await response.Content.ReadFromJsonAsync<VehicleReservationDto>())!));
        var pending = await member.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        Assert.All(pending!.Single(x => x.Vehicle.Id == vehicle.Id).Days, x => Assert.Equal(VehicleDayStatus.Pending, x.Status));
        var decisions = await Task.WhenAll(reservations.Select(reservation => officer.PostAsJsonAsync(
            $"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = reservation.Version })));
        Assert.Single(decisions, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(decisions, x => x.StatusCode == HttpStatusCode.Conflict);

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        var days = await db.VehicleBookingDays.Where(x => x.VehicleId == vehicle.Id).ToListAsync();
        Assert.Equal(4, days.Count);
        var winner = Assert.Single(days.Select(x => x.ReservationId).Distinct());
        var stored = await db.VehicleReservations.SingleAsync(x => x.Id == winner);
        Assert.Equal(VehicleReservationStatus.Confirmed, stored.Status);
        Assert.Equal(1, await db.VehicleMailMessages.CountAsync(x => x.ReservationId == winner && x.Kind == "Confirmed"));
        var booked = await member.GetFromJsonAsync<VehicleAvailabilityDto[]>($"/api/vehicles/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        Assert.All(booked!.Single(x => x.Vehicle.Id == vehicle.Id).Days, x => Assert.Equal(VehicleDayStatus.Booked, x.Status));

        await scope.ServiceProvider.GetRequiredService<IVehicleMailDispatcher>().Dispatch(default);
        using var mail = new HttpClient { BaseAddress = fixture.MailpitUrl };
        var messages = await mail.GetFromJsonAsync<JsonElement>("/api/v1/messages");
        var matching = messages.GetProperty("messages").EnumerateArray().Where(x =>
            x.GetProperty("Subject").GetString() == $"MTW-Buchung bestätigt: {vehicle.RadioCallSign}").ToArray();
        var delivered = Assert.Single(matching);
        var body = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{delivered.GetProperty("ID").GetString()}");
        Assert.Contains(comment, body.GetProperty("Text").GetString());
        var html = body.GetProperty("HTML").GetString()!;
        Assert.Contains("cid:feuerwehr-logo", html);
        Assert.Contains("&lt;Ausflug&gt;", html);
        Assert.DoesNotContain("<Ausflug>", html);
        Assert.NotEmpty(body.GetProperty("Inline").EnumerateArray());
        var notifications = messages.GetProperty("messages").EnumerateArray().Where(x =>
            x.GetProperty("Subject").GetString() == $"Neue MTW-Reservierungsanfrage: {vehicle.RadioCallSign}").ToArray();
        Assert.Equal(2, notifications.Length);
        foreach (var notification in notifications)
        {
            var notificationBody = await mail.GetFromJsonAsync<JsonElement>($"/api/v1/message/{notification.GetProperty("ID").GetString()}");
            Assert.Contains(comment, notificationBody.GetProperty("Text").GetString());
        }
    }

    [Fact]
    public async Task ParallelDirectBookingAndApproval_CannotPartiallyBookOverlappingRanges()
    {
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        using var member = await fixture.Client(Roles.Firefighter);
        var vehicle = await CreateVehicle(officer);
        var start = fixture.Today.AddDays(10);
        var request = await member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
            { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(2) });
        request.EnsureSuccessStatusCode();
        var reservation = (await request.Content.ReadFromJsonAsync<VehicleReservationDto>())!;
        var results = await Task.WhenAll(
            officer.PostAsJsonAsync($"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = 1 }),
            officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", new CreateVehicleReservationRequest
                { CommandId = Guid.NewGuid(), From = start.AddDays(1), To = start.AddDays(3) }));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        var days = await db.VehicleBookingDays.Where(x => x.VehicleId == vehicle.Id).ToListAsync();
        Assert.Equal(3, days.Count);
        Assert.Single(days.Select(x => x.ReservationId).Distinct());
    }

    [Fact]
    public async Task VehicleApiEnforcesOwnershipAndRoles_AndPreservesBookingsAfterDeactivation()
    {
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        using var admin = await fixture.Client(Roles.Admin);
        using var member = await fixture.Client(Roles.Firefighter);
        using var viewer = await fixture.Client(Roles.InventoryViewer);
        var vehicle = await CreateVehicle(officer);
        var start = fixture.Today.AddDays(20);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/Users/{vehicle.EquipmentOfficerId}/permanent")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
            { CommandId = Guid.NewGuid(), From = start, To = start })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest
            { IsActive = false, ExpectedVersion = vehicle.Version })).StatusCode);
        var command = new CreateVehicleReservationRequest { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(1) };
        var created = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", command);
        created.EnsureSuccessStatusCode();
        var replay = await officer.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/direct-bookings", command);
        Assert.Equal((await created.Content.ReadFromJsonAsync<VehicleReservationDto>())!.Id, (await replay.Content.ReadFromJsonAsync<VehicleReservationDto>())!.Id);
        var current = (await admin.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!.Single(x => x.Id == vehicle.Id);
        var deactivated = await admin.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest { ExpectedVersion = current.Version, IsActive = false });
        deactivated.EnsureSuccessStatusCode();
        Assert.DoesNotContain((await member.GetFromJsonAsync<VehicleDto[]>("/api/vehicles"))!, x => x.Id == vehicle.Id);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        Assert.Equal(2, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
        Assert.Single(await db.VehicleReservations.Where(x => x.VehicleId == vehicle.Id).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentDeactivationAndApproval_UsesTheSameVehicleLock()
    {
        using var officer = await fixture.Client(Roles.EquipmentOfficer);
        using var member = await fixture.Client(Roles.Firefighter);
        var vehicle = await CreateVehicle(officer);
        var start = fixture.Today.AddDays(30);
        var response = await member.PostAsJsonAsync($"/api/vehicles/{vehicle.Id}/reservations", new CreateVehicleReservationRequest
            { CommandId = Guid.NewGuid(), From = start, To = start.AddDays(2) });
        response.EnsureSuccessStatusCode();
        var reservation = (await response.Content.ReadFromJsonAsync<VehicleReservationDto>())!;
        var results = await Task.WhenAll(
            officer.PostAsJsonAsync($"/api/vehicles/reservations/{reservation.Id}/approve", new DecideVehicleReservationRequest { ExpectedVersion = 1 }),
            officer.PutAsJsonAsync($"/api/vehicles/{vehicle.Id}/active", new SetVehicleActiveRequest { ExpectedVersion = 2, IsActive = false }));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        var active = (await db.Vehicles.FindAsync(vehicle.Id))!.IsActive;
        Assert.Equal(active ? 3 : 0, await db.VehicleBookingDays.CountAsync(x => x.VehicleId == vehicle.Id));
    }

    private async Task<VehicleDto> CreateVehicle(HttpClient client)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        var officer = await db.Users.SingleAsync(x => x.Email == $"{Roles.EquipmentOfficer}@integration.test");
        if (!officer.FireDepartmentId.HasValue)
        {
            var department = new FireDepartment { Name = "MTW Integration" };
            db.FireDepartments.Add(department); await db.SaveChangesAsync();
            officer.FireDepartmentId = department.Id; await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/vehicles", new SaveVehicleRequest
        { FireDepartmentId = officer.FireDepartmentId.Value, EquipmentOfficerId = officer.Id, RadioCallSign = $"Florian {Guid.NewGuid():N}" });
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }
}
