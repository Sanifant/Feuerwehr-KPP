using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using ZXing;
using CameraDevice = Android.Hardware.Camera2.CameraDevice;
using ImageFormatType = Android.Graphics.ImageFormatType;

namespace de.openelp.feuerwehr.App.Android
{
    [Activity(Label = "Gerätebarcode scannen", Theme = "@android:style/Theme.Material.Light.NoActionBar", ScreenOrientation = ScreenOrientation.Portrait, Exported = false)]
    public sealed class BarcodeScannerActivity : Activity, TextureView.ISurfaceTextureListener, ImageReader.IOnImageAvailableListener
    {
        private static TaskCompletionSource<string?>? pending;
        private static WeakReference<BarcodeScannerActivity>? current;
        private TextureView preview = null!;
        private CameraDevice? camera;
        private CameraCaptureSession? session;
        private ImageReader? reader;
        private Surface? previewSurface;
        private HandlerThread? thread;
        private Handler? handler;
        private CameraState? cameraState;
        private SessionState? sessionState;
        private bool closing;
        private bool permissionPending;
        private readonly BarcodeReaderGeneric decoder = new() { AutoRotate = true, Options = new() { TryHarder = true, PossibleFormats = [BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.QR_CODE] } };

        internal static void Begin(Activity owner, TaskCompletionSource<string?> completion)
        {
            if (pending is not null)
                throw new InvalidOperationException("Ein Kamerascan läuft bereits.");
            pending = completion;
            try
            { owner.StartActivity(new Intent(owner, typeof(BarcodeScannerActivity))); }
            catch { pending = null; throw; }
        }
        internal static void Cancel(TaskCompletionSource<string?> completion)
        {
            if (!ReferenceEquals(pending, completion))
                return;
            if (current is not null && current.TryGetTarget(out var activity))
                activity.Finish();
            else
            { pending = null; completion.TrySetResult(null); }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            if (pending is null)
            { this.Finish(); return; }
            current = new(this);
            LinearLayout layout = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
            TextView hint = new TextView(this) { Text = "Barcode KPP-001 bis KPP-999 in die Kamera halten.", TextSize = 18 };
            layout.AddView(hint);
            this.preview = new TextureView(this) { SurfaceTextureListener = this };
            layout.AddView(this.preview, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
            Button cancel = new Button(this) { Text = "Abbrechen" };
            cancel.Click += (_, _) => this.Finish();
            layout.AddView(cancel);
            this.SetContentView(layout);
            if (ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                this.permissionPending = true;
                this.RequestPermissions([global::Android.Manifest.Permission.Camera], 3050);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode != 3050)
                return;
            this.permissionPending = false;
            if (grantResults.FirstOrDefault() == Permission.Granted && grantResults.Length > 0)
            { if (this.preview.IsAvailable) this.OpenCamera(); }
            else
                this.Fail("Kamerazugriff wurde verweigert.");
        }

        private void OpenCamera()
        {
            if (this.camera is not null || this.thread is not null || this.closing || ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.Camera) != Permission.Granted)
                return;
            try
            {
                CameraManager manager = (CameraManager)this.GetSystemService(CameraService)!;
                var id = manager.GetCameraIdList().FirstOrDefault(id =>
                    (manager.GetCameraCharacteristics(id).Get(CameraCharacteristics.LensFacing) as Java.Lang.Integer)?.IntValue() == (int)LensFacing.Back)
                    ?? manager.GetCameraIdList().FirstOrDefault() ?? throw new InvalidOperationException("Keine Kamera verfügbar.");
                this.thread = new HandlerThread("InventoryBarcode");
                this.thread.Start();
                this.handler = new Handler(this.thread.Looper!);
                this.reader = ImageReader.NewInstance(640, 480, ImageFormatType.Yuv420888, 2);
                this.reader.SetOnImageAvailableListener(this, this.handler);
                this.cameraState = new CameraState(this);
                manager.OpenCamera(id, this.cameraState, this.handler);
            }
            catch (Exception) { this.Fail("Die Kamera konnte nicht geöffnet werden."); }
        }

        private void StartPreview(CameraDevice device)
        {
            if (this.closing)
            { device.Close(); return; }
            this.camera = device;
            this.preview.SurfaceTexture!.SetDefaultBufferSize(640, 480);
            this.previewSurface = new Surface(this.preview.SurfaceTexture);
            this.sessionState = new SessionState(this);
            // Der kompatible Camera2-Overload unterstützt auch die minimale Android-Version 24.
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                SessionConfiguration configuration = new SessionConfiguration((int)SessionType.Regular,
                    [new OutputConfiguration(this.previewSurface), new OutputConfiguration(this.reader!.Surface!)], this.MainExecutor!, this.sessionState);
                device.CreateCaptureSession(configuration);
            }
            else
            {
                device.CreateCaptureSession([this.previewSurface, this.reader!.Surface!], this.sessionState, this.handler);
            }
        }

        public void OnImageAvailable(ImageReader? imageReader)
        {
            if (this.closing)
                return;
            try
            {
                using var image = imageReader?.AcquireLatestImage();
                if (image is null || this.closing)
                    return;
                var plane = image.GetPlanes()![0];
                var buffer = plane.Buffer!;
                var raw = new byte[buffer.Remaining()];
                buffer.Get(raw);
                var luminance = new byte[image.Width * image.Height];
                for (var y = 0; y < image.Height; y++)
                    for (var x = 0; x < image.Width; x++)
                        luminance[y * image.Width + x] = raw[y * plane.RowStride + x * plane.PixelStride];
                PlanarYUVLuminanceSource source = new PlanarYUVLuminanceSource(luminance, image.Width, image.Height, 0, 0, image.Width, image.Height, false);
                var result = this.decoder.Decode(source);
                if (result is null)
                    return;
                var code = result.Text.Trim().ToUpperInvariant();
                if (code.Length != 7 || !code.StartsWith("KPP-", StringComparison.Ordinal) || !int.TryParse(code.AsSpan(4), out var number) || number is < 1 or > 999)
                    return;
                this.RunOnUiThread(() => { pending?.TrySetResult(code); this.Finish(); });
            }
            catch (Exception) { /* Einzelne unlesbare Frames überspringen. */ }
        }

        private void Fail(string message)
        {
            this.RunOnUiThread(() => { pending?.TrySetException(new InvalidOperationException(message)); this.Finish(); });
        }

        protected override void OnPause()
        {
            if (this.permissionPending)
            { base.OnPause(); return; }
            this.closing = true;
            this.session?.Close();
            this.camera?.Close();
            this.reader?.Close();
            this.previewSurface?.Dispose();
            this.session = null;
            this.camera = null;
            this.reader = null;
            this.thread?.QuitSafely();
            this.thread?.Join();
            this.handler?.Dispose();
            this.thread?.Dispose();
            this.handler = null;
            this.thread = null;
            base.OnPause();
            // Auch bei App-Wechsel wird die Kamera freigegeben und der Scan beendet.
            if (!this.IsFinishing)
                this.Finish();
        }
        protected override void OnResume()
        {
            base.OnResume();
            this.closing = false;
            if (this.preview is not null && this.preview.IsAvailable && !this.permissionPending)
                this.OpenCamera();
        }
        protected override void OnDestroy()
        {
            pending?.TrySetResult(null);
            pending = null;
            current = null;
            base.OnDestroy();
        }
        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            this.OpenCamera();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height) { }
        public void OnSurfaceTextureUpdated(SurfaceTexture surface) { }

        private sealed class CameraState(BarcodeScannerActivity owner) : CameraDevice.StateCallback
        {
            public override void OnOpened(CameraDevice camera)
            {
                owner.RunOnUiThread(() => owner.StartPreview(camera));
            }

            public override void OnDisconnected(CameraDevice camera) { camera.Close(); owner.Fail("Kamera getrennt."); }
            public override void OnError(CameraDevice camera, CameraError error) { camera.Close(); owner.Fail("Kamerafehler."); }
        }
        private sealed class SessionState(BarcodeScannerActivity owner) : CameraCaptureSession.StateCallback
        {
            public override void OnConfigured(CameraCaptureSession session)
            {
                if (owner.closing)
                { session.Close(); return; }
                try
                {
                    owner.session = session;
                    using var request = owner.camera!.CreateCaptureRequest(CameraTemplate.Preview);
                    request.AddTarget(owner.previewSurface!);
                    request.AddTarget(owner.reader!.Surface!);
                    request.Set(CaptureRequest.ControlAfMode!, (int)ControlAFMode.ContinuousPicture);
                    session.SetRepeatingRequest(request.Build(), null, owner.handler);
                }
                catch (Exception) { owner.Fail("Kameravorschau konnte nicht gestartet werden."); }
            }
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                owner.Fail("Kameravorschau nicht verfügbar.");
            }
        }
    }
}
