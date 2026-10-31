using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services;

public enum InventoryTransferKind { Equipment, Types, Categories }

/// <summary>
/// Executes inventory Transfer Service.
/// </summary>
/// <param name="db">The db.</param>
/// <param name="clock">The clock.</param>
public sealed class InventoryTransferService(FeuerwehrDbContext db, TimeProvider clock) : IInventoryTransferService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static ServiceResult Invalid(IEnumerable<InventoryImportError> errors) => ServiceResult.BadRequest(new InventoryImportResponse(0, 0, errors.Take(100).ToList()));

    /// <summary>
    /// Executes export.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> Export(InventoryTransferKind kind, CancellationToken ct)
    {
        if (kind == InventoryTransferKind.Categories)
        {
            var categories = await db.InventoryCategories.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new InventoryCategoryDto(x.Id, x.Name)).ToListAsync(ct);
            return ServiceResult.File(JsonSerializer.SerializeToUtf8Bytes(categories, Json), "application/json", "inventarkategorien.json");
        }
        if (kind == InventoryTransferKind.Types)
        {
            var types = await db.EquipmentTypes.AsNoTracking().Include(x => x.Category).OrderBy(x => x.Name)
                .Select(x => new EquipmentTypeDto(x.Id, x.CategoryId, x.Category.Name, x.Name, x.InspectionIntervalMonths, x.InspectionInstructions, x.Version)).ToListAsync(ct);
            return ServiceResult.File(JsonSerializer.SerializeToUtf8Bytes(types, Json), "application/json", "geraetetypen.json");
        }
        var equipment = await db.Equipment.AsNoTracking().Include(x => x.EquipmentType).ThenInclude(x => x.Category).OrderBy(x => x.BarcodeNumber).ToListAsync(ct);
        var csv = new StringBuilder("\uFEFF" + string.Join(';', InventoryCsv.Columns) + "\r\n");
        foreach (var entity in equipment)
        {
            ct.ThrowIfCancellationRequested();
            var item = InventoryService.ToDto(entity);
            var status = item.Status switch { EquipmentStatus.Ready => "Einsatzklar", EquipmentStatus.Defective => "Defekt", _ => "In Reparatur" };
            csv.AppendJoin(';', new[] { item.Id.ToString(), item.Barcode, item.EquipmentTypeId.ToString(), item.EquipmentTypeName, item.CategoryName,
                item.SerialNumber, item.Manufacturer, status, item.IsActive ? "true" : "false", Date(item.CreatedOn),
                item.LastInspectedOn is { } last ? Date(last) : "", Date(item.NextInspectionOn) }.Select(InventoryCsv.Quote)).Append("\r\n");
        }
        return ServiceResult.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "inventar.csv");
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// Executes import.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="stream">The stream.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> Import(InventoryTransferKind kind, Stream stream, CancellationToken ct)
    {
        if (kind == InventoryTransferKind.Equipment)
        {
            var (rows, errors) = InventoryCsv.Read(stream, ct);
            if (errors.Count > 0) return Invalid(errors);
            return await InTransaction(kind, () => ImportEquipment(rows, ct), ct);
        }
        try
        {
            if (kind == InventoryTransferKind.Categories)
            {
                var rows = await JsonSerializer.DeserializeAsync<List<InventoryCategoryImportDto>>(stream, Json, ct);
                var errors = Validate(rows);
                return errors.Count > 0 ? Invalid(errors) : await InTransaction(kind, () => ImportCategories(rows!, ct), ct);
            }
            else
            {
                var rows = await JsonSerializer.DeserializeAsync<List<EquipmentTypeImportDto>>(stream, Json, ct);
                var errors = Validate(rows);
                return errors.Count > 0 ? Invalid(errors) : await InTransaction(kind, () => ImportTypes(rows!, ct), ct);
            }
        }
        catch (JsonException) { return Invalid([new(null, "Ungültige JSON-Datei. Erwartet wird eine Liste von Kategorien bzw. Gerätetypen mit gültigen UUIDs und Feldwerten.")]); }
    }

    private static List<InventoryImportError> Validate<T>(List<T>? rows) where T : class
    {
        if (rows is null || rows.Count == 0) return [new(null, "Die JSON-Datei muss eine nicht leere Liste enthalten.")];
        if (rows.Count > 10_000) return [new(null, "Pro Import sind höchstens 10.000 Einträge erlaubt.")];
        List<InventoryImportError> errors = [];
        for (var i = 0; i < rows.Count && errors.Count < 100; i++)
        {
            if (rows[i] is not { } row) { errors.Add(new(i + 1, "Der Eintrag darf nicht null sein.")); continue; }
            List<ValidationResult> validation = [];
            Validator.TryValidateObject(row, new ValidationContext(row), validation, validateAllProperties: true);
            errors.AddRange(validation.Select(x => new InventoryImportError(i + 1, x.ErrorMessage ?? "Ungültiger Eintrag.")));
        }
        return errors;
    }

    private async Task<ServiceResult> InTransaction(InventoryTransferKind kind, Func<Task<ServiceResult>> apply, CancellationToken ct)
    {
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
                if (db.Database.IsNpgsql())
                {
                    // Serialize imports with normal writes. In particular, no INSERT may allocate a barcode
                    // between reading the sequence and advancing it past explicitly imported numbers.
                    var sql = kind switch
                    {
                        InventoryTransferKind.Equipment => "LOCK TABLE \"Equipment\" IN SHARE ROW EXCLUSIVE MODE",
                        InventoryTransferKind.Types => "LOCK TABLE \"EquipmentTypes\" IN SHARE ROW EXCLUSIVE MODE",
                        _ => "LOCK TABLE \"InventoryCategories\" IN SHARE ROW EXCLUSIVE MODE",
                    };
                    await db.Database.ExecuteSqlRawAsync(sql, ct);
                }
                var result = await apply();
                if (result.Code >= 400) { db.ChangeTracker.Clear(); return result; }
                await db.SaveChangesAsync(ct);
                if (kind == InventoryTransferKind.Equipment && db.Database.IsNpgsql())
                {
                    // Sequences are not transactional; advancing can leave gaps on rollback, but never
                    // rewinds an already issued number and never makes a duplicate possible.
                    await db.Database.ExecuteSqlRawAsync("SELECT setval('\"InventoryBarcodeSequence\"', GREATEST((SELECT last_value FROM \"InventoryBarcodeSequence\"), (SELECT MAX(\"BarcodeNumber\") FROM \"Equipment\")), true)", ct);
                }
                if (transaction is not null) await transaction.CommitAsync(ct);
                return result;
            });
        }
        catch (DbUpdateConcurrencyException) { return ServiceResult.Problem(409, "Die Daten wurden gleichzeitig geändert. Bitte die Datei und den aktuellen Bestand prüfen und erneut importieren."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" })
        { return ServiceResult.Problem(409, "Ein Barcode, eine Kategorie oder eine Zuordnung wurde gleichzeitig geändert. Es wurden keine Importzeilen gespeichert."); }
    }

    private async Task<ServiceResult> ImportCategories(List<InventoryCategoryImportDto> rows, CancellationToken ct)
    {
        var existing = await db.InventoryCategories.ToListAsync(ct);
        var byIds = existing.ToDictionary(x => x.Id);
        var byNames = existing.ToDictionary(x => x.NormalizedName);
        var ids = new HashSet<Guid>(); var names = new HashSet<string>(); var targets = new HashSet<Guid>();
        List<InventoryImportError> errors = []; var created = 0; var updated = 0;
        for (var i = 0; i < rows.Count && errors.Count < 100; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = rows[i]; var name = row.Name.Trim(); var normalized = Normalize(name);
            if (name.Length == 0 || !names.Add(normalized) || row.Id is { } rowId && (rowId == Guid.Empty || !ids.Add(rowId)))
            { errors.Add(new(i + 1, "Kategoriename/ID ist leer, ungültig oder in der Datei mehrfach vorhanden.")); continue; }
            var byId = row.Id is { } id ? byIds.GetValueOrDefault(id) : null;
            var byName = byNames.GetValueOrDefault(normalized);
            if (byId is not null && byName is not null && byId.Id != byName.Id)
            { errors.Add(new(i + 1, "ID und Name verweisen auf unterschiedliche bestehende Kategorien.")); continue; }
            var category = byId ?? byName;
            if (category is null)
            {
                category = new InventoryCategory { Id = row.Id ?? Guid.NewGuid() }; db.InventoryCategories.Add(category); created++;
            }
            else updated++;
            if (!targets.Add(category.Id)) { errors.Add(new(i + 1, "Dieselbe Kategorie würde mehrfach ersetzt werden.")); continue; }
            category.Name = name; category.NormalizedName = normalized;
        }
        return errors.Count > 0 ? Invalid(errors) : ServiceResult.Ok(new InventoryImportResponse(created, updated, []));
    }

    private async Task<ServiceResult> ImportTypes(List<EquipmentTypeImportDto> rows, CancellationToken ct)
    {
        var categories = await db.InventoryCategories.ToListAsync(ct);
        var existing = await db.EquipmentTypes.ToListAsync(ct);
        var categoriesById = categories.ToDictionary(x => x.Id);
        var categoriesByName = categories.ToDictionary(x => x.NormalizedName);
        var typesById = existing.ToDictionary(x => x.Id);
        var typesByName = existing.GroupBy(x => (x.CategoryId, Name: Normalize(x.Name))).ToDictionary(x => x.Key, x => x.ToList());
        var ids = new HashSet<Guid>(); var keys = new HashSet<string>(); var targets = new HashSet<Guid>();
        List<InventoryImportError> errors = []; var created = 0; var updated = 0;
        for (var i = 0; i < rows.Count && errors.Count < 100; i++)
        {
            ct.ThrowIfCancellationRequested();
            var row = rows[i];
            var category = (row.CategoryId is { } categoryId ? categoriesById.GetValueOrDefault(categoryId) : null)
                ?? categoriesByName.GetValueOrDefault(Normalize(row.CategoryName ?? ""));
            if (category is null) { errors.Add(new(i + 1, "Die Kategorie existiert nicht. Bitte zuerst die Kategorien importieren.")); continue; }
            if (!string.IsNullOrWhiteSpace(row.CategoryName) && Normalize(row.CategoryName) != category.NormalizedName)
            { errors.Add(new(i + 1, "Kategorie-ID und Kategoriename passen nicht zusammen.")); continue; }
            var name = row.Name.Trim();
            if (name.Length == 0 || string.IsNullOrWhiteSpace(row.InspectionInstructions) ||
                row.Id is { } rowId && (rowId == Guid.Empty || !ids.Add(rowId)))
            { errors.Add(new(i + 1, "Typname/Prüfanweisung ist leer oder Gerätetyp/ID in der Datei mehrfach vorhanden.")); continue; }
            var type = row.Id is { } id ? typesById.GetValueOrDefault(id) : null;
            if (type is null)
            {
                if (!keys.Add($"{category.Id}/{Normalize(name)}")) { errors.Add(new(i + 1, "Der Gerätetyp ist in der Datei mehrfach vorhanden.")); continue; }
                var matches = typesByName.GetValueOrDefault((category.Id, Normalize(name))) ?? [];
                if (matches.Count > 1) { errors.Add(new(i + 1, "Der Gerätetyp ist nicht eindeutig. Bitte seine bestehende ID angeben.")); continue; }
                type = matches.SingleOrDefault();
            }
            if (type is null) { type = new EquipmentType { Id = row.Id ?? Guid.NewGuid() }; db.EquipmentTypes.Add(type); created++; }
            else { type.Version++; updated++; }
            if (!targets.Add(type.Id)) { errors.Add(new(i + 1, "Derselbe Gerätetyp würde mehrfach ersetzt werden.")); continue; }
            type.Category = category; type.CategoryId = category.Id; type.Name = name;
            type.InspectionIntervalMonths = row.InspectionIntervalMonths; type.InspectionInstructions = row.InspectionInstructions.Trim();
        }
        return errors.Count > 0 ? Invalid(errors) : ServiceResult.Ok(new InventoryImportResponse(created, updated, []));
    }

    private async Task<ServiceResult> ImportEquipment(List<InventoryCsvRow> rows, CancellationToken ct)
    {
        var existing = await db.Equipment.ToDictionaryAsync(x => x.BarcodeNumber, ct);
        var types = await db.EquipmentTypes.Include(x => x.Category).ToListAsync(ct);
        var typesById = types.ToDictionary(x => x.Id);
        var typesByName = types.GroupBy(x => Normalize(x.Name)).ToDictionary(x => x.Key, x => x.ToList());
        var typesByCategory = types.GroupBy(x => (Name: Normalize(x.Name), Category: x.Category.NormalizedName)).ToDictionary(x => x.Key, x => x.ToList());
        List<InventoryImportError> errors = []; var created = 0; var updated = 0;
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            if (errors.Count >= 100) break;
            var type = row.TypeId is { } typeId ? typesById.GetValueOrDefault(typeId) : null;
            if (type is not null && (row.TypeName.Length > 0 && Normalize(row.TypeName) != Normalize(type.Name) ||
                row.Category.Length > 0 && Normalize(row.Category) != type.Category.NormalizedName))
            { errors.Add(new(row.Line, "Gerätetyp-ID und Typname/Kategorie passen nicht zusammen.")); continue; }
            if (type is null)
            {
                var matches = (row.Category.Length == 0 ? typesByName.GetValueOrDefault(Normalize(row.TypeName))
                    : typesByCategory.GetValueOrDefault((Normalize(row.TypeName), Normalize(row.Category)))) ?? [];
                if (matches.Count != 1) { errors.Add(new(row.Line, "Gerätetyp nicht gefunden oder nicht eindeutig. Bitte vorhandene ID oder Typname und Kategorie angeben.")); continue; }
                type = matches[0];
            }
            if (existing.TryGetValue(row.BarcodeNumber, out var item))
            {
                // A replacement updates the master data in place; identity, inspection state and
                // historical evidence are retained, including the state of deactivated equipment.
                item.Version++; updated++;
            }
            else
            {
                var createdOn = row.CreatedOn ?? Today;
                if (createdOn > Today || row.LastInspectedOn > Today || row.LastInspectedOn < createdOn)
                { errors.Add(new(row.Line, "Bei neuen Geräten darf das Datum nicht in der Zukunft liegen; die letzte Prüfung darf nicht vor der Anlage liegen.")); continue; }
                item = new Equipment { Id = Guid.NewGuid(), BarcodeNumber = row.BarcodeNumber, CreatedOn = createdOn,
                    LastInspectedOn = row.LastInspectedOn, Status = row.Status, IsActive = row.IsActive };
                db.Equipment.Add(item); created++;
            }
            item.EquipmentType = type; item.EquipmentTypeId = type.Id;
            item.SerialNumber = row.SerialNumber; item.Manufacturer = row.Manufacturer;
        }
        return errors.Count > 0 ? Invalid(errors) : ServiceResult.Ok(new InventoryImportResponse(created, updated, []));
    }
}
