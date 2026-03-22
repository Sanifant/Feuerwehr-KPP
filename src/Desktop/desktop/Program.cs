using Avalonia;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace de.openelp.feuerwehr.desktop
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();

            // ViewModels
            services.AddHttpClient<ApiService>();
            services.AddTransient<ApiService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<LoginViewModel>();

            // Services
            //services.AddSingleton<INavigationService, NavigationService>();

            // HttpClient
            services.AddHttpClient<ApiService>();

            // Build
            var provider = services.BuildServiceProvider();

            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
