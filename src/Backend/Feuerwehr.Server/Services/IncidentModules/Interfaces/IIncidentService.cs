using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the incident operations required by application consumers.
    /// </summary>
    public interface IIncidentService
    {
        /// <summary>
        /// Retrieves the current active incident, when one exists.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the active incident, when present, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetActiveIncident(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a page of closed incidents from the archive.
        /// </summary>
        /// <param name="page">The one-based page number to retrieve.</param>
        /// <param name="pageSize">The maximum number of records to include in a page.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the requested page of closed incidents, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetArchive(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an active incident, records the command outcome, and stages its live update event.
        /// </summary>
        /// <param name="request">The create incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the created incident and its initial editing lease, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CreateIncident(CreateIncidentRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the active incident after checking its revision and records the command and live event consistently.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The close incident input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the closed incident and updated revision, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CloseIncident(Guid incidentId, CloseIncidentRequest request, CancellationToken cancellationToken);
    }
}
