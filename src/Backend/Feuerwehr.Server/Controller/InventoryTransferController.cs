using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for inventory transfer operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The inventory transfer service handling the requested operations.</param>
    [ApiController, Route("api/inventory")]
    [Authorize(Policy = PolicyNames.CanViewInventory)]
    [RequestSizeLimit(MaximumImportBytes + (64 * 1024))]
    public sealed class InventoryTransferController(IInventoryTransferService service) : ControllerBase
    {
        /// <summary>
        /// The maximum accepted upload size in bytes.
        /// </summary>
        public const long MaximumImportBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Exports equipment for inventory transfer in the supported download format.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportEquipment(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Equipment, ct));

        /// <summary>
        /// Exports types for inventory transfer as JSON.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("types/export")]
        public async Task<IActionResult> ExportTypes(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Types, ct));

        /// <summary>
        /// Exports categories for inventory transfer as JSON.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("categories/export")]
        public async Task<IActionResult> ExportCategories(CancellationToken ct) => this.ToActionResult(await service.Export(InventoryTransferKind.Categories, ct));

        /// <summary>
        /// Validates and imports equipment for inventory transfer, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the inventory transfer operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public Task<IActionResult> ImportEquipment([FromForm] IFormFile? file, CancellationToken ct) => this.Import(InventoryTransferKind.Equipment, file, ct);

        /// <summary>
        /// Validates and imports types for inventory transfer, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the inventory transfer operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("types/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public Task<IActionResult> ImportTypes([FromForm] IFormFile? file, CancellationToken ct) => this.Import(InventoryTransferKind.Types, file, ct);

        /// <summary>
        /// Validates and imports categories for inventory transfer, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="file">The uploaded import file; null when no file was supplied.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the outcome of the inventory transfer operation, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("categories/import"), Consumes("multipart/form-data"), Authorize(Policy = PolicyNames.RequireAdmin)]
        public Task<IActionResult> ImportCategories([FromForm] IFormFile? file, CancellationToken ct) => this.Import(InventoryTransferKind.Categories, file, ct);

        private async Task<IActionResult> Import(InventoryTransferKind kind, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return this.BadRequest(new InventoryImportResponse(0, 0, [new (null, "Bitte eine nicht leere Importdatei auswählen.")]));
            }

            if (file.Length > MaximumImportBytes)
            {
                return this.BadRequest(new InventoryImportResponse(0, 0, [new (null, "Die Importdatei darf höchstens 5 MB groÃŸ sein.")]));
            }

            await using Stream stream = file.OpenReadStream();
            return this.ToActionResult(await service.Import(kind, stream, ct));
        }
    }
}
