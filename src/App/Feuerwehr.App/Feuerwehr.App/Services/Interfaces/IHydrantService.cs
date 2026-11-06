using System.Collections.Generic;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the hydrant operations required by application consumers.
    /// </summary>
    public interface IHydrantService
    {
        /// <summary>
        /// Creates hydrant records from the validated input.
        /// </summary>
        /// <param name="hydrant">The hydrant data to create or update.</param>
        /// <returns>A task whose result is the hydrant returned by the API.</returns>
        Task<Hydrant> CreateAsync(Hydrant hydrant);

        /// <summary>
        /// Deletes the hydrant record matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <returns>A task whose result is true if the requested hydrant operation succeeds; otherwise, false.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Retrieves hydrant records visible to the caller.
        /// </summary>
        /// <returns>A task whose result is the hydrants returned by the API.</returns>
        Task<IEnumerable<Hydrant>> GetAllAsync();

        /// <summary>
        /// Retrieves hydrant records matching the supplied identifier.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to retrieve.</param>
        /// <returns>A task whose result is the hydrant returned by the API.</returns>
        Task<Hydrant> GetByIdAsync(int id);

        /// <summary>
        /// Updates hydrant records matching the supplied identifier after validating the input.
        /// </summary>
        /// <param name="id">The identifier of the hydrant to process.</param>
        /// <param name="hydrant">The hydrant data to create or update.</param>
        /// <returns>A task whose result is true if the requested hydrant operation succeeds; otherwise, false.</returns>
        Task<bool> UpdateAsync(int id, Hydrant hydrant);
    }
}
