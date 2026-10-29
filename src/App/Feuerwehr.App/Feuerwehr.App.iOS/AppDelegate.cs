using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using Foundation;
using UIKit;
using de.openelp.feuerwehr.App.Services;
using de.openelp.feuerwehr.App.iOS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace de.openelp.feuerwehr.App.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .AfterSetup(app => ((App)app.Instance!).ConfigurePlatformServices = services =>
                    services.Replace(ServiceDescriptor.Singleton<IBarcodeScanner, IosBarcodeScanner>()));
        }
    }
}
