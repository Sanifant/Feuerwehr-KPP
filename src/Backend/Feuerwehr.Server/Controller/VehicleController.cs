using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller;

[ApiController]
[Route("api/vehicles")]
[Authorize]
/// <summary>
/// Executes vehicle Controller.
/// </summary>
/// <param name="service">The service.</param>
public sealed class VehicleController(IVehicleService service) : ControllerBase
{
    [HttpGet("statuses")]
    /// <summary>
    /// Executes statuses.
    /// </summary>
    /// <param name="includeInactive">The include Inactive.</param>
    public async Task<IActionResult> Statuses([FromQuery] bool includeInactive, CancellationToken ct) => this.ToActionResult(await service.Statuses(includeInactive, ct));
    [HttpPost("statuses")]
    /// <summary>
    /// Creates the status.
    /// </summary>
    /// <param name="request">The request.</param>
    public async Task<IActionResult> CreateStatus(SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(null, request, ct));
    [HttpPut("statuses/{id:guid}")]
    /// <summary>
    /// Updates the status.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    public async Task<IActionResult> UpdateStatus(Guid id, SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(id, request, ct));
    [HttpGet("types")]
    /// <summary>
    /// Executes types.
    /// </summary>
    /// <param name="includeInactive">The include Inactive.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Types([FromQuery] bool includeInactive, CancellationToken ct) =>
        this.ToActionResult(await service.Types(includeInactive, ct));

    [HttpPost("types")]
    /// <summary>
    /// Creates the type.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> CreateType(SaveVehicleTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(null, request, ct));

    [HttpPut("types/{id:guid}")]
    /// <summary>
    /// Updates the type.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> UpdateType(Guid id, SaveVehicleTypeRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SaveType(id, request, ct));

    [HttpPut("types/{id:guid}/active")]
    /// <summary>
    /// Sets the type Active.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.SetTypeActive(id, request, ct));

    [HttpGet]
    /// <summary>
    /// Executes list.
    /// </summary>
    /// <param name="includeInactive">The include Inactive.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        this.ToActionResult(await service.List(includeInactive, ct));

    [HttpGet("availability")]
    /// <summary>
    /// Executes availability.
    /// </summary>
    /// <param name="from">The from.</param>
    /// <param name="to">The to.</param>
    /// <param name="ct">The ct.</param>
    /// <param name="vehicleType">The vehicle Type.</param>
    public async Task<IActionResult> Availability([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct, [FromQuery] string? vehicleType = null) =>
        this.ToActionResult(await service.Availability(from, to, ct, vehicleType));

    [HttpGet("management-options")]
    /// <summary>
    /// Executes management Options.
    /// </summary>
    public async Task<IActionResult> ManagementOptions(CancellationToken ct) => this.ToActionResult(await service.ManagementOptions(ct));

    [HttpPost]
    /// <summary>
    /// Creates the operation.
    /// </summary>
    /// <param name="request">The request.</param>
    public async Task<IActionResult> Create(SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(null, request, ct));

    [HttpPut("{id:guid}")]
    /// <summary>
    /// Updates the operation.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    public async Task<IActionResult> Update(Guid id, SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(id, request, ct));

    [HttpPut("{id:guid}/active")]
    /// <summary>
    /// Sets the active.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    public async Task<IActionResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct) => this.ToActionResult(await service.SetActive(id, request, ct));

    [HttpGet("reservations")]
    /// <summary>
    /// Executes reservations.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Reservations([FromQuery] string scope = "mine", CancellationToken ct = default) =>
        this.ToActionResult(await service.Reservations(scope, ct));

    [HttpPost("{id:guid}/reservations")]
    /// <summary>
    /// Executes reserve.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Reserve(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Reserve(id, request, false, ct));

    [HttpPost("{id:guid}/direct-bookings")]
    /// <summary>
    /// Executes book Directly.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> BookDirectly(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Reserve(id, request, true, ct));

    [HttpPost("reservations/{id:guid}/approve")]
    /// <summary>
    /// Executes approve.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Approve(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Confirmed, ct));

    [HttpPost("reservations/{id:guid}/reject")]
    /// <summary>
    /// Executes reject.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Reject(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Rejected, ct));

    [HttpPost("reservations/{id:guid}/cancel")]
    /// <summary>
    /// Executes cancel.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="ct">The ct.</param>
    public async Task<IActionResult> Cancel(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
        this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Cancelled, ct));
}

