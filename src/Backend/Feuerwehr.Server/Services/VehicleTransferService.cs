using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services
{
    public enum VehicleTransferKind { Vehicles, Types }

    /// <summary>
    /// Executes vehicle Transfer Service.
    /// </summary>
    /// <param name="db">The db.</param>
    /// <param name="http">The http.</param>
    /// <param name="normalizer">The normalizer.</param>
    /// <param name="clock">The clock.</param>
    public sealed class VehicleTransferService(FeuerwehrDbContext db, IHttpContextAccessor http, ILookupNormalizer normalizer,
        TimeProvider clock) : IVehicleTransferService
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        private ClaimsPrincipal Principal => http.HttpContext?.User ?? new ClaimsPrincipal();
        private string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private bool IsAdmin => Principal.IsInRole(Roles.Admin);
        private bool Allowed(VehicleTransferKind kind) => IsAdmin || kind == VehicleTransferKind.Vehicles && Principal.IsInRole(Roles.EquipmentOfficer);
        private static string Normalize(string text) => text.Trim().ToUpperInvariant();
        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        private static ServiceResult Invalid(IEnumerable<VehicleImportError> errors, int code = 400) =>
            ServiceResult.StatusCode(code, new VehicleImportResponse(0, 0, errors.Take(100).ToList()));

        /// <summary>
        /// Executes export.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <param name="ct">The ct.</param>
        public async Task<ServiceResult> Export(VehicleTransferKind kind, CancellationToken ct)
        {
            if (!Allowed(kind)) return ServiceResult.Forbid();
            if (kind == VehicleTransferKind.Types)
            {
                List<VehicleTypeImportDto> types = await db.Set<VehicleType>().AsNoTracking().OrderBy(x => x.Name)
                    .Select(x => new VehicleTypeImportDto { Id = x.Id, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
                return ServiceResult.File(JsonSerializer.SerializeToUtf8Bytes(types, Json), "application/json", "fahrzeugtypen.json");
            }
            IQueryable<Vehicle> query = db.Vehicles.AsNoTracking().Include(x => x.FireDepartment).Include(x => x.EquipmentOfficer)
                .Include(x => x.VehicleType).Include(x => x.VehicleStatus).AsQueryable();
            if (!IsAdmin) query = query.Where(x => x.EquipmentOfficerId == UserId);
            List<Vehicle> vehicles = await query.OrderBy(x => x.FireDepartment.Name).ThenBy(x => x.RadioCallSign).ToListAsync(ct);
            StringBuilder csv = new StringBuilder("\uFEFF" + string.Join(';', VehicleCsv.Columns) + "\r\n");
            foreach (Vehicle? vehicle in vehicles)
            {
                csv.AppendJoin(';', new[] { vehicle.Id.ToString(), vehicle.RadioCallSign, vehicle.Name, vehicle.LicensePlate,
                    vehicle.FireDepartment.Name, vehicle.FireDepartmentId.ToString(CultureInfo.InvariantCulture), vehicle.VehicleType?.Name,
                    vehicle.VehicleStatus.Name, vehicle.EquipmentOfficer.Email, vehicle.IsActive ? "true" : "false" }.Select(VehicleCsv.Quote)).Append("\r\n");
            }
            return ServiceResult.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "fahrzeuge.csv");
        }

        /// <summary>
        /// Executes import.
        /// </summary>
        /// <param name="kind">The kind.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="ct">The ct.</param>
        public async Task<ServiceResult> Import(VehicleTransferKind kind, Stream stream, CancellationToken ct)
        {
            if (!Allowed(kind)) return ServiceResult.Forbid();
            if (kind == VehicleTransferKind.Vehicles)
            {
                (List<VehicleCsvRow>? rows, List<VehicleImportError>? errors) = VehicleCsv.Read(stream, ct);
                foreach (VehicleCsvRow row in rows) errors.AddRange(Validate(row.Data, row.Line));
                if (errors.Count > 0) return Invalid(errors);
                return await InTransaction(kind, () => ImportVehicles(rows, ct), ct);
            }
            try
            {
                List<VehicleTypeImportDto>? rows = await JsonSerializer.DeserializeAsync<List<VehicleTypeImportDto>>(stream, Json, ct);
                if (rows is null || rows.Count is 0 or > 10_000) return Invalid([new(null, "Die JSON-Datei muss 1 bis 10.000 Fahrzeugtypen enthalten.")]);
                List<VehicleImportError> errors = rows.SelectMany((row, index) => Validate(row, index + 1)).Take(100).ToList();
                return errors.Count > 0 ? Invalid(errors) : await InTransaction(kind, () => ImportTypes(rows, ct), ct);
            }
            catch (JsonException) { return Invalid([new(null, "Ungültige JSON-Datei. Erwartet wird eine Liste von Fahrzeugtypen mit gültigen Feldwerten.")]); }
        }

        private static IEnumerable<VehicleImportError> Validate(object? row, long line)
        {
            if (row is null) return [new(line, "Der Eintrag darf nicht null sein.")];
            List<ValidationResult> errors = [];
            Validator.TryValidateObject(row, new ValidationContext(row), errors, true);
            return errors.Select(error => new VehicleImportError(line, error.ErrorMessage ?? "Ungültiger Eintrag."));
        }

        private async Task<ServiceResult> InTransaction(VehicleTransferKind kind, Func<Task<ServiceResult>> apply, CancellationToken ct)
        {
            try
            {
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear();
                    await using IDbContextTransaction? transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
                    if (db.Database.IsNpgsql())
                        await db.Database.ExecuteSqlRawAsync(kind == VehicleTransferKind.Vehicles
                            ? "LOCK TABLE \"Vehicles\" IN EXCLUSIVE MODE" : "LOCK TABLE \"VehicleTypes\" IN EXCLUSIVE MODE", ct);
                    ServiceResult result = await apply();
                    if (result.Code >= 400) { db.ChangeTracker.Clear(); return result; }
                    await db.SaveChangesAsync(ct);
                    if (transaction is not null) await transaction.CommitAsync(ct);
                    return result;
                });
            }
            catch (DbUpdateConcurrencyException) { return Invalid([new(null, "Die Daten wurden gleichzeitig geändert. Es wurde kein Eintrag importiert.")], 409); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" or "23503" })
            { return Invalid([new(null, "Eine Zuordnung oder ein Name wurde gleichzeitig geändert. Es wurde kein Eintrag importiert.")], 409); }
        }

        private async Task<ServiceResult> ImportTypes(List<VehicleTypeImportDto> rows, CancellationToken ct)
        {
            List<VehicleType> existing = await db.Set<VehicleType>().ToListAsync(ct);
            Dictionary<Guid, VehicleType> byId = existing.ToDictionary(x => x.Id);
            Dictionary<string, VehicleType> byName = existing.ToDictionary(x => x.NormalizedName);
            HashSet<Guid> ids = new HashSet<Guid>();
            HashSet<string> names = new HashSet<string>();
            HashSet<Guid> targets = new HashSet<Guid>();
            List<VehicleImportError> errors = [];
            int created = 0;
            int updated = 0;
            for (int index = 0; index < rows.Count && errors.Count < 100; index++)
            {
                VehicleTypeImportDto row = rows[index];
                string name = row.Name.Trim();
                string normalized = Normalize(name);
                if (name.Length == 0 || !names.Add(normalized) || row.Id is { } rowId && (rowId == Guid.Empty || !ids.Add(rowId)))
                { errors.Add(new(index + 1, "Typname/ID ist leer, ungültig oder in der Datei mehrfach vorhanden.")); continue; }
                VehicleType? idMatch = row.Id is { } id ? byId.GetValueOrDefault(id) : null;
                VehicleType? nameMatch = byName.GetValueOrDefault(normalized);
                if (idMatch is not null && nameMatch is not null && idMatch.Id != nameMatch.Id)
                { errors.Add(new(index + 1, "ID und Name verweisen auf unterschiedliche bestehende Fahrzeugtypen.")); continue; }
                VehicleType? type = idMatch ?? nameMatch;
                if (type is null) { type = new VehicleType { Id = row.Id ?? Guid.NewGuid() }; db.Set<VehicleType>().Add(type); created++; }
                else { type.Version++; updated++; }
                if (!targets.Add(type.Id)) { errors.Add(new(index + 1, "Derselbe Fahrzeugtyp würde mehrfach ersetzt werden.")); continue; }
                type.Name = name; type.NormalizedName = normalized; type.IsActive = row.IsActive;
            }
            return errors.Count > 0 ? Invalid(errors) : ServiceResult.Ok(new VehicleImportResponse(created, updated, []));
        }

        private async Task<ServiceResult> ImportVehicles(List<VehicleCsvRow> rows, CancellationToken ct)
        {
            ApplicationUser? actor = await db.Users.SingleOrDefaultAsync(x => x.Id == UserId && x.IsActive, ct);
            if (actor is null) return ServiceResult.Forbid();
            ILookup<string, FireDepartment> departments = (await db.FireDepartments.ToListAsync(ct)).ToLookup(x => Normalize(x.Name));
            List<ApplicationUser> officers = await (from user in db.Users
                                  join membership in db.UserRoles on user.Id equals membership.UserId
                                  join role in db.Roles on membership.RoleId equals role.Id
                                  where user.IsActive && user.Email != null && role.Name == Roles.EquipmentOfficer select user).ToListAsync(ct);
            ILookup<string, ApplicationUser> byEmail = officers.DistinctBy(x => x.Id).ToLookup(x => normalizer.NormalizeEmail(x.Email!.Trim()) ?? "");
            Dictionary<string, VehicleType> types = (await db.Set<VehicleType>().ToListAsync(ct)).ToDictionary(x => x.NormalizedName);
            Dictionary<string, VehicleStatus> statuses = (await db.Set<VehicleStatus>().ToListAsync(ct)).ToDictionary(x => x.NormalizedName);
            List<Vehicle> existing = await db.Vehicles.ToListAsync(ct);
            Dictionary<Guid, Vehicle> byId = existing.ToDictionary(x => x.Id);
            ILookup<(int FireDepartmentId, string Name), Vehicle> byKey = existing.ToLookup(x => (x.FireDepartmentId, Name: Normalize(x.RadioCallSign)));
            HashSet<Guid> ids = new HashSet<Guid>();
            HashSet<(int, string)> keys = new HashSet<(int, string)>();
            HashSet<Guid> targets = new HashSet<Guid>();
            List<VehicleImportError> errors = [];
            int created = 0;
            int updated = 0;
            foreach (VehicleCsvRow entry in rows)
            {
                ct.ThrowIfCancellationRequested();
                if (errors.Count >= 100) break;
                VehicleImportDto row = entry.Data;
                List<FireDepartment> candidates = departments[Normalize(row.FireDepartment)].ToList();
                FireDepartment? department = candidates.Count == 1 ? candidates[0] : candidates.SingleOrDefault(x => x.Id == row.FireDepartmentId);
                if (department is null) { errors.Add(new(entry.Line, "Feuerwehr nicht gefunden oder nicht eindeutig. Bei gleichen Namen bitte fireDepartmentId angeben.")); continue; }
                List<ApplicationUser> matches = byEmail[normalizer.NormalizeEmail(row.EquipmentOfficerEmail.Trim()) ?? ""].ToList();
                if (matches.Count != 1 || matches[0].FireDepartmentId != department.Id)
                { errors.Add(new(entry.Line, "Die Gerätewart-E-Mail muss genau einem aktiven Gerätewart dieser Feuerwehr zugeordnet sein.")); continue; }
                ApplicationUser officer = matches[0];
                (int Id, string) key = (department.Id, Normalize(row.RadioCallSign));
                if (!keys.Add(key) || row.Id is { } rowId && !ids.Add(rowId))
                { errors.Add(new(entry.Line, "Funkrufname/Feuerwehr oder ID ist in der Datei mehrfach vorhanden.")); continue; }
                Vehicle? vehicle = row.Id is { } id ? byId.GetValueOrDefault(id) : null;
                List<Vehicle> natural = byKey[key].ToList();
                if (vehicle is null && natural.Count > 1)
                { errors.Add(new(entry.Line, "Das Fahrzeug ist nicht eindeutig. Bitte seine bestehende ID angeben.")); continue; }
                if (vehicle is not null && natural.Any(x => x.Id != vehicle.Id))
                { errors.Add(new(entry.Line, "ID und Funkrufname/Feuerwehr verweisen auf unterschiedliche Fahrzeuge.")); continue; }
                vehicle ??= natural.SingleOrDefault();
                if (!IsAdmin && (department.Id != actor.FireDepartmentId || vehicle is not null && vehicle.EquipmentOfficerId != UserId))
                    return Invalid([new(entry.Line, "Keine Berechtigung für dieses Fahrzeug oder diese Feuerwehr.")], 403);
                bool isNew = vehicle is null;
                vehicle ??= new Vehicle { Id = row.Id ?? Guid.NewGuid() };
                if (!targets.Add(vehicle.Id)) { errors.Add(new(entry.Line, "Dasselbe Fahrzeug würde mehrfach ersetzt werden.")); continue; }
                VehicleType? type = row.VehicleType is null ? vehicle.VehicleTypeId is { } currentType ? types.Values.SingleOrDefault(x => x.Id == currentType) : null
                    : Clean(row.VehicleType) is { } typeName ? types.GetValueOrDefault(Normalize(typeName)) : null;
                if (!string.IsNullOrWhiteSpace(row.VehicleType) && type is null || type is not null && !type.IsActive && (isNew || vehicle.VehicleTypeId != type.Id))
                { errors.Add(new(entry.Line, "Fahrzeugtyp nicht gefunden oder für neue Zuordnungen deaktiviert. Bitte zuerst die Fahrzeugtypen importieren.")); continue; }
                VehicleStatus? status = !string.IsNullOrWhiteSpace(row.Status) ? statuses.GetValueOrDefault(Normalize(row.Status))
                    : statuses.Values.SingleOrDefault(x => x.Id == vehicle.VehicleStatusId);
                if (status is null || !status.IsActive && (isNew || vehicle.VehicleStatusId != status.Id))
                { errors.Add(new(entry.Line, "Status nicht gefunden oder für neue Zuordnungen deaktiviert. Bitte zuerst den Status anlegen.")); continue; }
                vehicle.FireDepartmentId = department.Id; vehicle.EquipmentOfficerId = officer.Id;
                vehicle.RadioCallSign = row.RadioCallSign.Trim();
                if (row.Name is not null) vehicle.Name = Clean(row.Name);
                if (row.LicensePlate is not null) vehicle.LicensePlate = Clean(row.LicensePlate);
                vehicle.VehicleTypeId = type?.Id;
                if (isNew || vehicle.VehicleStatusId != status.Id)
                    vehicle.StatusChangedAtUtc = clock.GetUtcNow().UtcDateTime;
                vehicle.VehicleStatusId = status.Id;
                vehicle.IsActive = row.IsActive ?? vehicle.IsActive;
                if (isNew) { db.Vehicles.Add(vehicle); created++; } else { vehicle.Version++; updated++; }
                db.VehicleEvents.Add(new VehicleEvent { Id = Guid.NewGuid(), VehicleId = vehicle.Id, ActorId = UserId,
                    Action = isNew ? "VehicleImported" : "VehicleImportUpdated", Details = "Fahrzeugstammdaten per CSV importiert.", OccurredAtUtc = clock.GetUtcNow().UtcDateTime });
            }
            return errors.Count > 0 ? Invalid(errors) : ServiceResult.Ok(new VehicleImportResponse(created, updated, []));
        }
    }
}
