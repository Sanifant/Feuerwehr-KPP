using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using De.Openelp.Feuerwehr.App.Android.Services;
using De.Openelp.Feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace De.Openelp.Feuerwehr.App.Android
{
    /// <summary>
    /// Bootstraps Avalonia and registers Android platform services.
    /// </summary>
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Application" /> class with its required dependencies.
        /// </summary>
        /// <param name="javaReference">The native Java object reference for this Android application.</param>
        /// <param name="transfer">The ownership policy for the native Java reference.</param>
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        /// <summary>
        /// Registers platform-specific services in the Avalonia application builder.
        /// </summary>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
        /// <returns>The configured Avalonia application builder.</returns>
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .AfterSetup(appBuilder =>
                {
                    ((App)appBuilder.Instance!).ConfigurePlatformServices = services =>
                    {
                        _ = services.Replace(ServiceDescriptor.Singleton<IGpsService, AndroidGpsService>());
                        _ = services.Replace(ServiceDescriptor.Singleton<IBarcodeScanner, AndroidBarcodeScanner>());
                    };
                });
        }
    }
}
