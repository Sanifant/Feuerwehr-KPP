using de.openelp.feuerwehr.Common.Models.Vehicles;
using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public sealed class VehicleController(IVehicleService service) : ControllerBase
{
    [HttpGet("statuses")]
    public async Task<IActionResult> Statuses([FromQuery] bool includeInactive, CancellationToken ct) => this.ToActionResult(await service.Statuses(includeInactive, ct));
    [HttpPost("statuses")]
    public async Task<IActionResult> CreateStatus(SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(null, request, ct));
    [HttpPut("statuses/{id:guid}")]
    public async Task<IActionResult> UpdateStatus(Guid id, SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(id, request, ct));
    [HttpGet("types")]
    public async Task<IActionResult> Types([FromQuery] bool includeInactive, CancellationToken ct) =>
        this.ToActionResult(await service.Types(includeInactive, ct));

    [HttpPost("types")]
    public async Task<IActionResult> CreateType(SaveVehicleTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(null, request, ct));

    [HttpPut("types/{id:guid}")]
    public async Task<IActionResult> UpdateType(Guid id, SaveVehicleTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(id, request, ct));

    [HttpPut("types/{id:guid}/active")]
    public async Task<IActionResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SetTypeActive(id, request, ct));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        this.ToActionResult(await service.List(includeInactive, ct));

    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct, [FromQuery] string? vehicleType = null) =>
        this.ToActionResult(await service.Availability(from, to, ct, vehicleType));

    [HttpGet("management-options")]
    public async Task<IActionResult> ManagementOptions(CancellationToken ct) => this.ToActionResult(await service.ManagementOptions(ct));

    [HttpPost]
    public async Task<IActionResult> Create(SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(null, request, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(id, request, ct));

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct) => this.ToActionResult(await service.SetActive(id, request, ct));

    [HttpGet("reservations")]
    public async Task<IActionResult> Reservations([FromQuery] string scope = "mine", CancellationToken ct = default) =>
        this.ToActionResult(await service.Reservations(scope, ct));

    [HttpPost("{id:guid}/reservations")]
    public async Task<IActionResult> Reserve(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Reserve(id, request, false, ct));

    [HttpPost("{id:guid}/direct-bookings")]
    public async Task<IActionResult> BookDirectly(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Reserve(id, request, true, ct));

    [HttpPost("reservations/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Confirmed, ct));

    [HttpPost("reservations/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Rejected, ct));

    [HttpPost("reservations/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Cancelled, ct));
}
