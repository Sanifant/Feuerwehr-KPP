using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.App.Models;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the GPS operations required by application consumers.
    /// </summary>
    public interface IGpsService
    {
        /// <summary>
        /// Requests the device's current location using the platform provider and caller cancellation token.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the device location, or null if location is unavailable or permission is denied.</returns>
        Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
    }
}
