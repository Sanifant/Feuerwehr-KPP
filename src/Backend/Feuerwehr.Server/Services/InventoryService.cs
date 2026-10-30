using System.Globalization;
using System.Text;
using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace De.Openelp.Feuerwehr.Server.Services;

public interface IInventoryService
{
    Task<ServiceResult> GetCategories(CancellationToken ct);
    Task<ServiceResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct);
    Task<ServiceResult> GetTypes(CancellationToken ct);
    Task<ServiceResult> SaveType(Guid? id, SaveEquipmentTypeRequest request, CancellationToken ct);
    Task<ServiceResult> GetAll(bool includeInactive, CancellationToken ct);
    Task<ServiceResult> Get(Guid id, CancellationToken ct);
    Task<ServiceResult> FindBarcode(string barcode, CancellationToken ct);
    Task<ServiceResult> BarcodeLabel(Guid id, CancellationToken ct);
    Task<ServiceResult> Save(Guid? id, SaveEquipmentRequest request, CancellationToken ct);
    Task<ServiceResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct);
    Task<ServiceResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct);
}

/// <summary>
/// Executes inventory Service.
/// </summary>
/// <param name="db">The db.</param>
/// <param name="user">The user.</param>
/// <param name="clock">The clock.</param>
public sealed class InventoryService(FeuerwehrDbContext db, IUserContextAccessor user, TimeProvider clock) : IInventoryService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
    private IQueryable<Equipment> EquipmentQuery => db.Equipment.Include(x => x.EquipmentType).ThenInclude(x => x.Category);
    private static ServiceResult Conflict(string title) => ServiceResult.Problem(409, title);
    private static ServiceResult Invalid(string title) => ServiceResult.Problem(400, title);

    /// <summary>
    /// Gets the categories.
    /// </summary>
    public async Task<ServiceResult> GetCategories(CancellationToken ct) => ServiceResult.Ok(await db.InventoryCategories
        .OrderBy(x => x.Name).Select(x => new InventoryCategoryDto(x.Id, x.Name)).ToListAsync(ct));

    /// <summary>
    /// Creates the category.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (name.Length is 0 or > 100) return Invalid("Ein Kategoriename ist erforderlich (maximal 100 Zeichen).");
        var normalized = name.ToUpperInvariant();
        if (await db.InventoryCategories.AnyAsync(x => x.NormalizedName == normalized, ct)) return Conflict("Diese Kategorie existiert bereits.");
        var category = new InventoryCategory { Id = Guid.NewGuid(), Name = name, NormalizedName = normalized };
        db.InventoryCategories.Add(category);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict("Diese Kategorie existiert bereits."); }
        return ServiceResult.StatusCode(201, new InventoryCategoryDto(category.Id, category.Name));
    }

    /// <summary>
    /// Gets the types.
    /// </summary>
    public async Task<ServiceResult> GetTypes(CancellationToken ct) => ServiceResult.Ok(await db.EquipmentTypes
        .Include(x => x.Category).OrderBy(x => x.Name)
        .Select(x => new EquipmentTypeDto(x.Id, x.CategoryId, x.Category.Name, x.Name, x.InspectionIntervalMonths, x.InspectionInstructions, x.Version)).ToListAsync(ct));

    /// <summary>
    /// Saves the type.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> SaveType(Guid? id, SaveEquipmentTypeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 150 ||
            string.IsNullOrWhiteSpace(request.InspectionInstructions) || request.InspectionInstructions.Length > 8000 ||
            request.InspectionIntervalMonths is < 1 or > 120) return Invalid("GerÃ¤tetyp, PrÃ¼fanweisung und ein Intervall von 1 bis 120 Monaten sind erforderlich.");
        var category = await db.InventoryCategories.FindAsync([request.CategoryId], ct);
        if (category is null) return Invalid("Die Kategorie existiert nicht.");
        var type = id.HasValue ? await db.EquipmentTypes.FindAsync([id.Value], ct) : new EquipmentType { Id = Guid.NewGuid() };
        if (type is null) return ServiceResult.NotFound();
        if (id.HasValue && type.Version != request.ExpectedVersion) return Conflict("Der GerÃ¤tetyp wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden.");
        if (id.HasValue) type.Version++;
        else db.EquipmentTypes.Add(type);
        type.Category = category;
        type.CategoryId = category.Id;
        type.Name = request.Name.Trim();
        type.InspectionIntervalMonths = request.InspectionIntervalMonths;
        type.InspectionInstructions = request.InspectionInstructions.Trim();
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict("Der GerÃ¤tetyp wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden."); }
        return ServiceResult.StatusCode(id.HasValue ? 200 : 201, new EquipmentTypeDto(type.Id, category.Id, category.Name,
            type.Name, type.InspectionIntervalMonths, type.InspectionInstructions, type.Version));
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <param name="includeInactive">The include Inactive.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> GetAll(bool includeInactive, CancellationToken ct)
    {
        var items = await EquipmentQuery.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.BarcodeNumber).ToListAsync(ct);
        return ServiceResult.Ok(items.Select(ToDto).ToList());
    }

    /// <summary>
    /// Gets the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> Get(Guid id, CancellationToken ct)
    {
        var item = await EquipmentQuery.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult.NotFound();
        var history = await db.EquipmentInspections.AsNoTracking().Where(x => x.EquipmentId == id)
            .OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new EquipmentInspectionDto(x.Id, x.InspectedOn, x.RecordedAtUtc, x.InspectorName, x.Comment,
                x.Status, x.EquipmentTypeName, x.InspectionIntervalMonths, x.InspectionInstructions)).ToListAsync(ct);
        return ServiceResult.Ok(new EquipmentDetailDto(ToDto(item), history));
    }

    /// <summary>
    /// Executes find Barcode.
    /// </summary>
    /// <param name="barcode">The barcode.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> FindBarcode(string barcode, CancellationToken ct)
    {
        barcode = barcode.Trim().ToUpperInvariant();
        if (barcode.Length != 7 || !barcode.StartsWith("KPP-", StringComparison.Ordinal) ||
            !int.TryParse(barcode.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number is < 1 or > 999)
            return Invalid("Der Barcode muss dem Muster KPP-001 bis KPP-999 entsprechen.");
        var item = await db.Equipment.AsNoTracking().FirstOrDefaultAsync(x => x.BarcodeNumber == number, ct);
        return item is null ? ServiceResult.NotFound() : await Get(item.Id, ct);
    }

    /// <summary>
    /// Executes barcode Label.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> BarcodeLabel(Guid id, CancellationToken ct)
    {
        var item = await db.Equipment.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult.NotFound();
        var barcode = $"KPP-{item.BarcodeNumber:D3}";
        var writer = new ZXing.BarcodeWriterSvg
        {
            Format = ZXing.BarcodeFormat.CODE_128,
            Options = new ZXing.Common.EncodingOptions { Width = 320, Height = 110, Margin = 12 },
        };
        return ServiceResult.File(Encoding.UTF8.GetBytes(writer.Write(barcode).Content), "image/svg+xml", $"{barcode}.svg");
    }

    /// <summary>
    /// Saves the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> Save(Guid? id, SaveEquipmentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber) || request.SerialNumber.Length > 150 ||
            string.IsNullOrWhiteSpace(request.Manufacturer) || request.Manufacturer.Length > 150 || !Enum.IsDefined(request.Status))
            return Invalid("Hersteller, Seriennummer und ein gÃ¼ltiger GerÃ¤testatus sind erforderlich.");
        var type = await db.EquipmentTypes.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == request.EquipmentTypeId, ct);
        if (type is null) return Invalid("Der GerÃ¤tetyp existiert nicht.");
        var item = id.HasValue ? await db.Equipment.FindAsync([id.Value], ct) : new Equipment { Id = Guid.NewGuid(), CreatedOn = Today };
        if (item is null) return ServiceResult.NotFound();
        if (id.HasValue && item.Version != request.ExpectedVersion) return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden.");
        if (id.HasValue && !item.IsActive) return Conflict("Deaktivierte GerÃ¤te kÃ¶nnen nicht bearbeitet werden.");
        // StatusÃ¤nderungen bestehender GerÃ¤te werden ausschlieÃŸlich als PrÃ¼fung protokolliert.
        if (id.HasValue && request.Status != item.Status) return Invalid("Den GerÃ¤testatus bitte Ã¼ber eine PrÃ¼fung Ã¤ndern.");
        if (id.HasValue) item.Version++;
        else db.Equipment.Add(item);
        item.EquipmentType = type;
        item.EquipmentTypeId = type.Id;
        item.SerialNumber = request.SerialNumber.Trim();
        item.Manufacturer = request.Manufacturer.Trim();
        item.Status = request.Status;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "2200H" })
        { return Conflict("Der Barcodevorrat KPP-001 bis KPP-999 ist ausgeschÃ¶pft. Es kÃ¶nnen keine weiteren GerÃ¤te angelegt werden."); }
        return ServiceResult.StatusCode(id.HasValue ? 200 : 201, ToDto(item));
    }

    /// <summary>
    /// Sets the active.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct)
    {
        var item = await db.Equipment.FindAsync([id], ct);
        if (item is null) return ServiceResult.NotFound();
        if (item.Version != request.ExpectedVersion) return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden.");
        item.IsActive = request.IsActive;
        item.Version++;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden."); }
        return await Get(id, ct);
    }

    /// <summary>
    /// Executes inspect.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<ServiceResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct)
    {
        if (request.CommandId == Guid.Empty || !Enum.IsDefined(request.Status) || request.Comment?.Length > 4000)
            return Invalid("PrÃ¼fauftrag und Status sind erforderlich; der Kommentar darf maximal 4000 Zeichen enthalten.");
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        var previous = await db.EquipmentInspections.AsNoTracking().FirstOrDefaultAsync(x => x.CommandId == request.CommandId, ct);
        if (previous is not null)
        {
            if (previous.EquipmentId != id || previous.InspectorId != user.UserId || previous.InspectedOn != request.InspectedOn ||
                previous.Status != request.Status || previous.Comment != comment) return Conflict("Dieser PrÃ¼fauftrag wurde bereits mit anderen Angaben verwendet.");
            return await Get(id, ct);
        }
        var item = await EquipmentQuery.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return ServiceResult.NotFound();
        if (!item.IsActive) return Conflict("Deaktivierte GerÃ¤te kÃ¶nnen nicht geprÃ¼ft werden.");
        if (item.Version != request.ExpectedVersion) return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden.");
        if (request.InspectedOn > Today || request.InspectedOn < (item.LastInspectedOn ?? item.CreatedOn))
            return Invalid("Das PrÃ¼fdatum darf weder in der Zukunft noch vor der Anlage bzw. letzten PrÃ¼fung liegen.");
        item.LastInspectedOn = request.InspectedOn;
        item.Status = request.Status;
        item.Version++;
        db.EquipmentInspections.Add(new EquipmentInspection
        {
            Id = Guid.NewGuid(), EquipmentId = id, CommandId = request.CommandId,
            InspectedOn = request.InspectedOn, RecordedAtUtc = clock.GetUtcNow().UtcDateTime,
            InspectorId = user.UserId, InspectorName = user.DisplayName, Comment = comment, Status = request.Status,
            EquipmentTypeName = item.EquipmentType.Name, InspectionIntervalMonths = item.EquipmentType.InspectionIntervalMonths,
            InspectionInstructions = item.EquipmentType.InspectionInstructions,
        });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict("Das GerÃ¤t wurde zwischenzeitlich geÃ¤ndert. Bitte neu laden."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { return Conflict("Der PrÃ¼fauftrag wird bereits verarbeitet. Bitte das GerÃ¤t neu laden."); }
        return await Get(id, ct);
    }

    internal static EquipmentDto ToDto(Equipment item) => new(item.Id, $"KPP-{item.BarcodeNumber:D3}", item.EquipmentTypeId,
        item.EquipmentType.Name, item.EquipmentType.Category.Name, item.SerialNumber, item.Manufacturer, item.Status, item.IsActive,
        item.CreatedOn, item.LastInspectedOn, (item.LastInspectedOn ?? item.CreatedOn).AddMonths(item.EquipmentType.InspectionIntervalMonths),
        item.EquipmentType.InspectionIntervalMonths, item.EquipmentType.InspectionInstructions, item.Version);
}

