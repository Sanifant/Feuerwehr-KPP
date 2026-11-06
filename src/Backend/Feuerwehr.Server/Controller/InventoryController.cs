using De.Openelp.Feuerwehr.Common.Models.Inventory;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    [ApiController]
    [Route("api/inventory")]
    [Authorize(Policy = PolicyNames.CanViewInventory)]
    /// <summary>
    /// Executes inventory Controller.
    /// </summary>
    /// <param name="service">The service.</param>
    public sealed class InventoryController(IInventoryService service) : ControllerBase
    {
        [HttpGet("categories")]
        /// <summary>
        /// Executes categories.
        /// </summary>
        public async Task<IActionResult> Categories(CancellationToken ct) => this.ToActionResult(await service.GetCategories(ct));

        [HttpPost("categories"), Authorize(Policy = PolicyNames.RequireAdmin)]
        /// <summary>
        /// Creates the category.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.CreateCategory(request, ct));

        [HttpGet("types")]
        /// <summary>
        /// Executes types.
        /// </summary>
        public async Task<IActionResult> Types(CancellationToken ct) => this.ToActionResult(await service.GetTypes(ct));

        [HttpPost("types"), Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Creates the type.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> CreateType(SaveEquipmentTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(null, request, ct));

        [HttpPut("types/{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Updates the type.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> UpdateType(Guid id, SaveEquipmentTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(id, request, ct));

        [HttpGet]
        /// <summary>
        /// Executes list.
        /// </summary>
        /// <param name="includeInactive">The include Inactive.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
            this.ToActionResult(await service.GetAll(includeInactive, ct));

        [HttpGet("{id:guid}")]
        /// <summary>
        /// Gets the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<IActionResult> Get(Guid id, CancellationToken ct) => this.ToActionResult(await service.Get(id, ct));

        [HttpGet("barcode/{barcode}")]
        /// <summary>
        /// Executes barcode.
        /// </summary>
        /// <param name="barcode">The barcode.</param>
        public async Task<IActionResult> Barcode(string barcode, CancellationToken ct) => this.ToActionResult(await service.FindBarcode(barcode, ct));

        [HttpGet("{id:guid}/barcode.svg")]
        /// <summary>
        /// Executes barcode Label.
        /// </summary>
        /// <param name="id">The id.</param>
        public async Task<IActionResult> BarcodeLabel(Guid id, CancellationToken ct) => this.ToActionResult(await service.BarcodeLabel(id, ct));

        [HttpPost, Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Creates the operation.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> Create(SaveEquipmentRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Save(null, request, ct));

        [HttpPut("{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Updates the operation.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> Update(Guid id, SaveEquipmentRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Save(id, request, ct));

        [HttpPut("{id:guid}/active"), Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Sets the active.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SetActive(id, request, ct));

        [HttpPost("{id:guid}/inspections"), Authorize(Policy = PolicyNames.CanManageInventory)]
        /// <summary>
        /// Executes inspect.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="request">The request.</param>
        /// <param name="ct">The ct.</param>
        public async Task<IActionResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Inspect(id, request, ct));
    }

}
