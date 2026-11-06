using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using de.openelp.feuerwehr.App.Services;

namespace de.openelp.feuerwehr.App.Android.Services
{
    public sealed class AndroidBarcodeScanner : IBarcodeScanner
    {
        public bool IsSupported => true;
        /// <summary>
        /// Executes scan.
        /// </summary>
        /// <param name="cancellationToken">The cancellation Token.</param>
        public async Task<string?> ScanAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<string?> completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
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
