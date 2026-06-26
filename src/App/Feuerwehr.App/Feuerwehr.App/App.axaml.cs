using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Feuerwehr.App.Services;
using Feuerwehr.App.ViewModels;
using Feuerwehr.App.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.IO;

namespace Feuerwehr.App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            //var appConfiguration = AppConfiguration.Current;
            ReloadServices();


            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };
            }
            else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = new MainViewModel() };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }


            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Rebuilds the DI service container, optionally registering the given configuration persistence implementation.
        /// </summary>
        /// <param name="configurationPersistence">
        /// The persistence service to register, or <c>null</c> if no persistence is needed
        /// (e.g. on Android where <c>MainActivity</c> handles it).
        /// </param>
        public void ReloadServices()
        {

            ServiceCollection collection = new ServiceCollection();

            collection.AddTransient<MainViewModel>();
            collection.AddTransient<MapViewModel>();

            collection.AddScoped<IGpsService, GpsService>();

            // Für Blazor WebAssembly / Web-Clients
            collection.AddScoped<IHydrantService, HydrantService>();

            var baseUrl = Environment.GetEnvironmentVariable("webapi");
            // Aspire injiziert Verbindungen als ConnectionString ("webapi")
            // Im Release nutzen wir die FallbackUrl aus der appsettings.json

            if (string.IsNullOrEmpty(baseUrl))
            {
                // Standard-Fallback für lokales Testen ohne Aspire
                baseUrl = "https://localhost:7538/";
            }

            collection.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });

            Services = collection.BuildServiceProvider();
        }
    }
}