using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller;

[ApiController, Route("api/inventory")]
[Authorize(Policy = PolicyNames.CanViewInventory)]
[RequestSizeLimit(MaximumImportBytes + 64 * 1024)]
/// <summary>
/// Executes inventory Transfer Controller.
/// </summary>
/// <param name="service">The service.</param>
public sealed class InventoryTransferController(IInventoryTransferService service) : ControllerBase
{
    public const long MaximumImportBytes = 5 * 1024 * 1024;

    [HttpGet("export")]
    /// <summary>
    /// Executes export Equipment.
    /// </summary>
    public async Task<IActionResult> ExportEquipment(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Equipment, ct));
    [HttpGet("types/export")]
    /// <summary>
    /// Executes export Types.
    /// </summary>
    public async Task<IActionResult> ExportTypes(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Types, ct));
    [HttpGet("categories/export")]
    /// <summary>
    /// Executes export Categories.
    /// </summary>
    public async Task<IActionResult> ExportCategories(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Categories, ct));

    [HttpPost("import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
    /// <summary>
    /// Executes import Equipment.
    /// </summary>
    /// <param name="file">The file.</param>
    public Task<IActionResult> ImportEquipment([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Equipment, file, ct);
    [HttpPost("types/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
    /// <summary>
    /// Executes import Types.
    /// </summary>
    /// <param name="file">The file.</param>
    public Task<IActionResult> ImportTypes([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Types, file, ct);
    [HttpPost("categories/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.RequireAdmin)]
    /// <summary>
    /// Executes import Categories.
    /// </summary>
    /// <param name="file">The file.</param>
    public Task<IActionResult> ImportCategories([FromForm] IFormFile? file, CancellationToken ct) => Import(InventoryTransferKind.Categories, file, ct);

    private async Task<IActionResult> Import(InventoryTransferKind kind, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new InventoryImportResponse(0, 0, [new(null, "Bitte eine nicht leere Importdatei auswÃ¤hlen.")]));
        if (file.Length > MaximumImportBytes) return BadRequest(new InventoryImportResponse(0, 0, [new(null, "Die Importdatei darf hÃ¶chstens 5 MB groÃŸ sein.")]));
        await using var stream = file.OpenReadStream();
        return this.ToActionResult(await service.Import(kind, stream, ct));
    }
}

