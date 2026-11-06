using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the barcode scanner operations required by application consumers.
    /// </summary>
    public interface IBarcodeScanner
    {
        /// <summary>
        /// Gets a value indicating whether the current platform supports barcode scanning.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Scans an equipment barcode with the platform camera and loads the matching equipment.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the decoded barcode, or null if scanning is canceled or no value is obtained.</returns>
        Task<string?> ScanAsync(CancellationToken cancellationToken);
    }
}
