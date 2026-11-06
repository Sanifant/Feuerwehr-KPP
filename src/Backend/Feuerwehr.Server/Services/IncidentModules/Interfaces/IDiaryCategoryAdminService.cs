using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the diary category operations required by application consumers.
    /// </summary>
    public interface IDiaryCategoryAdminService
    {
        /// <summary>
        /// Retrieves diary category records visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the matching diary category records, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetAll(CancellationToken cancellationToken);

        /// <summary>
        /// Creates or updates diary category records after validating input and concurrency state.
        /// </summary>
        /// <param name="request">The upsert diary category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the outcome of the diary category operation, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Upsert(UpsertDiaryCategoryRequest request, CancellationToken cancellationToken);
    }
}
