using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the situation map operations required by application consumers.
    /// </summary>
    public interface ISituationMapService
    {
        /// <summary>
        /// Retrieves the incident's map elements, hydrants, symbols, and current editing lease.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the incident's map state and editing lease, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetState(Guid incidentId, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to acquire the incident's map editing lease for the requesting user session.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The acquire lease input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the granted or renewed editing lease, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> AcquireLease(Guid incidentId, AcquireLeaseRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Renews the editing lease after validating its owner, session, and token.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The lease heartbeat input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the granted or renewed editing lease, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Heartbeat(Guid incidentId, LeaseHeartbeatRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Releases the editing lease owned by the requesting session.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The release lease input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing confirmation of the lease release, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Release(Guid incidentId, ReleaseLeaseRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Creates or updates a map element after lease and version checks and persists its audit and live event.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The upsert map element input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the map change outcome and updated incident revision, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> UpsertElement(Guid incidentId, UpsertMapElementRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a map element after lease and version checks while retaining its audit history.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="elementId">The identifier of the map element.</param>
        /// <param name="request">The delete map element input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the map change outcome and updated incident revision, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> DeleteElement(Guid incidentId, Guid elementId, DeleteMapElementRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a page of situation-map audit events for the incident.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="page">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in a page.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the requested page of map audit events, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetAudit(Guid incidentId, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports the incident's situation-map audit trail as CSV.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ExportAuditCsv(Guid incidentId, CancellationToken cancellationToken);

        /// <summary>
        /// Exports the incident's situation-map information as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ExportMapPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
