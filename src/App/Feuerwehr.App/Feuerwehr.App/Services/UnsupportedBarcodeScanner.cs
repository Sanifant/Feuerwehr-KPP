using System.Threading;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.App.Services
{
    public sealed class UnsupportedBarcodeScanner : IBarcodeScanner
    {
        public bool IsSupported => false;
        /// <summary>
        /// Executes scan.
        /// </summary>
        public Task<string?> ScanAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
