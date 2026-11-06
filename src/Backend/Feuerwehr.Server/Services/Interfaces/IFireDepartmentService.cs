using De.Openelp.Feuerwehr.Common.Models.Dto;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the fire department operations required by application consumers.
    /// </summary>
    public interface IFireDepartmentService
    {
        /// <summary>
        /// Retrieves fire department records visible to the caller.
        /// </summary>
        /// <returns>A task whose result is an application result containing the matching fire department records, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Get();

        /// <summary>
        /// Creates fire department records from the validated input.
        /// </summary>
        /// <param name="fireDepartmentDto">The fire department details to persist.</param>
        /// <returns>A task whose result is an application result containing the saved fire department record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Add(FireDepartmentDto fireDepartmentDto);

        /// <summary>
        /// Retrieves fire department records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the fire department to retrieve.</param>
        /// <returns>A task whose result is an application result containing the matching fire department record, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Get(int id);
    }
}
