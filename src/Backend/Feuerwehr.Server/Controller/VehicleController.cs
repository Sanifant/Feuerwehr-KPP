using De.Openelp.Feuerwehr.Common.Models.Vehicles;
using De.Openelp.Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace De.Openelp.Feuerwehr.Server.Controller
{
    /// <summary>
    /// Exposes HTTP endpoints for vehicle operations and maps application results to responses.
    /// </summary>
    /// <param name="service">The vehicle service handling the requested operations.</param>
    [ApiController]
    [Route("api/vehicles")]
    [Authorize]
    public sealed class VehicleController(IVehicleService service) : ControllerBase
    {
        /// <summary>
        /// Retrieves vehicle status definitions visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the configured vehicle status definitions, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("statuses")]
        public async Task<IActionResult> Statuses([FromQuery] bool includeInactive, CancellationToken ct) => this.ToActionResult(await service.Statuses(includeInactive, ct));

        /// <summary>
        /// Creates vehicle status definitions from the validated input.
        /// </summary>
        /// <param name="request">The save vehicle status input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle status definition, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("statuses")]
        public async Task<IActionResult> CreateStatus(SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(null, request, ct));

        /// <summary>
        /// Updates vehicle status definitions matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the vehicle status to process.</param>
        /// <param name="request">The save vehicle status input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle status definition, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("statuses/{id:guid}")]
        public async Task<IActionResult> UpdateStatus(Guid id, SaveVehicleStatusRequest request, CancellationToken ct) => this.ToActionResult(await service.SaveStatus(id, request, ct));

        /// <summary>
        /// Retrieves vehicle types visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the vehicle type definitions, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("types")]
        public async Task<IActionResult> Types([FromQuery] bool includeInactive, CancellationToken ct) =>
            this.ToActionResult(await service.Types(includeInactive, ct));

        /// <summary>
        /// Creates vehicle types from the validated input.
        /// </summary>
        /// <param name="request">The save vehicle type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle type, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("types")]
        public async Task<IActionResult> CreateType(SaveVehicleTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(null, request, ct));

        /// <summary>
        /// Updates vehicle types matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the vehicle type to process.</param>
        /// <param name="request">The save vehicle type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle type, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("types/{id:guid}")]
        public async Task<IActionResult> UpdateType(Guid id, SaveVehicleTypeRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SaveType(id, request, ct));

        /// <summary>
        /// Changes the activation state of vehicle types while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the vehicle type to process.</param>
        /// <param name="request">The set vehicle type active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("types/{id:guid}/active")]
        public async Task<IActionResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.SetTypeActive(id, request, ct));

        /// <summary>
        /// Retrieves vehicle records visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the matching vehicle records, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
            this.ToActionResult(await service.List(includeInactive, ct));

        /// <summary>
        /// Retrieves vehicle availability for every day in the inclusive date range and optional vehicle-type filter.
        /// </summary>
        /// <param name="from">The first date of the reservation range, inclusive.</param>
        /// <param name="to">The last date of the reservation range, inclusive.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <param name="vehicleType">An optional vehicle-type filter for the availability calendar.</param>
        /// <returns>A task whose result is an HTTP response containing daily vehicle availability for the requested range, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("availability")]
        public async Task<IActionResult> Availability([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct, [FromQuery] string? vehicleType = null) =>
            this.ToActionResult(await service.Availability(from, to, ct, vehicleType));

        /// <summary>
        /// Retrieves the departments and equipment officers available when assigning a vehicle.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the assignable departments and officers, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("management-options")]
        public async Task<IActionResult> ManagementOptions(CancellationToken ct) => this.ToActionResult(await service.ManagementOptions(ct));

        /// <summary>
        /// Creates vehicle records from the validated input.
        /// </summary>
        /// <param name="request">The save vehicle input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost]
        public async Task<IActionResult> Create(SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(null, request, ct));

        /// <summary>
        /// Updates vehicle records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The save vehicle input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the saved vehicle record or success status, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, SaveVehicleRequest request, CancellationToken ct) => this.ToActionResult(await service.Save(id, request, ct));

        /// <summary>
        /// Changes the activation state of vehicle records while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The set vehicle active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        [HttpPut("{id:guid}/active")]
        public async Task<IActionResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct) => this.ToActionResult(await service.SetActive(id, request, ct));

        /// <summary>
        /// Retrieves vehicle reservations visible in the requested ownership or management scope.
        /// </summary>
        /// <param name="scope">The reservation view to retrieve, such as the current user's bookings.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservations in the requested ownership or management scope, or validation, authorization, or conflict details on failure.</returns>
        [HttpGet("reservations")]
        public async Task<IActionResult> Reservations([FromQuery] string scope = "mine", CancellationToken ct = default) =>
            this.ToActionResult(await service.Reservations(scope, ct));

        /// <summary>
        /// Creates a vehicle reservation request or directly confirms the requested range when authorized.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The create vehicle reservation input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("{id:guid}/reservations")]
        public async Task<IActionResult> Reserve(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Reserve(id, request, false, ct));

        /// <summary>
        /// Confirms a vehicle booking directly and claims all requested calendar days atomically.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The create vehicle reservation input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("{id:guid}/direct-bookings")]
        public async Task<IActionResult> BookDirectly(Guid id, CreateVehicleReservationRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Reserve(id, request, true, ct));

        /// <summary>
        /// Approves a pending vehicle reservation and claims its complete date range.
        /// </summary>
        /// <param name="id">The identifier of the vehicle reservation to process.</param>
        /// <param name="request">The decide vehicle reservation input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("reservations/{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Confirmed, ct));

        /// <summary>
        /// Rejects a pending vehicle reservation with the supplied decision comment.
        /// </summary>
        /// <param name="id">The identifier of the vehicle reservation to process.</param>
        /// <param name="request">The decide vehicle reservation input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("reservations/{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Rejected, ct));

        /// <summary>
        /// Cancels an authorized vehicle reservation while retaining its booking history.
        /// </summary>
        /// <param name="id">The identifier of the vehicle reservation to process.</param>
        /// <param name="request">The decide vehicle reservation input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an HTTP response containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        [HttpPost("reservations/{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, DecideVehicleReservationRequest request, CancellationToken ct) =>
            this.ToActionResult(await service.Decide(id, request, VehicleReservationStatus.Cancelled, ct));
    }
}
