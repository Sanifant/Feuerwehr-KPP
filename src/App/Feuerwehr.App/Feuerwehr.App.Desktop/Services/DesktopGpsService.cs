using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.App.Models;
using de.openelp.feuerwehr.App.Services;

namespace de.openelp.feuerwehr.App.Desktop.Services;

/// <summary>Desktop stub that returns a fixed location.</summary>
public sealed class DesktopGpsService : IGpsService
{
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
