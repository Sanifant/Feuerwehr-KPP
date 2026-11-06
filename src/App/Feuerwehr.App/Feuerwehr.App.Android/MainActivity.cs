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

        internal static MainActivity? CurrentActivity =>
            current is not null && current.TryGetTarget(out MainActivity? activity)
                && !activity.IsFinishing && !activity.IsDestroyed ? activity : null;

        internal CancellationToken DestroyedToken => this.destroyed.Token;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            current = new WeakReference<MainActivity>(this);
            base.OnCreate(savedInstanceState);
        }

        internal bool HasLocationPermission =>
            ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted
            || ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted;

        /// <summary>
        /// Executes on Request Permissions Result.
        /// </summary>
        /// <param name="requestCode">The request Code.</param>
        /// <param name="permissions">The permissions.</param>
        /// <param name="grantResults">The grant Results.</param>
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
