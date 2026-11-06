using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Reports barcode scanning as unavailable on platforms without a camera scanner implementation.
    /// </summary>
    public sealed class UnsupportedBarcodeScanner : IBarcodeScanner
    {
        /// <summary>
        /// Gets a value indicating whether the current platform supports barcode scanning.
        /// </summary>
        public bool IsSupported => false;

        /// <summary>
        /// Scans an equipment barcode with the platform camera and loads the matching equipment.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the decoded barcode, or null if scanning is canceled or no value is obtained.</returns>
        public Task<string?> ScanAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }
}
