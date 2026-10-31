
using System;
using Avalonia;
using De.Openelp.Feuerwehr.App.Desktop.Services;
using De.Openelp.Feuerwehr.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace De.Openelp.Feuerwehr.App.Desktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        /// <summary>
        /// Executes main.
        /// </summary>
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        /// <summary>
        /// Builds the avalonia App.
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
#if DEBUG
                .WithDeveloperTools()
#endif
                .WithInterFont()
                .AfterSetup(appBuilder =>
                {
                    ((App)appBuilder.Instance!).ConfigurePlatformServices = services =>
                        services.Replace(ServiceDescriptor.Singleton<IGpsService, DesktopGpsService>());
                })
                .LogToTrace();
    }
}

