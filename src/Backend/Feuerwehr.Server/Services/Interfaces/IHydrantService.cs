using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the hydrant operations required by application consumers.
    /// </summary>
    public interface IHydrantService
    {
        /// <summary>
        /// Retrieves hydrant records visible to the caller.
        /// </summary>
        /// <returns>An application result containing the matching hydrant records, or validation, authorization, or conflict details on failure.</returns>
        ServiceResult Get();

        /// <summary>
        /// Retrieves hydrant records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to retrieve.</param>
        /// <returns>An application result containing the matching hydrant record, or validation, authorization, or conflict details on failure.</returns>
        ServiceResult Get(int id);

        /// <summary>
        /// Creates hydrant records from the validated input.
        /// </summary>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An application result containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        ServiceResult Post(Hydrant value);

        /// <summary>
        /// Updates hydrant records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <param name="value">The hydrant value to process.</param>
        /// <returns>An application result containing the saved hydrant record or success status, or validation, authorization, or conflict details on failure.</returns>
        ServiceResult Put(int id, Hydrant value);

        /// <summary>
        /// Deletes the hydrant record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <returns>An application result containing confirmation that the record was deleted, or validation, authorization, or conflict details on failure.</returns>
        ServiceResult Delete(int id);

        /// <summary>
        /// Exports hydrant records in the supported download format.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the generated export file bytes.</returns>
        Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates and imports hydrant records, rejecting invalid input before applying changes.
        /// </summary>
        /// <param name="csv">The readable CSV stream to validate and import.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is an application result containing the created and updated record counts and any import errors, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> ImportAsync(Stream csv, CancellationToken cancellationToken = default);
    }
}
