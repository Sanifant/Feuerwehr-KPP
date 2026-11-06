using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using De.Openelp.Feuerwehr.App.Services;
using UIKit;

namespace De.Openelp.Feuerwehr.App.iOS.Services
{
    /// <summary>
    /// Presents the iOS camera scanner and returns the decoded equipment barcode.
    /// </summary>
    public sealed class IosBarcodeScanner : IBarcodeScanner
    {
        private readonly SemaphoreSlim gate = new(1, 1);

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
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (!await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video))
                    throw new InvalidOperationException("Kamerazugriff wurde verweigert.");
                cancellationToken.ThrowIfCancellationRequested();
                TaskCompletionSource<string?> completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                BarcodeViewController controller = new BarcodeViewController(completion) { ModalPresentationStyle = UIModalPresentationStyle.FullScreen };
                try
                {
                    await InvokeAsync(() =>
                    {
                        UIViewController presenter = UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>()
                            .SelectMany(x => x.Windows).FirstOrDefault(x => x.IsKeyWindow)?.RootViewController
                            ?? throw new InvalidOperationException("Keine aktive Ansicht.");
                        while (presenter.PresentedViewController is { } presented) presenter = presented;
                        presenter.PresentViewController(controller, true, null);
                    });
                    using CancellationTokenRegistration registration = cancellationToken.Register(() => UIApplication.SharedApplication.BeginInvokeOnMainThread(() => controller.Complete(null)));
                    return await completion.Task;
                }
                finally { await InvokeAsync(controller.Dispose); }
            }
            finally { gate.Release(); }
        }

        private static Task InvokeAsync(Action action)
        {
            TaskCompletionSource completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                try { action(); completion.SetResult(); } catch (Exception ex) { completion.SetException(ex); }
            });
            return completion.Task;
        }

        /// <summary>
        /// Exposes HTTP endpoints for barcode view operations and maps application results to responses.
        /// </summary>
        /// <param name="completion">The completion source receiving the decoded barcode or cancellation result.</param>
        private sealed class BarcodeViewController(TaskCompletionSource<string?> completion) : UIViewController
        {
            private readonly AVCaptureSession session = new();
            private readonly DispatchQueue sessionQueue = new("De.Openelp.Feuerwehr.inventory.camera");
            private AVCaptureVideoPreviewLayer? preview;
            private MetadataDelegate? metadataDelegate;
            private bool completed;

            /// <summary>
            /// Creates the iOS scanner preview and starts camera capture.
            /// </summary>
            public override void ViewDidLoad()
            {
                base.ViewDidLoad();
                View!.BackgroundColor = UIColor.Black;
                try
                {
                    AVCaptureDevice device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video)
                        ?? throw new InvalidOperationException("Keine Kamera verfügbar.");
                    AVCaptureDeviceInput? input = AVCaptureDeviceInput.FromDevice(device, out NSError? error);
                    if (input is null) throw new InvalidOperationException(error?.LocalizedDescription ?? "Kamera nicht verfügbar.");
                    if (!session.CanAddInput(input)) throw new InvalidOperationException("Kameraeingang nicht verfügbar.");
                    session.AddInput(input);
                    AVCaptureMetadataOutput output = new AVCaptureMetadataOutput();
                    if (!session.CanAddOutput(output)) throw new InvalidOperationException("Barcodescan nicht verfügbar.");
                    session.AddOutput(output);
                    metadataDelegate = new MetadataDelegate(this);
                    output.SetDelegate(metadataDelegate, DispatchQueue.MainQueue);
                    output.MetadataObjectTypes = (AVMetadataObjectType.Code128 | AVMetadataObjectType.Code39 | AVMetadataObjectType.QRCode) & output.AvailableMetadataObjectTypes;
                    preview = new AVCaptureVideoPreviewLayer(session) { VideoGravity = AVLayerVideoGravity.ResizeAspectFill, Frame = View.Bounds };
                    View.Layer.AddSublayer(preview);
                    UIButton cancel = UIButton.FromType(UIButtonType.System);
                    cancel.SetTitle("Abbrechen", UIControlState.Normal); cancel.BackgroundColor = UIColor.White;
                    cancel.Frame = new CGRect(20, 60, 160, 48); cancel.TouchUpInside += (_, _) => Complete(null); View.AddSubview(cancel);
                    sessionQueue.DispatchAsync(() => { if (!completed) session.StartRunning(); });
                }
                catch (Exception ex) { Complete(null, ex); }
            }

            /// <summary>
            /// Resizes the camera preview to match the scanner view bounds.
            /// </summary>
            public override void ViewDidLayoutSubviews()
            { base.ViewDidLayoutSubviews(); if (preview is not null) preview.Frame = View!.Bounds; }

            /// <summary>
            /// Completes the pending barcode scan and releases camera resources.
            /// </summary>
            /// <param name="barcode">The unique equipment barcode used for lookup and import matching.</param>
            /// <param name="error">The platform camera failure code.</param>
            public void Complete(string? barcode, Exception? error = null)
            {
                if (completed) return;
                completed = true;
                sessionQueue.DispatchAsync(() => session.StopRunning());
                DismissViewController(true, () => { if (error is null) completion.TrySetResult(barcode); else completion.TrySetException(error); });
            }

            /// <summary>
            /// Stops camera capture after the scanner view disappears.
            /// </summary>
            /// <param name="animated">Whether the platform animates the view transition.</param>
            public override void ViewDidDisappear(bool animated)
            {
                base.ViewDidDisappear(animated);
                if (!completed) Complete(null);
            }

            /// <summary>
            /// Releases the resources owned by this barcode view component.
            /// </summary>
            /// <param name="disposing">Whether managed resources should also be released.</param>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    sessionQueue.DispatchSync(() => session.StopRunning());
                    preview?.Dispose(); metadataDelegate?.Dispose(); session.Dispose(); sessionQueue.Dispose();
                }
                base.Dispose(disposing);
            }

            /// <summary>
            /// Forwards decoded iOS camera metadata to the pending barcode scan.
            /// </summary>
            /// <param name="owner">The activity or controller owning the scanner operation.</param>
            private sealed class MetadataDelegate(BarcodeViewController owner) : AVCaptureMetadataOutputObjectsDelegate
            {
                /// <summary>
                /// Completes the iOS scan when camera metadata contains a supported barcode.
                /// </summary>
                /// <param name="captureOutput">The capture output producing barcode metadata.</param>
                /// <param name="metadataObjects">The metadata objects decoded from the camera capture.</param>
                /// <param name="connection">The camera connection associated with the decoded metadata.</param>
                public override void DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
                {
                    foreach (AVMetadataMachineReadableCodeObject value in metadataObjects.OfType<AVMetadataMachineReadableCodeObject>())
                    {
                        string? code = value.StringValue?.Trim().ToUpperInvariant();
                        if (code?.Length == 7 && code.StartsWith("KPP-", StringComparison.Ordinal) && int.TryParse(code.AsSpan(4), out int number) && number is >= 1 and <= 999)
                        { owner.Complete(code); return; }
                    }
                }
            }
        }
    }
}
