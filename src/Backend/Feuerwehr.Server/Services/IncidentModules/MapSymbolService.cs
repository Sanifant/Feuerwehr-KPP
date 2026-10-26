using System.Xml;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.Server.Services.IncidentModules;

public interface IMapSymbolService
{
    Task<ServiceResult> GetSymbols(CancellationToken cancellationToken);
    ServiceResult GetCategories();
    Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken);
}

public sealed class MapSymbolService(FeuerwehrDbContext dbContext, IUserContextAccessor user, ILogger<MapSymbolService> logger) : IMapSymbolService
{
    public ServiceResult GetCategories() => ServiceResult.Ok(MapSymbolCategories.All);

    public async Task<ServiceResult> GetSymbols(CancellationToken cancellationToken)
    {
        logger.LogDebug("Map symbol catalog load started. Admin: {IsAdmin}", user.IsAdmin);
        try
        {
            var items = await dbContext.MapSymbols.AsNoTracking().Where(x => x.IsActive || user.IsAdmin)
                .OrderBy(x => x.Category).ThenBy(x => x.Name)
                .Select(x => new MapSymbolDto(x.Id, x.Name, x.Category, x.SvgContent, x.IsActive)).ToListAsync(cancellationToken);
            logger.LogDebug("Map symbol catalog loaded. Symbols: {Count}", items.Count);
            return ServiceResult.Ok(items);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Map symbol catalog could not be loaded.");
            throw;
        }
    }

    public async Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
    {
        if (!user.IsAdmin)
        {
            logger.LogWarning("Map symbol save rejected: administrator permission missing.");
            return ServiceResult.Forbid();
        }
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 || !MapSymbolCategories.All.Contains(request.Category))
        {
            logger.LogWarning("Map symbol save rejected: invalid name or category. SymbolId: {SymbolId}", id);
            return ServiceResult.Problem(400, "Bitte Name und eine gültige Symbolkategorie angeben.");
        }
        var content = request.SvgContent ?? string.Empty;
        var categoryForLog = (request.Category ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        logger.LogInformation("Map symbol save started. SymbolId: {SymbolId}, Category: {Category}, SvgCharacters: {Length}", id, categoryForLog, content.Length);
        string svg;
        try { svg = SvgSymbolValidator.Validate(content); }
        catch (Exception error) when (error is XmlException or ArgumentException)
        {
            logger.LogWarning("Map symbol SVG validation rejected. SymbolId: {SymbolId}, ErrorType: {ErrorType}", id, error.GetType().Name);
            return ServiceResult.Problem(400, error is XmlException ? "Die Datei enthält kein gültiges SVG." : error.Message);
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
        symbol.Category = request.Category;
        symbol.SvgContent = svg;
        symbol.IsActive = request.IsActive;
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
        return ServiceResult.StatusCode(created ? 201 : 200, new MapSymbolDto(symbol.Id, symbol.Name, symbol.Category, symbol.SvgContent, symbol.IsActive));
    }
}
