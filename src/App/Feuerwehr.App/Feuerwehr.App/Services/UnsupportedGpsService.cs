using System.Threading;
using System.Threading.Tasks;
using de.openelp.feuerwehr.App.Models;

namespace de.openelp.feuerwehr.App.Services;

/// <summary>Used until the host registers a platform location implementation.</summary>
public sealed class UnsupportedGpsService : IGpsService
{
    public Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<GpsLocation?>(null);
    }
}
