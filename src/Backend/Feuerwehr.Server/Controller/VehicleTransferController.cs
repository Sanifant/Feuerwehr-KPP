using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller;

[ApiController, Route("api/vehicles"), Authorize]
[RequestSizeLimit(MaximumImportBytes + 64 * 1024)]
/// <summary>
/// Executes vehicle Transfer Controller.
/// </summary>
/// <param name="service">The service.</param>
public sealed class VehicleTransferController(IVehicleTransferService service) : ControllerBase
{
    public const long MaximumImportBytes = 5 * 1024 * 1024;
    [HttpGet("export")]
    /// <summary>
    /// Executes export Vehicles.
    /// </summary>
    public async Task<IActionResult> ExportVehicles(CancellationToken ct) => this.ToActionResult(await service.Export(VehicleTransferKind.Vehicles, ct));
    [HttpGet("types/export")]
    /// <summary>
    /// Executes export Types.
    /// </summary>
    public async Task<IActionResult> ExportTypes(CancellationToken ct) => this.ToActionResult(await service.Export(VehicleTransferKind.Types, ct));
    [HttpPost("import"), Consumes("multipart/form-data")]
    /// <summary>
    /// Executes import Vehicles.
    /// </summary>
    /// <param name="file">The file.</param>
    public Task<IActionResult> ImportVehicles([FromForm] IFormFile? file, CancellationToken ct) => Import(VehicleTransferKind.Vehicles, file, ct);
    [HttpPost("types/import"), Consumes("multipart/form-data")]
    /// <summary>
    /// Executes import Types.
    /// </summary>
    /// <param name="file">The file.</param>
    public Task<IActionResult> ImportTypes([FromForm] IFormFile? file, CancellationToken ct) => Import(VehicleTransferKind.Types, file, ct);
    private async Task<IActionResult> Import(VehicleTransferKind kind, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new VehicleImportResponse(0, 0, [new(null, "Bitte eine nicht leere Importdatei auswÃ¤hlen.")]));
        if (file.Length > MaximumImportBytes) return BadRequest(new VehicleImportResponse(0, 0, [new(null, "Die Importdatei darf hÃ¶chstens 5 MB groÃŸ sein.")]));
        await using var stream = file.OpenReadStream();
        return this.ToActionResult(await service.Import(kind, stream, ct));
    }
}

