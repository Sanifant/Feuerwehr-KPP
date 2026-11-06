using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using De.Openelp.Feuerwehr.App.Services;

namespace De.Openelp.Feuerwehr.App.Android.Services
{
    /// <summary>
    /// Coordinates camera permission and the Android barcode-scanning activity.
    /// </summary>
    public sealed class AndroidBarcodeScanner : IBarcodeScanner
    {
        /// <summary>
        /// Gets a value indicating whether the current platform supports barcode scanning.
        /// </summary>
        public bool IsSupported => true;

        /// <summary>
        /// Scans an equipment barcode with the platform camera and loads the matching equipment.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the decoded barcode, or null if scanning is canceled or no value is obtained.</returns>
        public async Task<string?> ScanAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<string?> completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainActivity activity = MainActivity.CurrentActivity ?? throw new InvalidOperationException("Keine aktive Android-Ansicht.");
                BarcodeScannerActivity.Begin(activity, completion);
            });
            using CancellationTokenRegistration registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(() => BarcodeScannerActivity.Cancel(completion)));
            return await completion.Task;
        }
    }
}
