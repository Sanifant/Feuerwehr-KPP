using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.App.Models;

namespace de.openelp.feuerwehr.App.Services;

public interface IGpsService
{
    /// <summary>
    /// Gets a current location, requesting foreground permission when necessary.
    /// Returns null if permission is denied, location is disabled, the platform is
    /// unsupported, or no fix is available within the platform's timeout.
    /// Caller cancellation throws OperationCanceledException.
    /// </summary>
    Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}
