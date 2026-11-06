using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;

namespace De.Openelp.Feuerwehr.App.Desktop.Services
{
    /// <summary>
    /// Desktop stub that returns a fixed location.
    /// </summary>
    public sealed class DesktopGpsService : IGpsService
    {
        /// <summary>
        /// Requests the device's current location using the platform provider and caller cancellation token.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the device location, or null if location is unavailable or permission is denied.</returns>
        public Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<GpsLocation?>(new GpsLocation
            {
                Latitude = 53.929134,
                Longitude = 10.638436
            });
        }
    }
}
