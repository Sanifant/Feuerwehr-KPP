using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.App.Models;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Used until the host registers a platform location implementation.
    /// </summary>
    public sealed class UnsupportedGpsService : IGpsService
    {
        /// <summary>
        /// Requests the device's current location using the platform provider and caller cancellation token.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the device location, or null if location is unavailable or permission is denied.</returns>
        public Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<GpsLocation?>(null);
        }
    }
}
