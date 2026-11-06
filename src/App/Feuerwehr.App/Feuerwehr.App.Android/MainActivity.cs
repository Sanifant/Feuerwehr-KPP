using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
using Avalonia.Android;
using Manifest = global::Android.Manifest;

namespace De.Openelp.Feuerwehr.App.Android
{
    /// <summary>
    /// Hosts the Android Avalonia application and coordinates runtime location permissions.
    /// </summary>
    [Activity(
        Label = "De.Openelp.Feuerwehr.App.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        private const int LocationPermissionRequestCode = 4101;
        private static WeakReference<MainActivity>? current;
        private readonly CancellationTokenSource destroyed = new ();
        private TaskCompletionSource<bool>? locationPermissionRequest;

        /// <summary>
        /// Gets the currently active Android application activity.
        /// </summary>
        internal static MainActivity? CurrentActivity =>
            current is not null && current.TryGetTarget(out MainActivity? activity)
                && !activity.IsFinishing && !activity.IsDestroyed ? activity : null;

        /// <summary>
        /// Gets the cancellation token signaled when the Android activity is destroyed.
        /// </summary>
        internal CancellationToken DestroyedToken => this.destroyed.Token;

        /// <summary>
        /// Initializes the Android activity and its platform services from the saved state.
        /// </summary>
        /// <param name="savedInstanceState">The saved Android activity state, when restoring an existing activity.</param>
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            current = new WeakReference<MainActivity>(this);
            base.OnCreate(savedInstanceState);
        }

        /// <summary>
        /// Gets a value indicating whether Android has granted runtime access to device location.
        /// </summary>
        internal bool HasLocationPermission =>
            ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted
            || ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted;

        /// <summary>
        /// Completes the pending platform permission request using the returned grant results.
        /// </summary>
        /// <param name="requestCode">The identifier of the completed Android permission request.</param>
        /// <param name="permissions">The Android permissions included in the completed permission request.</param>
        /// <param name="grantResults">The platform grant or denial result for each requested permission.</param>
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == LocationPermissionRequestCode)
            {
                TaskCompletionSource<bool>? request = this.locationPermissionRequest;
                this.locationPermissionRequest = null;
                _ = (request?.TrySetResult(this.HasLocationPermission));
            }
        }

        // Called on the UI thread; concurrent callers share the same permission dialog.
        /// <summary>
        /// Requests Android location permission when it has not already been granted.
        /// </summary>
        /// <returns>A task whose result is true if location access is granted; otherwise, false.</returns>
        internal Task<bool> EnsureLocationPermissionAsync()
        {
            if (this.HasLocationPermission)
            {
                return Task.FromResult(true);
            }

            if (this.locationPermissionRequest is not null)
            {
                return this.locationPermissionRequest.Task;
            }

            TaskCompletionSource<bool> request = new (TaskCreationOptions.RunContinuationsAsynchronously);
            this.locationPermissionRequest = request;
            this.RequestPermissions(
                new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation },
                LocationPermissionRequestCode);
            return request.Task;
        }

        /// <summary>
        /// Cancels pending platform operations and releases activity resources.
        /// </summary>
        protected override void OnDestroy()
        {
            if (current is not null && current.TryGetTarget(out MainActivity? activity) && ReferenceEquals(activity, this))
            {
                current = null;
            }

            _ = (this.locationPermissionRequest?.TrySetResult(false));
            this.locationPermissionRequest = null;
            this.destroyed.Cancel();
            base.OnDestroy();
            this.destroyed.Dispose();
        }
    }
}
