using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller.IncidentModules;

[ApiController]
[Route("api/map-symbols")]
[Authorize(Policy = PolicyNames.CanViewMapSymbols)]
/// <summary>
/// Executes map Symbol Controller.
/// </summary>
/// <param name="service">The service.</param>
public sealed class MapSymbolController(IMapSymbolService service) : ControllerBase
{
    [HttpGet]
    /// <summary>
    /// Gets the symbols.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> GetSymbols(CancellationToken cancellationToken)
        => this.ToActionResult(await service.GetSymbols(cancellationToken));

    [HttpGet("categories")]
    /// <summary>
    /// Gets the categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        => this.ToActionResult(await service.GetCategories(cancellationToken));

    [HttpPost("categories")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    /// <summary>
    /// Creates the category.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.CreateCategory(request, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [RequestSizeLimit(2097152)]
    /// <summary>
    /// Creates the operation.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> Create(SaveMapSymbolRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.Save(null, request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [RequestSizeLimit(2097152)]
    /// <summary>
    /// Updates the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<IActionResult> Update(Guid id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.Save(id, request, cancellationToken));
}

