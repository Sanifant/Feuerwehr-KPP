using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using System;
using System.Threading;
using System.Threading.Tasks;
using AndroidX.Core.Content;
using Manifest = global::Android.Manifest;

namespace de.openelp.feuerwehr.App.Android
{
    [Activity(
        Label = "de.openelp.feuerwehr.App.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        private const int LocationPermissionRequestCode = 4101;
        private static WeakReference<MainActivity>? current;
        private readonly CancellationTokenSource destroyed = new();
        private TaskCompletionSource<bool>? locationPermissionRequest;

        internal static MainActivity? CurrentActivity =>
            current is not null && current.TryGetTarget(out var activity)
                && !activity.IsFinishing && !activity.IsDestroyed ? activity : null;

        internal CancellationToken DestroyedToken => destroyed.Token;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            current = new WeakReference<MainActivity>(this);
            base.OnCreate(savedInstanceState);
        }

        internal bool HasLocationPermission =>
            ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted
            || ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) == Permission.Granted;

        // Called on the UI thread; concurrent callers share the same permission dialog.
        internal Task<bool> EnsureLocationPermissionAsync()
        {
            if (HasLocationPermission)
                return Task.FromResult(true);

            if (locationPermissionRequest is not null)
                return locationPermissionRequest.Task;

            var request = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            locationPermissionRequest = request;
            RequestPermissions(
                new[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation },
                LocationPermissionRequestCode);
            return request.Task;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == LocationPermissionRequestCode)
            {
                var request = locationPermissionRequest;
                locationPermissionRequest = null;
                request?.TrySetResult(HasLocationPermission);
            }
        }

        protected override void OnDestroy()
        {
            if (current is not null && current.TryGetTarget(out var activity) && ReferenceEquals(activity, this))
                current = null;

            locationPermissionRequest?.TrySetResult(false);
            locationPermissionRequest = null;
            destroyed.Cancel();
            base.OnDestroy();
            destroyed.Dispose();
        }
    }
}
