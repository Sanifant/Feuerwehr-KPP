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

namespace De.Openelp.Feuerwehr.App.Android;

[Activity(Label = "GerÃ¤tebarcode scannen", Theme = "@android:style/Theme.Material.Light.NoActionBar", ScreenOrientation = ScreenOrientation.Portrait, Exported = false)]
public sealed class BarcodeScannerActivity : Activity, TextureView.ISurfaceTextureListener, ImageReader.IOnImageAvailableListener
{
    private static TaskCompletionSource<string?>? pending;
    private static WeakReference<BarcodeScannerActivity>? current;
    private TextureView preview = null!;
    private TextView hint = null!;
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
        if (pending is not null) throw new InvalidOperationException("Ein Kamerascan lÃ¤uft bereits.");
        pending = completion;
        try { owner.StartActivity(new Intent(owner, typeof(BarcodeScannerActivity))); }
        catch { pending = null; throw; }
    }
    internal static void Cancel(TaskCompletionSource<string?> completion)
    {
        if (!ReferenceEquals(pending, completion)) return;
        if (current is not null && current.TryGetTarget(out var activity)) activity.Finish();
        else { pending = null; completion.TrySetResult(null); }
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (pending is null) { Finish(); return; }
        current = new(this);
        var layout = new LinearLayout(this) { Orientation = global::Android.Widget.Orientation.Vertical };
        hint = new TextView(this) { Text = "Barcode KPP-001 bis KPP-999 in die Kamera halten.", TextSize = 18 };
        layout.AddView(hint);
        preview = new TextureView(this) { SurfaceTextureListener = this };
        layout.AddView(preview, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        var cancel = new Button(this) { Text = "Abbrechen" }; cancel.Click += (_, _) => Finish(); layout.AddView(cancel);
        SetContentView(layout);
        if (ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.Camera) != Permission.Granted)
        {
            permissionPending = true;
            RequestPermissions([global::Android.Manifest.Permission.Camera], 3050);
        }
    }

    /// <summary>
    /// Executes on Request Permissions Result.
    /// </summary>
    /// <param name="requestCode">The request Code.</param>
    /// <param name="permissions">The permissions.</param>
    /// <param name="grantResults">The grant Results.</param>
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode != 3050) return;
        permissionPending = false;
        if (grantResults.FirstOrDefault() == Permission.Granted && grantResults.Length > 0)
        { if (preview.IsAvailable) OpenCamera(); }
        else Fail("Kamerazugriff wurde verweigert.");
    }

    private void OpenCamera()
    {
        if (camera is not null || thread is not null || closing || ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.Camera) != Permission.Granted) return;
        try
        {
            var manager = (CameraManager)GetSystemService(CameraService)!;
            var id = manager.GetCameraIdList().FirstOrDefault(id =>
                (manager.GetCameraCharacteristics(id).Get(CameraCharacteristics.LensFacing) as Java.Lang.Integer)?.IntValue() == (int)LensFacing.Back)
                ?? manager.GetCameraIdList().FirstOrDefault() ?? throw new InvalidOperationException("Keine Kamera verfÃ¼gbar.");
            thread = new HandlerThread("InventoryBarcode"); thread.Start(); handler = new Handler(thread.Looper!);
            reader = ImageReader.NewInstance(640, 480, ImageFormatType.Yuv420888, 2);
            reader.SetOnImageAvailableListener(this, handler);
            cameraState = new CameraState(this);
            manager.OpenCamera(id, cameraState, handler);
        }
        catch (Exception) { Fail("Die Kamera konnte nicht geÃ¶ffnet werden."); }
    }

    private void StartPreview(CameraDevice device)
    {
        if (closing) { device.Close(); return; }
        camera = device;
        preview.SurfaceTexture!.SetDefaultBufferSize(640, 480);
        previewSurface = new Surface(preview.SurfaceTexture);
        sessionState = new SessionState(this);
        // Der kompatible Camera2-Overload unterstÃ¼tzt auch die minimale Android-Version 24.
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            var configuration = new SessionConfiguration((int)SessionType.Regular,
                [new OutputConfiguration(previewSurface), new OutputConfiguration(reader!.Surface!)], MainExecutor!, sessionState);
            device.CreateCaptureSession(configuration);
        }
        else
        {
            device.CreateCaptureSession([previewSurface, reader!.Surface!], sessionState, handler);
        }
    }

    /// <summary>
    /// Executes on Image Available.
    /// </summary>
    /// <param name="imageReader">The image Reader.</param>
    public void OnImageAvailable(ImageReader? imageReader)
    {
        if (closing) return;
        try
        {
            using var image = imageReader?.AcquireLatestImage();
            if (image is null || closing) return;
            var plane = image.GetPlanes()![0];
            var buffer = plane.Buffer!;
            var raw = new byte[buffer.Remaining()]; buffer.Get(raw);
            var luminance = new byte[image.Width * image.Height];
            for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++) luminance[y * image.Width + x] = raw[y * plane.RowStride + x * plane.PixelStride];
            var source = new PlanarYUVLuminanceSource(luminance, image.Width, image.Height, 0, 0, image.Width, image.Height, false);
            var result = decoder.Decode(source);
            if (result is null) return;
            var code = result.Text.Trim().ToUpperInvariant();
            if (code.Length != 7 || !code.StartsWith("KPP-", StringComparison.Ordinal) || !int.TryParse(code.AsSpan(4), out var number) || number is < 1 or > 999) return;
            RunOnUiThread(() => { pending?.TrySetResult(code); Finish(); });
        }
        catch (Exception) { /* Einzelne unlesbare Frames Ã¼berspringen. */ }
    }

    private void Fail(string message) => RunOnUiThread(() => { pending?.TrySetException(new InvalidOperationException(message)); Finish(); });
    protected override void OnPause()
    {
        if (permissionPending) { base.OnPause(); return; }
        closing = true;
        session?.Close(); camera?.Close(); reader?.Close(); previewSurface?.Dispose();
        session = null; camera = null; reader = null;
        thread?.QuitSafely(); thread?.Join(); handler?.Dispose(); thread?.Dispose(); handler = null; thread = null;
        base.OnPause();
        // Auch bei App-Wechsel wird die Kamera freigegeben und der Scan beendet.
        if (!IsFinishing) Finish();
    }
    protected override void OnResume()
    {
        base.OnResume();
        closing = false;
        if (preview is not null && preview.IsAvailable && !permissionPending) OpenCamera();
    }
    protected override void OnDestroy()
    {
        pending?.TrySetResult(null); pending = null; current = null;
        base.OnDestroy();
    }
    /// <summary>
    /// Executes on Surface Texture Available.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <param name="width">The width.</param>
    public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height) => OpenCamera();
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

    private sealed class CameraState(BarcodeScannerActivity owner) : CameraDevice.StateCallback
    {
        /// <summary>
        /// Executes on Opened.
        /// </summary>
        public override void OnOpened(CameraDevice camera) => owner.RunOnUiThread(() => owner.StartPreview(camera));
        /// <summary>
        /// Executes on Disconnected.
        /// </summary>
        public override void OnDisconnected(CameraDevice camera) { camera.Close(); owner.Fail("Kamera getrennt."); }
        /// <summary>
        /// Executes on Error.
        /// </summary>
        /// <param name="camera">The camera.</param>
        public override void OnError(CameraDevice camera, CameraError error) { camera.Close(); owner.Fail("Kamerafehler."); }
    }
    private sealed class SessionState(BarcodeScannerActivity owner) : CameraCaptureSession.StateCallback
    {
        /// <summary>
        /// Executes on Configured.
        /// </summary>
        /// <param name="session">The session.</param>
        public override void OnConfigured(CameraCaptureSession session)
        {
            if (owner.closing) { session.Close(); return; }
            try
            {
                owner.session = session;
                using var request = owner.camera!.CreateCaptureRequest(CameraTemplate.Preview);
                request.AddTarget(owner.previewSurface!); request.AddTarget(owner.reader!.Surface!);
                request.Set(CaptureRequest.ControlAfMode!, (int)ControlAFMode.ContinuousPicture);
                session.SetRepeatingRequest(request.Build(), null, owner.handler);
            }
            catch (Exception) { owner.Fail("Kameravorschau konnte nicht gestartet werden."); }
        }
        /// <summary>
        /// Executes on Configure Failed.
        /// </summary>
        public override void OnConfigureFailed(CameraCaptureSession session) => owner.Fail("Kameravorschau nicht verfÃ¼gbar.");
    }
}

