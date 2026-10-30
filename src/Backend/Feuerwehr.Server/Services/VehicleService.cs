using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using de.openelp.feuerwehr.Common.Models.Dto;
using de.openelp.feuerwehr.Common.Models.Vehicles;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Server.Models.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace de.openelp.feuerwehr.Server.Services;

public interface IVehicleService
{
    Task<ServiceResult> Types(bool includeInactive, CancellationToken ct);
    Task<ServiceResult> Statuses(bool includeInactive, CancellationToken ct);
    Task<ServiceResult> SaveStatus(Guid? id, SaveVehicleStatusRequest request, CancellationToken ct);
    Task<ServiceResult> SaveType(Guid? id, SaveVehicleTypeRequest request, CancellationToken ct);
    Task<ServiceResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct);
    Task<ServiceResult> List(bool includeInactive, CancellationToken ct);
    Task<ServiceResult> Availability(DateOnly from, DateOnly to, CancellationToken ct, string? vehicleType = null);
    Task<ServiceResult> ManagementOptions(CancellationToken ct);
    Task<ServiceResult> Save(Guid? id, SaveVehicleRequest request, CancellationToken ct);
    Task<ServiceResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct);
    Task<ServiceResult> Reservations(string scope, CancellationToken ct);
    Task<ServiceResult> Reserve(Guid id, CreateVehicleReservationRequest request, bool direct, CancellationToken ct);
    Task<ServiceResult> Decide(Guid id, DecideVehicleReservationRequest request, VehicleReservationStatus status, CancellationToken ct);
}

public sealed class VehicleService(FeuerwehrDbContext db, IHttpContextAccessor http, TimeProvider clock,
    IVehicleMailComposer mail) : IVehicleService
{
    private ClaimsPrincipal Principal => http.HttpContext?.User ?? new ClaimsPrincipal();
    private string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private bool IsAdmin => Principal.IsInRole(Roles.Admin);
    private bool IsOfficer => Principal.IsInRole(Roles.EquipmentOfficer);
    private bool CanManage => IsAdmin || IsOfficer;
    private bool CanRequest => Principal.IsInRole(Roles.Commander) || Principal.IsInRole(Roles.Firefighter);
    private bool Owns(Vehicle vehicle) => IsOfficer && vehicle.EquipmentOfficerId == UserId;
    private IQueryable<Vehicle> Vehicles => db.Vehicles.Include(x => x.FireDepartment).Include(x => x.EquipmentOfficer).Include(x => x.VehicleType).Include(x => x.VehicleStatus);
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(),
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin")).DateTime);
    private static ServiceResult Invalid(string message) => ServiceResult.BadRequest(new MessageResponse(message));
    private static ServiceResult Conflict(string message) => ServiceResult.StatusCode(409, new MessageResponse(message));

    public async Task<ServiceResult> Statuses(bool includeInactive, CancellationToken ct) => ServiceResult.Ok(await db.Set<VehicleStatus>()
        .AsNoTracking().Where(x => includeInactive || x.IsActive).OrderByDescending(x => x.Id == VehicleStatusDefaults.ReadyId).ThenBy(x => x.Name)
        .Select(x => new VehicleStatusDto(x.Id, x.Name, x.AllowsBooking, x.IsActive, x.Version, x.Id == VehicleStatusDefaults.ReadyId)).ToListAsync(ct));

    public Task<ServiceResult> SaveStatus(Guid? id, SaveVehicleStatusRequest request, CancellationToken ct) => Execute(async () =>
    {
        if (!IsAdmin) return ServiceResult.Forbid();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return Invalid("Bitte einen Status mit höchstens 100 Zeichen angeben.");
        var status = id.HasValue ? await db.Set<VehicleStatus>().SingleOrDefaultAsync(x => x.Id == id.Value, ct) : new VehicleStatus { Id = Guid.NewGuid() };
        if (status is null) return ServiceResult.NotFound();
        if (id.HasValue && status.Version != request.ExpectedVersion) return Conflict("Der Status wurde inzwischen geändert. Bitte neu laden.");
        if (status.Id == VehicleStatusDefaults.ReadyId && !request.IsActive) return Invalid("Der Standardstatus kann nicht deaktiviert werden.");
        var normalized = request.Name.Trim().ToUpperInvariant();
        if (await db.Set<VehicleStatus>().AnyAsync(x => x.NormalizedName == normalized && x.Id != status.Id, ct))
            return Conflict("Ein Status mit diesem Namen existiert bereits.");
        status.Name = request.Name.Trim(); status.NormalizedName = normalized;
        status.AllowsBooking = request.AllowsBooking; status.IsActive = request.IsActive;
        if (id.HasValue) status.Version++; else db.Set<VehicleStatus>().Add(status);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (IsConflict(exception)) { return Conflict("Der Status wurde inzwischen geändert oder existiert bereits."); }
        return ServiceResult.Ok(new VehicleStatusDto(status.Id, status.Name, status.AllowsBooking, status.IsActive, status.Version, status.Id == VehicleStatusDefaults.ReadyId));
    });

    public async Task<ServiceResult> Types(bool includeInactive, CancellationToken ct) => ServiceResult.Ok(await db.Set<VehicleType>()
        .AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name)
        .Select(x => new VehicleTypeDto(x.Id, x.Name, x.IsActive, x.Version)).ToListAsync(ct));

    public Task<ServiceResult> SaveType(Guid? id, SaveVehicleTypeRequest request, CancellationToken ct) => Execute(async () =>
    {
        if (!IsAdmin) return ServiceResult.Forbid();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
            return Invalid("Bitte einen Fahrzeugtyp mit höchstens 100 Zeichen angeben.");
        var name = request.Name.Trim();
        var normalized = name.ToUpperInvariant();
        var type = id.HasValue ? await db.Set<VehicleType>().SingleOrDefaultAsync(x => x.Id == id.Value, ct) : new VehicleType { Id = Guid.NewGuid() };
        if (type is null) return ServiceResult.NotFound();
        if (id.HasValue && type.Version != request.ExpectedVersion) return Conflict("Der Fahrzeugtyp wurde inzwischen geändert. Bitte neu laden.");
        if (await db.Set<VehicleType>().AnyAsync(x => x.NormalizedName == normalized && x.Id != type.Id, ct))
            return Conflict("Ein Fahrzeugtyp mit diesem Namen existiert bereits.");
        type.Name = name;
        type.NormalizedName = normalized;
        if (id.HasValue) type.Version++; else db.Set<VehicleType>().Add(type);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (IsConflict(exception)) { return Conflict("Der Fahrzeugtyp wurde inzwischen geändert oder existiert bereits. Bitte neu laden."); }
        return ServiceResult.Ok(new VehicleTypeDto(type.Id, type.Name, type.IsActive, type.Version));
    });

    public Task<ServiceResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct) => Execute(async () =>
    {
        if (!IsAdmin) return ServiceResult.Forbid();
        var type = await db.Set<VehicleType>().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (type is null) return ServiceResult.NotFound();
        if (type.Version != request.ExpectedVersion) return Conflict("Der Fahrzeugtyp wurde inzwischen geändert. Bitte neu laden.");
        type.IsActive = request.IsActive;
        type.Version++;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict("Der Fahrzeugtyp wurde inzwischen geändert. Bitte neu laden."); }
        return ServiceResult.Ok(new VehicleTypeDto(type.Id, type.Name, type.IsActive, type.Version));
    });

    public async Task<ServiceResult> List(bool includeInactive, CancellationToken ct)
    {
        var query = Vehicles.AsNoTracking();
        if (!includeInactive || !CanManage) query = query.Where(x => x.IsActive);
        return ServiceResult.Ok((await query.OrderBy(x => x.FireDepartment.Name).ThenBy(x => x.RadioCallSign).ToListAsync(ct)).Select(ToDto).ToList());
    }

    public async Task<ServiceResult> Availability(DateOnly from, DateOnly to, CancellationToken ct, string? vehicleType = null)
    {
        if (from == default || to < from || to.DayNumber - from.DayNumber > 365)
            return Invalid("Bitte einen gültigen Kalenderzeitraum von höchstens 366 Tagen wählen.");
        var query = Vehicles.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            var normalizedType = vehicleType.Trim().ToUpperInvariant();
            query = query.Where(x => x.VehicleType != null && x.VehicleType.NormalizedName == normalizedType);
        }
        var vehicles = await query
            .OrderBy(x => x.FireDepartment.Name).ThenBy(x => x.RadioCallSign).ToListAsync(ct);
        var bookings = await db.VehicleBookingDays.AsNoTracking().Where(x => x.Date >= from && x.Date <= to).ToListAsync(ct);
        var pending = await db.VehicleReservations.AsNoTracking().Where(x => x.Status == VehicleReservationStatus.Pending && x.From <= to && x.To >= from)
            .Select(x => new { x.VehicleId, x.From, x.To }).ToListAsync(ct);
        var booked = bookings.Select(x => (x.VehicleId, x.Date)).ToHashSet();
        return ServiceResult.Ok(vehicles.Select(vehicle => new VehicleAvailabilityDto(ToDto(vehicle),
            Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(offset =>
            {
                var day = from.AddDays(offset);
                var status = booked.Contains((vehicle.Id, day)) ? VehicleDayStatus.Booked : !vehicle.VehicleStatus.AllowsBooking ? VehicleDayStatus.Unavailable :
                    pending.Any(x => x.VehicleId == vehicle.Id && x.From <= day && x.To >= day) ? VehicleDayStatus.Pending : VehicleDayStatus.Free;
                return new VehicleDayDto(day, status);
            }).ToList())).ToList());
    }

    public async Task<ServiceResult> ManagementOptions(CancellationToken ct)
    {
        if (!CanManage) return ServiceResult.Forbid();
        var actor = await Actor(ct);
        if (actor is null) return ServiceResult.Forbid();
        var departments = db.FireDepartments.AsNoTracking();
        if (!IsAdmin) departments = departments.Where(x => x.Id == actor.FireDepartmentId);
        var departmentList = await departments.OrderBy(x => x.Name).Select(x => new VehicleDepartmentDto(x.Id, x.Name)).ToListAsync(ct);
        var ids = departmentList.Select(x => x.Id).ToArray();
        var officers = await (from user in db.Users
                              join membership in db.UserRoles on user.Id equals membership.UserId
                              join role in db.Roles on membership.RoleId equals role.Id
                              where user.IsActive && user.Email != null && user.FireDepartmentId.HasValue && ids.Contains(user.FireDepartmentId.Value)
                                  && role.Name == Roles.EquipmentOfficer
                              orderby user.LastName, user.FirstName
                              select new VehicleOfficerDto(user.Id, user.FirstName + " " + user.LastName, user.FireDepartmentId!.Value)).ToListAsync(ct);
        return ServiceResult.Ok(new VehicleManagementOptionsDto(departmentList, officers, VehicleStatusDefaults.ReadyId));
    }

    public Task<ServiceResult> Save(Guid? id, SaveVehicleRequest request, CancellationToken ct) => Execute(() => SaveCore(id, request, ct));

    private async Task<ServiceResult> SaveCore(Guid? id, SaveVehicleRequest request, CancellationToken ct)
    {
        if (!CanManage) return ServiceResult.Forbid();
        var actor = await Actor(ct);
        if (actor is null) return ServiceResult.Forbid();
        await using var transaction = await Begin(ct);
        var vehicle = id.HasValue ? await LockVehicle(id.Value, ct) : new Vehicle { Id = Guid.NewGuid() };
        if (vehicle is null) return ServiceResult.NotFound();
        if (id.HasValue && !IsAdmin && !Owns(vehicle)) return ServiceResult.Forbid();
        if (!IsAdmin && request.FireDepartmentId != actor.FireDepartmentId) return ServiceResult.Forbid();
        if (string.IsNullOrWhiteSpace(request.RadioCallSign) || request.RadioCallSign.Trim().Length > 100 ||
            request.Name?.Length > 150 || request.LicensePlate?.Length > 30)
            return Invalid("Ein Funkrufname ist erforderlich; bitte die maximalen Feldlängen beachten.");
        var department = await db.FireDepartments.FindAsync([request.FireDepartmentId], ct);
        var officer = await db.Users.SingleOrDefaultAsync(x => x.Id == request.EquipmentOfficerId && x.IsActive && x.Email != null &&
            x.FireDepartmentId == request.FireDepartmentId, ct);
        if (department is null || officer is null || !await HasOfficerRole(officer.Id, ct))
            return Invalid("Bitte eine Feuerwehr und einen aktiven Gerätewart dieser Feuerwehr mit E-Mail-Adresse wählen.");
        var statusId = request.VehicleStatusId ?? vehicle.VehicleStatusId;
        var vehicleStatus = await db.Set<VehicleStatus>().SingleOrDefaultAsync(x => x.Id == statusId, ct);
        if (vehicleStatus is null || (!vehicleStatus.IsActive && (!id.HasValue || vehicle.VehicleStatusId != statusId)))
            return Invalid("Bitte einen aktiven Fahrzeugstatus wählen.");
        VehicleType? vehicleType = null;
        if (request.VehicleTypeId.HasValue)
        {
            vehicleType = await db.Set<VehicleType>().SingleOrDefaultAsync(x => x.Id == request.VehicleTypeId, ct);
            if (vehicleType is null || (!vehicleType.IsActive && (!id.HasValue || vehicle.VehicleTypeId != vehicleType.Id)))
                return Invalid("Bitte einen aktiven Fahrzeugtyp wählen.");
        }
        if (id.HasValue && vehicle.Version != request.ExpectedVersion) return Conflict("Das Fahrzeug wurde inzwischen geändert. Bitte neu laden.");
        vehicle.FireDepartmentId = department.Id;
        vehicle.FireDepartment = department;
        vehicle.EquipmentOfficerId = officer.Id;
        vehicle.EquipmentOfficer = officer;
        vehicle.RadioCallSign = request.RadioCallSign.Trim();
        vehicle.Name = Clean(request.Name);
        vehicle.LicensePlate = Clean(request.LicensePlate);
        vehicle.VehicleTypeId = vehicleType?.Id;
        vehicle.VehicleType = vehicleType;
        if (!id.HasValue || vehicle.VehicleStatusId != vehicleStatus.Id)
            vehicle.StatusChangedAtUtc = Now;
        vehicle.VehicleStatusId = vehicleStatus.Id;
        vehicle.VehicleStatus = vehicleStatus;
        if (id.HasValue) vehicle.Version++; else db.Vehicles.Add(vehicle);
        Audit(vehicle, null, id.HasValue ? "VehicleUpdated" : "VehicleCreated", JsonSerializer.Serialize(request));
        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { return Conflict("Das Fahrzeug wurde inzwischen geändert. Bitte neu laden."); }
        return ServiceResult.Ok(ToDto(vehicle));
    }

    public Task<ServiceResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct) => Execute(() => SetActiveCore(id, request, ct));

    private async Task<ServiceResult> SetActiveCore(Guid id, SetVehicleActiveRequest request, CancellationToken ct)
    {
        await using var transaction = await Begin(ct);
        var vehicle = await LockVehicle(id, ct);
        if (vehicle is null) return ServiceResult.NotFound();
        if (!IsAdmin && !Owns(vehicle)) return ServiceResult.Forbid();
        if (vehicle.Version != request.ExpectedVersion) return Conflict("Das Fahrzeug wurde inzwischen geändert. Bitte neu laden.");
        vehicle.IsActive = request.IsActive;
        vehicle.Version++;
        Audit(vehicle, null, request.IsActive ? "VehicleActivated" : "VehicleDeactivated", "Bestehende Buchungen bleiben erhalten.");
        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException) { return Conflict("Das Fahrzeug wurde inzwischen geändert. Bitte neu laden."); }
        return ServiceResult.Ok(ToDto(vehicle));
    }

    public async Task<ServiceResult> Reservations(string scope, CancellationToken ct)
    {
        var query = db.VehicleReservations.AsNoTracking().Include(x => x.Vehicle).AsQueryable();
        if (scope == "mine") query = query.Where(x => x.RequesterId == UserId);
        else if (scope == "assigned")
        {
            if (!IsOfficer) return ServiceResult.Forbid();
            query = query.Where(x => x.Vehicle.EquipmentOfficerId == UserId);
        }
        else return Invalid("Als Bereich bitte mine oder assigned angeben.");
        return ServiceResult.Ok((await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)).Select(ToDto).ToList());
    }

    public Task<ServiceResult> Reserve(Guid id, CreateVehicleReservationRequest request, bool direct, CancellationToken ct) => Execute(() => ReserveCore(id, request, direct, ct));

    private async Task<ServiceResult> ReserveCore(Guid id, CreateVehicleReservationRequest request, bool direct, CancellationToken ct)
    {
        if (!direct && !CanRequest) return ServiceResult.Forbid();
        if (direct && !IsOfficer) return ServiceResult.Forbid();
        if (request.CommandId == Guid.Empty) return Invalid("Eine CommandId ist erforderlich.");
        if (request.From == default || request.To < request.From || request.To.DayNumber - request.From.DayNumber > 365 || request.To == DateOnly.MaxValue)
            return Invalid("Bitte einen gültigen Zeitraum von höchstens 366 Tagen wählen.");
        if (request.Comment?.Length > 2000) return Invalid("Der Kommentar darf höchstens 2000 Zeichen enthalten.");
        var actor = await Actor(ct);
        if (actor is null || string.IsNullOrWhiteSpace(actor.Email)) return ServiceResult.Forbid();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            { VehicleId = id, request.From, request.To, Comment = Clean(request.Comment), Direct = direct }))));
        await using var transaction = await Begin(ct);
        var vehicle = await LockVehicle(id, ct);
        if (vehicle is null) return ServiceResult.NotFound();
        if (direct && !Owns(vehicle)) return ServiceResult.Forbid();
        var existing = await db.VehicleReservations.SingleOrDefaultAsync(x => x.CommandId == request.CommandId, ct);
        if (existing is not null) return Replay(existing, hash);
        if (!vehicle.IsActive) return Conflict("Dieses Fahrzeug ist deaktiviert und kann nicht gebucht werden.");
        if (!vehicle.VehicleStatus.AllowsBooking) return Conflict($"Das Fahrzeug kann im Status {vehicle.VehicleStatus.Name} nicht gebucht werden.");
        if (request.From < Today) return Invalid("Buchungen in der Vergangenheit sind nicht möglich.");
        if (!vehicle.EquipmentOfficer.IsActive || string.IsNullOrWhiteSpace(vehicle.EquipmentOfficer.Email) ||
            !await HasOfficerRole(vehicle.EquipmentOfficerId, ct))
            return Conflict("Der zuständige Gerätewart ist nicht verfügbar. Bitte die Fahrzeugverwaltung kontaktieren.");
        var collision = await Collision(id, request.From, request.To, ct);
        if (collision is not null) return collision;
        var reservation = new VehicleReservation
        {
            Id = Guid.NewGuid(), VehicleId = id, Vehicle = vehicle, CommandId = request.CommandId, PayloadHash = hash,
            RequesterId = actor.Id, RequesterName = actor.FullName, RequesterEmail = actor.Email!,
            RadioCallSign = vehicle.RadioCallSign, FireDepartmentName = vehicle.FireDepartment.Name,
            From = request.From, To = request.To, Comment = Clean(request.Comment), CreatedAtUtc = Now,
            Status = direct ? VehicleReservationStatus.Confirmed : VehicleReservationStatus.Pending, IsDirectBooking = direct,
            DecidedAtUtc = direct ? Now : null, DecidedById = direct ? UserId : null
        };
        db.VehicleReservations.Add(reservation);
        if (direct) AddDays(reservation);
        vehicle.Version++;
        Audit(vehicle, reservation, direct ? "DirectBooking" : "ReservationRequested", reservation.Comment ?? "");
        db.VehicleMailMessages.Add(mail.Compose(reservation, direct ? actor.Email! : vehicle.EquipmentOfficer.Email!, !direct));
        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException exception) when (IsConflict(exception))
        {
            if (transaction is not null) await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            existing = await db.VehicleReservations.Include(x => x.Vehicle).SingleOrDefaultAsync(x => x.CommandId == request.CommandId, ct);
            return existing is not null ? Replay(existing, hash) : Conflict("Der Zeitraum oder das Fahrzeug wurde inzwischen geändert. Bitte neu laden.");
        }
        return ServiceResult.Ok(ToDto(reservation));
    }

    public Task<ServiceResult> Decide(Guid id, DecideVehicleReservationRequest request, VehicleReservationStatus status, CancellationToken ct) => Execute(() => DecideCore(id, request, status, ct));

    private async Task<ServiceResult> DecideCore(Guid id, DecideVehicleReservationRequest request, VehicleReservationStatus status, CancellationToken ct)
    {
        if (status is not (VehicleReservationStatus.Confirmed or VehicleReservationStatus.Rejected or VehicleReservationStatus.Cancelled))
            return Invalid("Ungültige Entscheidung.");
        var vehicleId = await db.VehicleReservations.Where(x => x.Id == id).Select(x => (Guid?)x.VehicleId).SingleOrDefaultAsync(ct);
        if (!vehicleId.HasValue) return ServiceResult.NotFound();
        await using var transaction = await Begin(ct);
        var vehicle = (await LockVehicle(vehicleId.Value, ct))!;
        var reservation = (await db.VehicleReservations.SingleAsync(x => x.Id == id, ct));
        var owns = Owns(vehicle);
        if (status == VehicleReservationStatus.Cancelled)
        {
            if (!owns && !(reservation.RequesterId == UserId && reservation.Status is VehicleReservationStatus.Pending or VehicleReservationStatus.Cancelled))
                return ServiceResult.Forbid();
        }
        else if (!owns) return ServiceResult.Forbid();
        if (reservation.Status == status) return ServiceResult.Ok(ToDto(reservation));
        if (reservation.Version != request.ExpectedVersion || reservation.Status is VehicleReservationStatus.Rejected or VehicleReservationStatus.Cancelled ||
            (reservation.Status == VehicleReservationStatus.Confirmed && status != VehicleReservationStatus.Cancelled))
            return Conflict("Die Anfrage wurde inzwischen bearbeitet. Bitte neu laden.");
        if (status == VehicleReservationStatus.Confirmed)
        {
            if (!vehicle.IsActive) return Conflict("Das Fahrzeug wurde deaktiviert und kann nicht mehr gebucht werden.");
            if (!vehicle.VehicleStatus.AllowsBooking) return Conflict($"Das Fahrzeug kann im Status {vehicle.VehicleStatus.Name} nicht gebucht werden.");
            if (reservation.From < Today) return Conflict("Der angefragte Zeitraum liegt inzwischen in der Vergangenheit.");
            var collision = await Collision(vehicle.Id, reservation.From, reservation.To, ct);
            if (collision is not null) return collision;
            AddDays(reservation);
            db.VehicleMailMessages.Add(mail.Compose(reservation, reservation.RequesterEmail, false));
        }
        if (status == VehicleReservationStatus.Cancelled)
            db.VehicleBookingDays.RemoveRange(await db.VehicleBookingDays.Where(x => x.ReservationId == id).ToListAsync(ct));
        reservation.Status = status;
        reservation.DecidedAtUtc = Now;
        reservation.DecidedById = UserId;
        reservation.Version++;
        vehicle.Version++;
        Audit(vehicle, reservation, status.ToString(), "");
        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException exception) when (IsConflict(exception))
        { return Conflict("Der Zeitraum oder die Anfrage wurde inzwischen geändert. Bitte neu laden."); }
        return ServiceResult.Ok(ToDto(reservation));
    }

    private Task<ApplicationUser?> Actor(CancellationToken ct) => db.Users.SingleOrDefaultAsync(x => x.Id == UserId && x.IsActive, ct);

    private Task<ServiceResult> Execute(Func<Task<ServiceResult>> operation) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        // A retry must reload both permissions and aggregate state from the database.
        db.ChangeTracker.Clear();
        return await operation();
    });
    private Task<bool> HasOfficerRole(string id, CancellationToken ct) =>
        (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
         where membership.UserId == id && role.Name == Roles.EquipmentOfficer select membership).AnyAsync(ct);
    private Task<IDbContextTransaction?> Begin(CancellationToken ct) => db.Database.IsRelational() ? BeginRelational(ct) : Task.FromResult<IDbContextTransaction?>(null);
    private async Task<IDbContextTransaction?> BeginRelational(CancellationToken ct) => await db.Database.BeginTransactionAsync(ct);
    private async Task<Vehicle?> LockVehicle(Guid id, CancellationToken ct)
    {
        // Serialize all writes for this vehicle across API instances, including deactivation.
        if (db.Database.IsNpgsql())
            await db.Vehicles.FromSqlInterpolated($"SELECT * FROM \"Vehicles\" WHERE \"Id\" = {id} FOR UPDATE").LoadAsync(ct);
        return await Vehicles.SingleOrDefaultAsync(x => x.Id == id, ct);
    }
    private async Task<ServiceResult?> Collision(Guid id, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var dates = await db.VehicleBookingDays.Where(x => x.VehicleId == id && x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date).Select(x => x.Date).ToListAsync(ct);
        return dates.Count == 0 ? null : Conflict("Das Fahrzeug ist bereits gebucht am " + string.Join(", ", dates.Select(x => x.ToString("dd.MM.yyyy"))) + ".");
    }
    private void AddDays(VehicleReservation reservation)
    {
        for (var day = reservation.From; day <= reservation.To; day = day.AddDays(1))
            db.VehicleBookingDays.Add(new VehicleBookingDay { VehicleId = reservation.VehicleId, Date = day, ReservationId = reservation.Id });
    }
    private ServiceResult Replay(VehicleReservation existing, string hash) => existing.RequesterId == UserId && existing.PayloadHash == hash
        ? ServiceResult.Ok(ToDto(existing)) : Conflict("Die CommandId wurde bereits für eine andere Anfrage verwendet.");
    private void Audit(Vehicle vehicle, VehicleReservation? reservation, string action, string details) => db.VehicleEvents.Add(new VehicleEvent
    { Id = Guid.NewGuid(), VehicleId = vehicle.Id, ReservationId = reservation?.Id, ActorId = UserId, Action = action, Details = details, OccurredAtUtc = Now });
    private VehicleDto ToDto(Vehicle vehicle) => new(vehicle.Id, vehicle.FireDepartmentId, vehicle.FireDepartment.Name,
        vehicle.RadioCallSign, vehicle.Name, vehicle.LicensePlate, vehicle.EquipmentOfficerId, vehicle.EquipmentOfficer.FullName,
        vehicle.IsActive, vehicle.Version, IsAdmin || Owns(vehicle), Owns(vehicle), vehicle.VehicleTypeId, vehicle.VehicleType?.Name,
        vehicle.VehicleStatusId, vehicle.VehicleStatus.Name, vehicle.VehicleStatus.AllowsBooking, vehicle.StatusChangedAtUtc);
    private VehicleReservationDto ToDto(VehicleReservation reservation) => new(reservation.Id, reservation.VehicleId, reservation.RadioCallSign,
        reservation.FireDepartmentName, reservation.From, reservation.To, reservation.Comment, reservation.Status, reservation.RequesterName,
        reservation.CreatedAtUtc, reservation.DecidedAtUtc, reservation.IsDirectBooking, reservation.Version,
        Owns(reservation.Vehicle) && reservation.Status == VehicleReservationStatus.Pending,
        (Owns(reservation.Vehicle) && reservation.Status is VehicleReservationStatus.Pending or VehicleReservationStatus.Confirmed) ||
        (reservation.RequesterId == UserId && reservation.Status == VehicleReservationStatus.Pending));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsConflict(DbUpdateException exception) => exception is DbUpdateConcurrencyException ||
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
