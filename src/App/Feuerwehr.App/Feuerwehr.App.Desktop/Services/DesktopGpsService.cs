using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;

namespace De.Openelp.Feuerwehr.App.Desktop.Services;

/// <summary>Desktop stub that returns a fixed location.</summary>
public sealed class DesktopGpsService : IGpsService
{
    /// <summary>
    /// Gets the current Location.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
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

