using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the incident diary operations required by application consumers.
    /// </summary>
    public interface IIncidentDiaryService
    {
        /// <summary>
        /// Retrieves the incident diary entries and their current revisions.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entries and revisions, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetEntries(Guid incidentId, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves incident diary categories visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the incident diary categories, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetCategories(CancellationToken cancellationToken);

        /// <summary>
        /// Creates a numbered diary entry and its initial revision, command outcome, and live event.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="request">The create diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CreateEntry(Guid incidentId, CreateDiaryEntryRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Appends a diary revision after validating the caller's expected revision.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="entryId">The identifier of the diary entry.</param>
        /// <param name="request">The update diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> UpdateEntry(Guid incidentId, Guid entryId, UpdateDiaryEntryRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels a diary entry by recording a new revision and retaining its previous content.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="entryId">The identifier of the diary entry.</param>
        /// <param name="request">The cancel diary entry input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entry and incident revision after the change, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CancelEntry(Guid incidentId, Guid entryId, CancelDiaryEntryRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Exports the incident diary as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ExportDiaryPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
