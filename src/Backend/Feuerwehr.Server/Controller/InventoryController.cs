using de.openelp.feuerwehr.Common.Models.Inventory;
using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[ApiController]
[Route("api/inventory")]
[Authorize(Policy = PolicyNames.CanViewInventory)]
public sealed class InventoryController(IInventoryService service) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct) => this.ToActionResult(await service.GetCategories(ct));

    [HttpPost("categories"), Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<IActionResult> CreateCategory(CreateInventoryCategoryRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.CreateCategory(request, ct));

    [HttpGet("types")]
    public async Task<IActionResult> Types(CancellationToken ct) => this.ToActionResult(await service.GetTypes(ct));

    [HttpPost("types"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> CreateType(SaveEquipmentTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(null, request, ct));

    [HttpPut("types/{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> UpdateType(Guid id, SaveEquipmentTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(id, request, ct));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        this.ToActionResult(await service.GetAll(includeInactive, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => this.ToActionResult(await service.Get(id, ct));

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> Barcode(string barcode, CancellationToken ct) => this.ToActionResult(await service.FindBarcode(barcode, ct));

    [HttpGet("{id:guid}/barcode.svg")]
    public async Task<IActionResult> BarcodeLabel(Guid id, CancellationToken ct) => this.ToActionResult(await service.BarcodeLabel(id, ct));

    [HttpPost, Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> Create(SaveEquipmentRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Save(null, request, ct));

    [HttpPut("{id:guid}"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> Update(Guid id, SaveEquipmentRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Save(id, request, ct));

    [HttpPut("{id:guid}/active"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> SetActive(Guid id, SetEquipmentActiveRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SetActive(id, request, ct));

    [HttpPost("{id:guid}/inspections"), Authorize(Policy = PolicyNames.CanManageInventory)]
    public async Task<IActionResult> Inspect(Guid id, RecordEquipmentInspectionRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Inspect(id, request, ct));
}
