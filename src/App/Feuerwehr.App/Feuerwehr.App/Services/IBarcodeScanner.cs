using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    public sealed class UnsupportedBarcodeScanner : IBarcodeScanner
    {
        public bool IsSupported => false;

        /// <summary>
        /// Executes scan.
        /// </summary>
        /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
        public Task<string?> ScanAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }
}
