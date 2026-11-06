namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the incident archive operations required by application consumers.
    /// </summary>
    public interface IIncidentArchiveService
    {
        /// <summary>
        /// Retrieves the archived incident diary with its retained revisions.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the diary entries and revisions, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetDiary(Guid incidentId, CancellationToken cancellationToken);

        /// <summary>
        /// Exports the archived incident diary as a PDF document.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the download content and MIME type, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ExportPdf(Guid incidentId, CancellationToken cancellationToken);
    }
}
