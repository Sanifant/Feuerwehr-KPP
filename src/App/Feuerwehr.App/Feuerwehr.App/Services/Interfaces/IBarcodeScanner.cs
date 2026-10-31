using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services
{
    public interface IBarcodeScanner
    {
        bool IsSupported { get; }
        Task<string?> ScanAsync(CancellationToken cancellationToken);
    }
}
