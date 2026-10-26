using de.openelp.feuerwehr.Common.Models.IncidentModules;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller.IncidentModules;

[ApiController]
[Route("api/map-symbols")]
[Authorize(Policy = PolicyNames.CanViewMapSymbols)]
public sealed class MapSymbolController(IMapSymbolService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSymbols(CancellationToken cancellationToken)
        => this.ToActionResult(await service.GetSymbols(cancellationToken));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        => this.ToActionResult(await service.GetCategories(cancellationToken));

    [HttpPost("categories")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.CreateCategory(request, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [RequestSizeLimit(2097152)]
    public async Task<IActionResult> Create(SaveMapSymbolRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.Save(null, request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    [RequestSizeLimit(2097152)]
    public async Task<IActionResult> Update(Guid id, SaveMapSymbolRequest request, CancellationToken cancellationToken)
        => this.ToActionResult(await service.Save(id, request, cancellationToken));
}
