using de.openelp.feuerwehr.Common.Models.Inventory;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[ApiController, Route("api/inventory")]
[Authorize(Policy = PolicyNames.CanViewInventory)]
[RequestSizeLimit(MaximumImportBytes + 64 * 1024)]
public sealed class InventoryTransferController(IInventoryTransferService service) : ControllerBase
{
    public const long MaximumImportBytes = 5 * 1024 * 1024;

    [HttpGet("export")]
    public async Task<IActionResult> ExportEquipment(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Equipment, ct));
    [HttpGet("types/export")]
    public async Task<IActionResult> ExportTypes(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Types, ct));
    [HttpGet("categories/export")]
    public async Task<IActionResult> ExportCategories(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Categories, ct));

    [HttpPost("import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public Task<IActionResult> ImportEquipment([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Equipment, file, ct);
    [HttpPost("types/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public Task<IActionResult> ImportTypes([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Types, file, ct);
    [HttpPost("categories/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.RequireAdmin)]
    public Task<IActionResult> ImportCategories([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Categories, file, ct);

    private async Task<IActionResult> Import(InventoryTransferKind kind, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new InventoryImportResponse(0, 0, [new(null, "Bitte eine nicht leere Importdatei auswählen.")]));
        if (file.Length > MaximumImportBytes) return BadRequest(new InventoryImportResponse(0, 0, [new(null, "Die Importdatei darf höchstens 5 MB groß sein.")]));
        await using var stream = file.OpenReadStream();
        return this.ToActionResult(await service.Import(kind, stream, ct));
    }
}
