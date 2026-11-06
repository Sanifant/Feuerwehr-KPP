using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for equipment operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The inventory service handling the requested operations.</param>
    [ApiController]
    [Route("api/inventory")]
    [Authorize(Policy = PolicyNames.CanViewInventory)]
    public sealed class InventoryController(IInventoryService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves inventory categories visible to the caller.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the inventory categories, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("categories")]
        public async Task<IActionResult> Categories(CancellationToken ct) => this.ToActionResult(await service.GetCategories(ct));

        /// <summary>
        /// Creates inventory categories from the validated input.
        /// </summary>
        /// <param name="request">The create inventory category input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the newly created category, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("categories"), Authorize(Policy = PolicyNames.RequireAdmin)]
        public async Task<IActionResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.CreateCategory(request, ct));

        /// <summary>
        /// Retrieves equipment types and their inspection plans visible to the caller.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the equipment types and inspection plans, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("types")]
        public async Task<IActionResult> Types(CancellationToken ct) => this.ToActionResult(await service.GetTypes(ct));

        /// <summary>
        /// Creates equipment types and their inspection plans from the validated input.
        /// </summary>
        /// <param name="request">The save equipment type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved equipment type and inspection plan, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("types"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> CreateType(SaveEquipmentTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(null, request, ct));

        /// <summary>
        /// Updates equipment types and their inspection plans matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the equipment type to process.</param>
        /// <param name="request">The save equipment type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved equipment type and inspection plan, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("types/{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> UpdateType(Guid id, SaveEquipmentTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(id, request, ct));

        /// <summary>
        /// Retrieves equipment records visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the matching equipment records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
            this.ToActionResult(await service.GetAll(includeInactive, ct));

        /// <summary>
        /// Retrieves equipment records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the equipment to retrieve.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the matching equipment record, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct) => this.ToActionResult(await service.Get(id, ct));

        /// <summary>
        /// Retrieves equipment details and inspection history for the supplied barcode.
        /// </summary>
        /// <param name="barcode">The unique equipment barcode used for lookup and import matching.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the equipment details and inspection history, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> Barcode(string barcode, CancellationToken ct) => this.ToActionResult(await service.FindBarcode(barcode, ct));

        /// <summary>
        /// Generates the equipment barcode as an SVG label suitable for printing.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("{id:guid}/barcode.svg")]
        public async Task<IActionResult> BarcodeLabel(Guid id, CancellationToken ct) => this.ToActionResult(await service.BarcodeLabel(id, ct));

        /// <summary>
        /// Creates equipment records from the validated input.
        /// </summary>
        /// <param name="request">The save equipment input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved equipment record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost, Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> Create(SaveEquipmentRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Save(null, request, ct));

        /// <summary>
        /// Updates equipment records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The save equipment input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved equipment record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> Update(Guid id, SaveEquipmentRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Save(id, request, ct));

        /// <summary>
        /// Changes the activation state of equipment records while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The set equipment active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id:guid}/active"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SetActive(id, request, ct));

        /// <summary>
        /// Records an equipment inspection with concurrency checking and a snapshot of its inspection plan.
        /// </summary>
        /// <param name="id">The identifier of the equipment to process.</param>
        /// <param name="request">The record equipment inspection input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the equipment details and inspection history, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("{id:guid}/inspections"), Authorize(Policy = PolicyNames.CanManageInventory)]
        public async Task<IActionResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Inspect(id, request, ct));
    }
}
