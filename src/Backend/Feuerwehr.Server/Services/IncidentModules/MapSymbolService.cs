using System.Xml;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Implements map symbol operations for the API application layer.
    /// </summary>
    /// <param name="dbContext">The database context for the current unit of work.</param>
    /// <param name="user">The accessor for the authenticated caller's identity and permissions.</param>
    /// <param name="logger">The logger used to record operational failures and diagnostics.</param>
    public sealed class MapSymbolService(FeuerwehrDbContext dbContext, IUserContextAccessor user, ILogger<MapSymbolService> logger) : IMapSymbolService
    {
        /// <summary>
        /// Retrieves map symbol categories visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the map symbol categories, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetCategories(CancellationToken cancellationToken)
            => ServiceResult.Ok(await dbContext.MapSymbolCategories.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(cancellationToken));

        /// <summary>
        /// Creates map symbol categories from the validated input.
        /// </summary>
        /// <param name="request">The create map symbol category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the newly created category, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            string? name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 40)
            {
                return ServiceResult.Problem(400, "Bitte einen Kategorienamen mit höchstens 40 Zeichen angeben.");
            }

            string normalizedName = name.ToUpperInvariant();
            if (await dbContext.MapSymbolCategories.AnyAsync(x => x.Name.ToUpper() == normalizedName, cancellationToken))
            {
                return ServiceResult.Problem(409, "Diese Symbolkategorie existiert bereits.");
            }

            _ = dbContext.MapSymbolCategories.Add(new MapSymbolCategory { Name = name });
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return ServiceResult.StatusCode(201, name);
        }

        /// <summary>
        /// Retrieves the available situation-map symbols.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the available map symbol definitions, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> GetSymbols(CancellationToken cancellationToken)
        {
            logger.LogDebug("Map symbol catalog load started. Admin: {IsAdmin}", user.IsAdmin);
            try
            {
                List<MapSymbolDto> items = await dbContext.MapSymbols.AsNoTracking().Where(x => x.IsActive || user.IsAdmin)
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
        /// Creates or updates map symbol records after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing map symbol identifier, or null to create a new record.</param>
        /// <param name="request">The save map symbol input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved map symbol record or success status, or validation, authorization, or conflict details on failure.</returns>
        public async Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                logger.LogWarning("Map symbol save rejected: administrator permission missing.");
                return ServiceResult.Forbid();
            }

            string category = request.Category ?? string.Empty;
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200
                || !await dbContext.MapSymbolCategories.AnyAsync(x => x.Name == category, cancellationToken))
            {
                logger.LogWarning("Map symbol save rejected: invalid name or category. SymbolId: {SymbolId}", id);
                return ServiceResult.Problem(400, "Bitte Name und eine gültige Symbolkategorie angeben.");
            }

            string content = request.SvgContent ?? string.Empty;
            string categoryForLog = category.Replace("\r", string.Empty).Replace("\n", string.Empty);
            logger.LogInformation("Map symbol save started. SymbolId: {SymbolId}, Category: {Category}, SvgCharacters: {Length}", id, categoryForLog, content.Length);
            string svg;
            try
            {
                svg = SvgSymbolValidator.Validate(content);
            }
            catch (Exception error) when (error is XmlException or ArgumentException)
            {
                logger.LogWarning("Map symbol SVG validation rejected. SymbolId: {SymbolId}, ErrorType: {ErrorType}", id, error.GetType().Name);
                return ServiceResult.Problem(400, error is XmlException ? "Die Datei enthält kein gültiges SVG." : error.Message);
            }

            logger.LogDebug("Map symbol SVG validation completed. SymbolId: {SymbolId}", id);

            logger.LogDebug("Map symbol database lookup started. SymbolId: {SymbolId}", id);
            MapSymbol? symbol = id.HasValue ? await dbContext.MapSymbols.FindAsync([id.Value], cancellationToken) : null;
            if (id.HasValue && symbol is null)
            {
                logger.LogWarning("Map symbol save rejected: symbol was not found. SymbolId: {SymbolId}", id);
                return ServiceResult.NotFound();
            }

            bool created = symbol is null;
            symbol ??= new MapSymbol { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
            symbol.Name = request.Name.Trim();
            symbol.Category = category;
            symbol.SvgContent = svg;
            symbol.IsActive = request.IsActive;
            symbol.IsVehicle = request.IsVehicle;
            symbol.UpdatedAtUtc = DateTime.UtcNow;
            if (created)
            {
                _ = dbContext.MapSymbols.Add(symbol);
            }

            logger.LogDebug("Map symbol database save started. SymbolId: {SymbolId}", symbol.Id);
            try
            {
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                logger.LogError(error, "Map symbol database save failed. SymbolId: {SymbolId}", symbol.Id);
                throw;
            }

            logger.LogInformation("Map symbol save completed. SymbolId: {SymbolId}, Created: {Created}", symbol.Id, created);
            return ServiceResult.StatusCode(created ? 201 : 200, new MapSymbolDto(symbol.Id, symbol.Name, symbol.Category, symbol.SvgContent, symbol.IsActive, symbol.IsVehicle));
        }
    }
}
