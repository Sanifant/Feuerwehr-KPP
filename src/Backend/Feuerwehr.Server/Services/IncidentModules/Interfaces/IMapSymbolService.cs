using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines the map symbol operations required by application consumers.
    /// </summary>
    public interface IMapSymbolService
    {
        /// <summary>
        /// Retrieves the available situation-map symbols.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the available map symbol definitions, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetSymbols(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves map symbol categories visible to the caller.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the map symbol categories, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> GetCategories(CancellationToken cancellationToken);

        /// <summary>
        /// Creates map symbol categories from the validated input.
        /// </summary>
        /// <param name="request">The create map symbol category input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the newly created category, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> CreateCategory(CreateMapSymbolCategoryRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Creates or updates map symbol records after validating input and concurrency state.
        /// </summary>
        /// <param name="id">The existing map symbol identifier, or null to create a new record.</param>
        /// <param name="request">The save map symbol input to validate and apply.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the saved map symbol record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Save(Guid? id, SaveMapSymbolRequest request, CancellationToken cancellationToken);
    }
}
