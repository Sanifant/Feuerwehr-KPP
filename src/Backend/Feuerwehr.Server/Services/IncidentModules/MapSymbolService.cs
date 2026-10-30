using System.Xml;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules;

public interface IMapSymbolService
{
    Task<ServiceResult> GetSymbols(CancellationToken cancellationToken);
    Task<ServiceResult> GetCategories(CancellationToken cancellationToken);
    Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Executes map Symbol Service.
/// </summary>
/// <param name="dbContext">The db Context.</param>
/// <param name="user">The user.</param>
/// <param name="logger">The logger.</param>
public sealed class MapSymbolService(FeuerwehrDbContext dbContext, IUserContextAccessor user, ILogger<MapSymbolService> logger) : IMapSymbolService
{
    /// <summary>
    /// Gets the categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> GetCategories(CancellationToken cancellationToken)
        => ServiceResult.Ok(await dbContext.MapSymbolCategories.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(cancellationToken));

    /// <summary>
    /// Creates the category.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin) return ServiceResult.Forbid();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 40)
            return ServiceResult.Problem(400, "Bitte einen Kategorienamen mit hÃ¶chstens 40 Zeichen angeben.");
        var normalizedName = name.ToUpperInvariant();
        if (await dbContext.MapSymbolCategories.AnyAsync(x => x.Name.ToUpper() == normalizedName, cancellationToken))
            return ServiceResult.Problem(409, "Diese Symbolkategorie existiert bereits.");
        dbContext.MapSymbolCategories.Add(new MapSymbolCategory { Name = name });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ServiceResult.StatusCode(201, name);
    }

    /// <summary>
    /// Gets the symbols.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> GetSymbols(CancellationToken cancellationToken)
    {
        logger.LogDebug("Map symbol catalog load started. Admin: {IsAdmin}", user.IsAdmin);
        try
        {
            var items = await dbContext.MapSymbols.AsNoTracking().Where(x => x.IsActive || user.IsAdmin)
                .OrderBy(x => x.Category).ThenBy(x => x.Name)
                .Select(x => new MapSymbolDto(x.Id, x.Name, x.Category, x.SvgContent, x.IsActive, x.IsVehicle)).ToListAsync(cancellationToken);
            logger.LogDebug("Map symbol catalog loaded. Symbols: {Count}", items.Count);
            return ServiceResult.Ok(items);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Map symbol catalog could not be loaded.");
            throw;
        }
    }

    /// <summary>
    /// Saves the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin)
        {
            logger.LogWarning("Map symbol save rejected: administrator permission missing.");
            return ServiceResult.Forbid();
        }
        var category = request.Category ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200
            || !await dbContext.MapSymbolCategories.AnyAsync(x => x.Name == category, cancellationToken))
        {
            logger.LogWarning("Map symbol save rejected: invalid name or category. SymbolId: {SymbolId}", id);
            return ServiceResult.Problem(400, "Bitte Name und eine gÃ¼ltige Symbolkategorie angeben.");
        }
        var content = request.SvgContent ?? string.Empty;
        var categoryForLog = category.Replace("\r", string.Empty).Replace("\n", string.Empty);
        logger.LogInformation("Map symbol save started. SymbolId: {SymbolId}, Category: {Category}, SvgCharacters: {Length}", id, categoryForLog, content.Length);
        string svg;
        try { svg = SvgSymbolValidator.Validate(content); }
        catch (Exception error) when (error is XmlException or ArgumentException)
        {
            logger.LogWarning("Map symbol SVG validation rejected. SymbolId: {SymbolId}, ErrorType: {ErrorType}", id, error.GetType().Name);
            return ServiceResult.Problem(400, error is XmlException ? "Die Datei enthÃ¤lt kein gÃ¼ltiges SVG." : error.Message);
        }
        logger.LogDebug("Map symbol SVG validation completed. SymbolId: {SymbolId}", id);

        logger.LogDebug("Map symbol database lookup started. SymbolId: {SymbolId}", id);
        var symbol = id.HasValue ? await dbContext.MapSymbols.FindAsync([id.Value], cancellationToken) : null;
        if (id.HasValue && symbol is null)
        {
            logger.LogWarning("Map symbol save rejected: symbol was not found. SymbolId: {SymbolId}", id);
            return ServiceResult.NotFound();
        }
        var created = symbol is null;
        symbol ??= new MapSymbol { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
        symbol.Name = request.Name.Trim();
        symbol.Category = category;
        symbol.SvgContent = svg;
        symbol.IsActive = request.IsActive;
        symbol.IsVehicle = request.IsVehicle;
        symbol.UpdatedAtUtc = DateTime.UtcNow;
        if (created) dbContext.MapSymbols.Add(symbol);
        logger.LogDebug("Map symbol database save started. SymbolId: {SymbolId}", symbol.Id);
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Map symbol database save failed. SymbolId: {SymbolId}", symbol.Id);
            throw;
        }
        logger.LogInformation("Map symbol save completed. SymbolId: {SymbolId}, Created: {Created}", symbol.Id, created);
        return ServiceResult.StatusCode(created ? 201 : 200, new MapSymbolDto(symbol.Id, symbol.Name, symbol.Category, symbol.SvgContent, symbol.IsActive, symbol.IsVehicle));
    }
}

