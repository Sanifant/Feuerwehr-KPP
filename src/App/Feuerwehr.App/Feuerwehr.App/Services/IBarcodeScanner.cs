using System.Threading;
using System.Threading.Tasks;

namespace De.Openelp.Feuerwehr.App.Services;

public interface IBarcodeScanner
{
    bool IsSupported { get; }
    Task<string?> ScanAsync(CancellationToken cancellationToken);
}

public sealed class UnsupportedBarcodeScanner : IBarcodeScanner
{
    public bool IsSupported => false;
    /// <summary>
    /// Executes scan.
    /// </summary>
    public Task<string?> ScanAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}

