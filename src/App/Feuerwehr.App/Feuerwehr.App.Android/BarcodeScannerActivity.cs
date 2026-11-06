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
using Java.Nio;
using ZXing;
using CameraDevice = Android.Hardware.Camera2.CameraDevice;
using ImageFormatType = Android.Graphics.ImageFormatType;

namespace De.Openelp.Feuerwehr.App.Android
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
        private readonly BarcodeReaderGeneric decoder = new()
        {
            AutoRotate = true,
            Options = new()
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.QR_CODE]
            }
        };

        /// <summary>
        /// Executes on Request Permissions Result.
        /// </summary>
        /// <param name="requestCode">The request Code.</param>
        /// <param name="permissions">The permissions.</param>
        /// <param name="grantResults">The grant Results.</param>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode != 3050)
            {
                return;
            }

            this.permissionPending = false;
            if (grantResults.FirstOrDefault() == Permission.Granted && grantResults.Length > 0)
            {
                if (this.preview.IsAvailable)
                {
                    this.OpenCamera();
                }
                else
                {
                    this.Fail("Kamerazugriff wurde verweigert.");
                }
            }
        }

        /// <summary>
        /// Executes on Image Available.
        /// </summary>
        /// <param name="imageReader">The image Reader.</param>
        public void OnImageAvailable(ImageReader? imageReader)
        {
            if (this.closing)
            {
                return;
            }

            try
            {
                using Image? image = imageReader?.AcquireLatestImage();
                if (image is null || this.closing)
                {
                    return;
                }

                Image.Plane plane = image.GetPlanes()![0];
                ByteBuffer buffer = plane.Buffer!;
                byte[] raw = new byte[buffer.Remaining()];
                _ = buffer.Get(raw);
                byte[] luminance = new byte[image.Width * image.Height];
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        luminance[(y * image.Width) + x] = raw[(y * plane.RowStride) + (x * plane.PixelStride)];
                    }
                }

                PlanarYUVLuminanceSource source = new PlanarYUVLuminanceSource(luminance, image.Width, image.Height, 0, 0, image.Width, image.Height, false);
                ZXing.Result? result = this.decoder.Decode(source);
                if (result is null)
                {
                    return;
                }

                string code = result.Text.Trim().ToUpperInvariant();
                if (code.Length != 7 || !code.StartsWith("KPP-", StringComparison.Ordinal) || !int.TryParse(code.AsSpan(4), out int number) || number is < 1 or > 999)
                {
                    return;
                }

                this.RunOnUiThread(() =>
                {
                    _ = (pending?.TrySetResult(code));
                    this.Finish();
                });
            }
            catch (Exception)
            { /* Einzelne unlesbare Frames überspringen. */
            }
        }

        /// <summary>
        /// Executes on Surface Texture Available.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="width">The width.</param>
        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) => this.OpenCamera();

        /// <summary>
        /// Executes on Surface Texture Destroyed.
        /// </summary>
        /// <param name="surface">The surface.</param>
        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface) => true;

        /// <summary>
        /// Executes on Surface Texture Size Changed.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height) { }

        /// <summary>
        /// Executes on Surface Texture Updated.
        /// </summary>
        /// <param name="surface">The surface.</param>
        public void OnSurfaceTextureUpdated(SurfaceTexture surface) { }

        internal static void Begin(Activity owner, TaskCompletionSource<string?> completion)
        {
            if (pending is not null)
            {
                throw new InvalidOperationException("Ein Kamerascan läuft bereits.");
            }

            pending = completion;
            try
            {
                owner.StartActivity(new Intent(owner, typeof(BarcodeScannerActivity)));
            }
            catch
            {
                pending = null;
                throw;
            }
        }

        internal static void Cancel(TaskCompletionSource<string?> completion)
        {
            if (!ReferenceEquals(pending, completion))
            {
                return;
            }

            if (current is not null && current.TryGetTarget(out BarcodeScannerActivity? activity))
                activity.Finish();
            else
            {
                pending = null;
                _ = completion.TrySetResult(null);
            }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            TextView hint = null!;
            base.OnCreate(savedInstanceState);
            if (pending is null)
            {
                this.Finish();
                return;
            }

            current = new(this);
            LinearLayout layout = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
            hint = new TextView(this) { Text = "Barcode KPP-001 bis KPP-999 in die Kamera halten.", TextSize = 18 };
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

        protected override void OnPause()
        {
            if (this.permissionPending)
            {
                base.OnPause();
                return;
            }
            this.closing = true;
            this.session?.Close();
            this.camera?.Close();
            this.reader?.Close();
            this.previewSurface?.Dispose();
            this.session = null;
            this.camera = null;
            this.reader = null;
            _ = (this.thread?.QuitSafely());
            this.thread?.Join();
            this.handler?.Dispose();
            this.thread?.Dispose();
            this.handler = null;
            this.thread = null;
            base.OnPause();
            // Auch bei App-Wechsel wird die Kamera freigegeben und der Scan beendet.
            if (!this.IsFinishing)
            {
                this.Finish();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.closing = false;
            if (this.preview is not null && this.preview.IsAvailable && !this.permissionPending)
            {
                this.OpenCamera();
            }
        }

        protected override void OnDestroy()
        {
            _ = (pending?.TrySetResult(null));
            pending = null;
            current = null;
            base.OnDestroy();
        }

        private void OpenCamera()
        {
            if (this.camera is not null || this.thread is not null || this.closing || ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.Camera) != Permission.Granted)
            {
                return;
            }

            try
            {
                CameraManager manager = (CameraManager)this.GetSystemService(CameraService)!;
                string id = manager.GetCameraIdList().FirstOrDefault(id =>
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
            catch (Exception)
            {
                this.Fail("Die Kamera konnte nicht geöffnet werden.");
            }
        }

        private void StartPreview(CameraDevice device)
        {
            if (this.closing)
            {
                device.Close();
                return;
            }

            this.camera = device;
            this.preview.SurfaceTexture!.SetDefaultBufferSize(640, 480);
            this.previewSurface = new Surface(this.preview.SurfaceTexture);
            this.sessionState = new SessionState(this);
            // Der kompatible Camera2-Overload unterstützt auch die minimale Android-Version 24.
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                SessionConfiguration configuration = new SessionConfiguration(
                    (int)SessionType.Regular,
                    [new OutputConfiguration(this.previewSurface), new OutputConfiguration(this.reader!.Surface!)],
                    this.MainExecutor!,
                    this.sessionState);
                device.CreateCaptureSession(configuration);
            }
            else
            {
                device.CreateCaptureSession([this.previewSurface, this.reader!.Surface!], this.sessionState, this.handler);
            }
        }

        private void Fail(string message) => this.RunOnUiThread(() =>
        {
            _ = (pending?.TrySetException(new InvalidOperationException(message)));
            this.Finish();
        });

        private sealed class CameraState(BarcodeScannerActivity owner) : CameraDevice.StateCallback
        {
            /// <summary>
            /// Executes on Opened.
            /// </summary>
            public override void OnOpened(CameraDevice camera) => owner.RunOnUiThread(() => owner.StartPreview(camera));

            /// <summary>
            /// Executes on Disconnected.
            /// </summary>
            public override void OnDisconnected(CameraDevice camera)
            {
                camera.Close();
                owner.Fail("Kamera getrennt.");
            }

            /// <summary>
            /// Executes on Error.
            /// </summary>
            /// <param name="camera">The camera.</param>
            public override void OnError(CameraDevice camera, CameraError error)
            {
                camera.Close();
                owner.Fail("Kamerafehler.");
            }
        }

        private sealed class SessionState(BarcodeScannerActivity owner) : CameraCaptureSession.StateCallback
        {
            /// <summary>
            /// Executes on Configured.
            /// </summary>
            /// <param name="session">The session.</param>
            public override void OnConfigured(CameraCaptureSession session)
            {
                if (owner.closing)
                {
                    session.Close();
                    return;
                }
                try
                {
                    owner.session = session;
                    using CaptureRequest.Builder request = owner.camera!.CreateCaptureRequest(CameraTemplate.Preview);
                    request.AddTarget(owner.previewSurface!);
                    request.AddTarget(owner.reader!.Surface!);
                    request.Set(CaptureRequest.ControlAfMode!, (int)ControlAFMode.ContinuousPicture);
                    _ = session.SetRepeatingRequest(request.Build(), null, owner.handler);
                }
                catch (Exception)
                {
                    owner.Fail("Kameravorschau konnte nicht gestartet werden.");
                }
            }

            /// <summary>
            /// Executes on Configure Failed.
            /// </summary>
            public override void OnConfigureFailed(CameraCaptureSession session) => owner.Fail("Kameravorschau nicht verfügbar.");
        }
    }
}
