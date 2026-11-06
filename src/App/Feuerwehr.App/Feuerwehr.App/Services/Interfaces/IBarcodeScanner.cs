using System.Threading;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.App.Services
{
    public interface IBarcodeScanner
    {
        bool IsSupported { get; }
        Task<string?> ScanAsync(CancellationToken cancellationToken);
    }
}
