using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for vehicle transfer operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The vehicle transfer service handling the requested operations.</param>
    [ApiController, Route("api/vehicles"), Authorize]
    [RequestSizeLimit(MaximumImportBytes + (64 * 1024))]
    public sealed class VehicleTransferController(IVehicleTransferService service) : ControllerBase
    {
        /// <summary>
        /// The maximum accepted upload size in bytes.
        /// </summary>
        public const long MaximumImportBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Exports vehicles for vehicle transfer in the supported download format.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportVehicles(CancellationToken ct) => this.ToActionResult(await service.Export(VehicleTransferKind.Vehicles, ct));

        /// <summary>
        /// Exports types for vehicle transfer as JSON.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("types/export")]
        public async Task<IActionResult> ExportTypes(CancellationToken ct) => this.ToActionResult(await service.Export(VehicleTransferKind.Types, ct));

        /// <summary>
        /// Validates and imports vehicles for vehicle transfer, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the vehicle transfer operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("import"), Consumes("multipart/form-data")]
        public Task<IActionResult> ImportVehicles([FromForm] IFormFile? file, CancellationToken ct) => this.Import(VehicleTransferKind.Vehicles, file, ct);

        /// <summary>
        /// Validates and imports types for vehicle transfer, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the vehicle transfer operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("types/import"), Consumes("multipart/form-data")]
        public Task<IActionResult> ImportTypes([FromForm] IFormFile? file, CancellationToken ct) => this.Import(VehicleTransferKind.Types, file, ct);

        private async Task<IActionResult> Import(VehicleTransferKind kind, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return this.BadRequest(new VehicleImportResponse(0, 0, [new (null, "Bitte eine nicht leere Importdatei auswählen.")]));
            }

            if (file.Length > MaximumImportBytes)
            {
                return this.BadRequest(new VehicleImportResponse(0, 0, [new (null, "Die Importdatei darf höchstens 5 MB groÃŸ sein.")]));
            }

            await using Stream stream = file.OpenReadStream();
            return this.ToActionResult(await service.Import(kind, stream, ct));
        }
    }
}
