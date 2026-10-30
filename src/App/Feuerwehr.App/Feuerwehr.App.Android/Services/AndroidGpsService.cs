using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.Locations;
using AndroidX.Core.Content;
using AndroidX.Core.Location;
using AndroidX.Core.Util;
using Avalonia.Threading;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Services;
using CancellationSignal = global::Android.OS.CancellationSignal;
using Manifest = global::Android.Manifest;

namespace De.Openelp.Feuerwehr.App.Android.Services;

/// <summary>Obtains one foreground location fix without requiring Google Play Services.</summary>
public sealed class AndroidGpsService : IGpsService
{
    private static readonly TimeSpan LocationTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the current Location.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    public async Task<GpsLocation?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Dispatcher.UIThread.InvokeAsync(() => GetLocationOnMainThreadAsync(cancellationToken));
    }

    private static async Task<GpsLocation?> GetLocationOnMainThreadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var activity = MainActivity.CurrentActivity;
        if (activity is null)
            return null;

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, activity.DestroyedToken);
        try
        {
            if (!await activity.EnsureLocationPermissionAsync().WaitAsync(lifetime.Token))
                return null;

            lifetime.Token.ThrowIfCancellationRequested();
            var context = global::Android.App.Application.Context;
            using var manager = context.GetSystemService(Context.LocationService) as LocationManager;
            if (manager is null || !LocationManagerCompat.IsLocationEnabled(manager))
                return null;

            var hasFineLocation = ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation)
                == Permission.Granted;
            string? provider = null;
            if (hasFineLocation && manager.IsProviderEnabled(LocationManager.GpsProvider))
                provider = LocationManager.GpsProvider;
            else if (manager.IsProviderEnabled(LocationManager.NetworkProvider))
                provider = LocationManager.NetworkProvider;

            if (provider is null)
                return null;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            timeout.CancelAfter(LocationTimeout);
            using var signal = new CancellationSignal();
            var completion = new TaskCompletionSource<GpsLocation?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var consumer = new LocationConsumer(completion);
            using var executor = ContextCompat.GetMainExecutor(context);
            try
            {
                LocationManagerCompat.GetCurrentLocation(manager, provider, signal, executor, consumer);
                return await completion.Task.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
            {
                return null;
            }
            finally
            {
                // Also removes pending listeners on API levels before Android 11.
                signal.Cancel();
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The activity was destroyed while permission or a fix was pending.
            return null;
        }
        catch (Java.Lang.SecurityException)
        {
            Debug.WriteLine("Location permission is no longer available.");
            return null;
        }
        catch (Java.Lang.IllegalArgumentException)
        {
            Debug.WriteLine("The requested location provider is unavailable.");
            return null;
        }
    }

    private sealed class LocationConsumer(TaskCompletionSource<GpsLocation?> completion) : Java.Lang.Object, IConsumer
    {
        /// <summary>
        /// Executes accept.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Accept(Java.Lang.Object? value)
        {
            completion.TrySetResult(value is Location location
                ? new GpsLocation { Latitude = location.Latitude, Longitude = location.Longitude }
                : null);
        }
    }
}

