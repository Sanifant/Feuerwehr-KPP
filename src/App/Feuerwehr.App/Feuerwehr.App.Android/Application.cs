using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using de.openelp.feuerwehr.App.Android.Services;
using de.openelp.feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace de.openelp.feuerwehr.App.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .AfterSetup(appBuilder =>
                {
                    ((App)appBuilder.Instance!).ConfigurePlatformServices = services =>
                        services.Replace(ServiceDescriptor.Singleton<IGpsService, AndroidGpsService>());
                });
        }
    }
}
