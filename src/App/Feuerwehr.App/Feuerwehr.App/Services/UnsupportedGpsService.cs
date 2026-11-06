using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.App.Models;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>Used until the host registers a platform location implementation.</summary>
    public sealed class UnsupportedGpsService : IGpsService
    {
        /// <summary>
        /// Gets the current Location.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<GpsLocation?>(null);
        }
    }

}
