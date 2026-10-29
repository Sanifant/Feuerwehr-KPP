using System.Threading;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.App.Services;

public interface IBarcodeScanner
{
    bool IsSupported { get; }
    Task<string?> ScanAsync(CancellationToken cancellationToken);
}

public sealed class UnsupportedBarcodeScanner : IBarcodeScanner
{
    public bool IsSupported => false;
    public Task<string?> ScanAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
