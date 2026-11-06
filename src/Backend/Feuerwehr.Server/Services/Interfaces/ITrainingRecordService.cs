using De.Openelp.Feuerwehr.Common.Models.Dto;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Defines the training course operations required by application consumers.
    /// </summary>
    public interface ITrainingRecordService
    {
        /// <summary>
        /// Retrieves training course records visible to the caller.
        /// </summary>
        /// <param name="level">The training qualification level.</param>
        /// <param name="title">The title to include in the response or exported document.</param>
        /// <returns>A task whose result is an application result containing the matching training course records, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Get(string? level, string? title);

        /// <summary>
        /// Retrieves training course records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the training course to retrieve.</param>
        /// <returns>A task whose result is an application result containing the matching training course record, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Get(int id);

        /// <summary>
        /// Creates training course records from the validated input.
        /// </summary>
        /// <param name="value">The training course data value to process.</param>
        /// <returns>A task whose result is an application result containing the saved training course record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Post(TrainingCourseDto value);

        /// <summary>
        /// Updates training course records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the training course to process.</param>
        /// <param name="value">The training course data value to process.</param>
        /// <returns>A task whose result is an application result containing the saved training course record or success status, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Put(int id, TrainingCourseDto value);

        /// <summary>
        /// Deletes the training course record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the training course to process.</param>
        /// <returns>A task whose result is an application result containing confirmation that the record was deleted, or validation, authorization, or conflict details on failure.</returns>
        Task<ServiceResult> Delete(int id);
    }
}
