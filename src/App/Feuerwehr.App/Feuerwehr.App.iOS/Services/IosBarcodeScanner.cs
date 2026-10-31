using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using De.Openelp.Feuerwehr.App.Services;
using UIKit;

namespace De.Openelp.Feuerwehr.App.iOS.Services;

public sealed class IosBarcodeScanner : IBarcodeScanner
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public bool IsSupported => true;

    /// <summary>
    /// Executes scan.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<string?> ScanAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video))
                throw new InvalidOperationException("Kamerazugriff wurde verweigert.");
            cancellationToken.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var controller = new BarcodeViewController(completion) { ModalPresentationStyle = UIModalPresentationStyle.FullScreen };
            try
            {
                await InvokeAsync(() =>
                {
                    var presenter = UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>()
                        .SelectMany(x => x.Windows).FirstOrDefault(x => x.IsKeyWindow)?.RootViewController
                        ?? throw new InvalidOperationException("Keine aktive Ansicht.");
                    while (presenter.PresentedViewController is { } presented) presenter = presented;
                    presenter.PresentViewController(controller, true, null);
                });
                using var registration = cancellationToken.Register(() => UIApplication.SharedApplication.BeginInvokeOnMainThread(() => controller.Complete(null)));
                return await completion.Task;
            }
            finally { await InvokeAsync(controller.Dispose); }
        }
        finally { gate.Release(); }
    }

    private static Task InvokeAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
        {
            try { action(); completion.SetResult(); } catch (Exception ex) { completion.SetException(ex); }
        });
        return completion.Task;
    }

    private sealed class BarcodeViewController(TaskCompletionSource<string?> completion) : UIViewController
    {
        private readonly AVCaptureSession session = new();
        private readonly DispatchQueue sessionQueue = new("De.Openelp.Feuerwehr.inventory.camera");
        private AVCaptureVideoPreviewLayer? preview;
        private MetadataDelegate? metadataDelegate;
        private bool completed;

        /// <summary>
        /// Executes view Did Load.
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View!.BackgroundColor = UIColor.Black;
            try
            {
                var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video)
                    ?? throw new InvalidOperationException("Keine Kamera verfügbar.");
                var input = AVCaptureDeviceInput.FromDevice(device, out var error);
                if (input is null) throw new InvalidOperationException(error?.LocalizedDescription ?? "Kamera nicht verfügbar.");
                if (!session.CanAddInput(input)) throw new InvalidOperationException("Kameraeingang nicht verfügbar.");
                session.AddInput(input);
                var output = new AVCaptureMetadataOutput();
                if (!session.CanAddOutput(output)) throw new InvalidOperationException("Barcodescan nicht verfügbar.");
                session.AddOutput(output);
                metadataDelegate = new MetadataDelegate(this);
                output.SetDelegate(metadataDelegate, DispatchQueue.MainQueue);
                output.MetadataObjectTypes = (AVMetadataObjectType.Code128 | AVMetadataObjectType.Code39 | AVMetadataObjectType.QRCode) & output.AvailableMetadataObjectTypes;
                preview = new AVCaptureVideoPreviewLayer(session) { VideoGravity = AVLayerVideoGravity.ResizeAspectFill, Frame = View.Bounds };
                View.Layer.AddSublayer(preview);
                var cancel = UIButton.FromType(UIButtonType.System);
                cancel.SetTitle("Abbrechen", UIControlState.Normal); cancel.BackgroundColor = UIColor.White;
                cancel.Frame = new CGRect(20, 60, 160, 48); cancel.TouchUpInside += (_, _) => Complete(null); View.AddSubview(cancel);
                sessionQueue.DispatchAsync(() => { if (!completed) session.StartRunning(); });
            }
            catch (Exception ex) { Complete(null, ex); }
        }
        /// <summary>
        /// Executes view Did Layout Subviews.
        /// </summary>
        public override void ViewDidLayoutSubviews()
        { base.ViewDidLayoutSubviews(); if (preview is not null) preview.Frame = View!.Bounds; }
        /// <summary>
        /// Executes complete.
        /// </summary>
        /// <param name="barcode">The barcode.</param>
        /// <param name="error">The error.</param>
        public void Complete(string? barcode, Exception? error = null)
        {
            if (completed) return;
            completed = true;
            sessionQueue.DispatchAsync(() => session.StopRunning());
            DismissViewController(true, () => { if (error is null) completion.TrySetResult(barcode); else completion.TrySetException(error); });
        }
        /// <summary>
        /// Executes view Did Disappear.
        /// </summary>
        /// <param name="animated">The animated.</param>
        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            if (!completed) Complete(null);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sessionQueue.DispatchSync(() => session.StopRunning());
                preview?.Dispose(); metadataDelegate?.Dispose(); session.Dispose(); sessionQueue.Dispose();
            }
            base.Dispose(disposing);
        }
        private sealed class MetadataDelegate(BarcodeViewController owner) : AVCaptureMetadataOutputObjectsDelegate
        {
            /// <summary>
            /// Executes did Output Metadata Objects.
            /// </summary>
            /// <param name="captureOutput">The capture Output.</param>
            /// <param name="metadataObjects">The metadata Objects.</param>
            /// <param name="connection">The connection.</param>
            public override void DidOutputMetadataObjects(AVCaptureMetadataOutput captureOutput, AVMetadataObject[] metadataObjects, AVCaptureConnection connection)
            {
                foreach (var value in metadataObjects.OfType<AVMetadataMachineReadableCodeObject>())
                {
                    var code = value.StringValue?.Trim().ToUpperInvariant();
                    if (code?.Length == 7 && code.StartsWith("KPP-", StringComparison.Ordinal) && int.TryParse(code.AsSpan(4), out var number) && number is >= 1 and <= 999)
                    { owner.Complete(code); return; }
                }
            }
        }
    }
}

