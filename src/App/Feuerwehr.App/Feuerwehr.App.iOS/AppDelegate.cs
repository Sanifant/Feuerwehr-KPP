using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using Foundation;
using UIKit;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.iOS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace De.Openelp.Feuerwehr.App.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    /// <summary>
    /// Configures Avalonia and platform services for the iOS application lifecycle.
    /// </summary>
    [Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        /// <summary>
        /// Registers platform-specific services in the Avalonia application builder.
        /// </summary>
        /// <param name="builder">The builder whose services and host settings are configured.</param>
        /// <returns>The configured Avalonia application builder.</returns>
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .AfterSetup(app => ((App)app.Instance!).ConfigurePlatformServices = services =>
                    services.Replace(ServiceDescriptor.Singleton<IBarcodeScanner, IosBarcodeScanner>()));
        }
    }
}

