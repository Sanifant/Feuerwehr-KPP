using De.Openelp.Feuerwehr.Common.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the vehicle operations required by application consumers.
    /// </summary>
    public interface IVehicleService
    {
        /// <summary>
        /// Retrieves vehicle types visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the vehicle type definitions, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Types(bool includeInactive, CancellationToken ct);

        /// <summary>
        /// Retrieves vehicle status definitions visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the configured vehicle status definitions, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Statuses(bool includeInactive, CancellationToken ct);

        /// <summary>
        /// Creates or updates vehicle status definitions after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing vehicle status identifier, or null to create a new record.</param>
        /// <param name="request">The save vehicle status input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved vehicle status definition, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SaveStatus(Guid? id, SaveVehicleStatusRequest request, CancellationToken ct);

        /// <summary>
        /// Creates or updates vehicle types after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing vehicle type identifier, or null to create a new record.</param>
        /// <param name="request">The save vehicle type input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved vehicle type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SaveType(Guid? id, SaveVehicleTypeRequest request, CancellationToken ct);

        /// <summary>
        /// Changes the activation state of vehicle types while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the vehicle type to process.</param>
        /// <param name="request">The set vehicle type active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SetTypeActive(Guid id, SetVehicleTypeActiveRequest request, CancellationToken ct);

        /// <summary>
        /// Retrieves vehicle records visible to the caller.
        /// </summary>
        /// <param name="includeInactive">Whether to include deactivated records in the result.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the matching vehicle records, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> List(bool includeInactive, CancellationToken ct);

        /// <summary>
        /// Retrieves vehicle availability for every day in the inclusive date range and optional vehicle-type filter.
        /// </summary>
        /// <param name="from">The first date of the reservation range, inclusive.</param>
        /// <param name="to">The last date of the reservation range, inclusive.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <param name="vehicleType">An optional vehicle-type filter for the availability calendar.</param>
        /// <returns>A task whose result is an application result containing daily vehicle availability for the requested range, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Availability(DateOnly from, DateOnly to, CancellationToken ct, string? vehicleType = null);

        /// <summary>
        /// Retrieves the departments and equipment officers available when assigning a vehicle.
        /// </summary>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the assignable departments and officers, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ManagementOptions(CancellationToken ct);

        /// <summary>
        /// Creates or updates vehicle records after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing vehicle identifier, or null to create a new record.</param>
        /// <param name="request">The save vehicle input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved vehicle record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Save(Guid? id, SaveVehicleRequest request, CancellationToken ct);

        /// <summary>
        /// Changes the activation state of vehicle records while preserving existing history.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The set vehicle active input to validate and apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the updated activation state, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> SetActive(Guid id, SetVehicleActiveRequest request, CancellationToken ct);

        /// <summary>
        /// Retrieves vehicle reservations visible in the requested ownership or management scope.
        /// </summary>
        /// <param name="scope">The reservation view to retrieve, such as the current user's bookings.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the reservations in the requested ownership or management scope, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Reservations(string scope, CancellationToken ct);

        /// <summary>
        /// Creates a vehicle reservation request or directly confirms the requested range when authorized.
        /// </summary>
        /// <param name="id">The identifier of the vehicle to process.</param>
        /// <param name="request">The create vehicle reservation input to validate and apply.</param>
        /// <param name="direct">Whether to claim a confirmed booking directly instead of creating a pending request.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Reserve(Guid id, CreateVehicleReservationRequest request, bool direct, CancellationToken ct);

        /// <summary>
        /// Applies an authorized reservation decision, claiming or releasing the affected calendar days consistently.
        /// </summary>
        /// <param name="id">The identifier of the vehicle reservation to process.</param>
        /// <param name="request">The decide vehicle reservation input to validate and apply.</param>
        /// <param name="status">The reservation decision status to apply.</param>
        /// <param name="ct">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the reservation after the requested booking or decision, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Decide(Guid id, DecideVehicleReservationRequest request, VehicleReservationStatus status, CancellationToken ct);
    }
}
