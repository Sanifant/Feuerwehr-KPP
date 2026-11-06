using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;
using De.Openelp.Feuerwehr.App.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        /// <summary>Set by the platform host before framework initialization.</summary>
        public Action<IServiceCollection>? ConfigurePlatformServices { get; set; }

        /// <summary>
        /// Initializes the operation.
        /// </summary>
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Executes on Framework Initialization Completed.
        /// </summary>
        public override void OnFrameworkInitializationCompleted()
        {
            //var appConfiguration = AppConfiguration.Current;
            this.ReloadServices();

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                IAuthService authService = Services.GetRequiredService<IAuthService>();
                Func<MainViewModel> mainViewModelFactory = new (() => Services.GetRequiredService<MainViewModel>());
                Func<LoginViewModel> loginViewModelFactory = new (() => Services.GetRequiredService<LoginViewModel>());

                desktop.MainWindow = new MainWindow(authService, mainViewModelFactory, loginViewModelFactory);
            }
            else if (this.ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
            {
                singleViewFactoryApplicationLifetime.MainViewFactory = this.CreateActivityView;
            }
            else if (this.ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                IAuthService authService = Services.GetRequiredService<IAuthService>();
                // For single view platforms, check auth state
                if (authService.IsAuthenticated)
                {
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = Services.GetRequiredService<MainViewModel>()
                    };
                }
                else
                {
                    singleViewPlatform.MainView = new LoginView
                    {
                        DataContext = Services.GetRequiredService<LoginViewModel>()
                    };
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Rebuilds the DI service container, including the platform registrations.
        /// </summary>
        public void ReloadServices()
        {

            ServiceCollection collection = new ();

            _ = collection.AddTransient<MainViewModel>();
            _ = collection.AddTransient<LoginViewModel>();
            _ = collection.AddTransient<MapViewModel>();
            _ = collection.AddTransient<SituationMapViewModel>();
            _ = collection.AddTransient<InventoryViewModel>();
            _ = collection.AddSingleton<IBarcodeScanner, UnsupportedBarcodeScanner>();

            _ = collection.AddSingleton<IGpsService, UnsupportedGpsService>();

            // Authentication services
            _ = collection.AddSingleton<ISecureStorage, InMemorySecureStorage>();
            _ = collection.AddSingleton<IAuthService, AuthService>();
            _ = collection.AddScoped<AuthenticatedHttpClient>();

            // Für Blazor WebAssembly / Web-Clients
            _ = collection.AddScoped<IHydrantService, HydrantService>();
            _ = collection.AddScoped<ISituationMapService, SituationMapService>();
            _ = collection.AddScoped<IInventoryService, InventoryService>();

            string? baseUrl = Environment.GetEnvironmentVariable("webapi")
                ?? Environment.GetEnvironmentVariable("services__server__https__0")
                ?? Environment.GetEnvironmentVariable("services__server__http__0");
            // Explicit override takes precedence over Aspire's server endpoint references.

            if (string.IsNullOrEmpty(baseUrl))
            {
                // Standard-Fallback für lokales Testen ohne Aspire
                baseUrl = "https://integration.ff-kleinparin-pohnsdorf.de/";
            }

            _ = collection.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(baseUrl) });

            this.ConfigurePlatformServices?.Invoke(collection);
            Services = collection.BuildServiceProvider();
        }

        private Control CreateActivityView()
        {
            ContentControl root = new ();
            IAuthService authService = Services.GetRequiredService<IAuthService>();

            void ShowMainView()
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    root.Content = new MainView
                    {
                        DataContext = Services.GetRequiredService<MainViewModel>()
                    };
                });
            }

            if (authService.IsAuthenticated)
            {
                ShowMainView();
                return root;
            }

            LoginViewModel loginViewModel = Services.GetRequiredService<LoginViewModel>();

            EventHandler? handler = null;

            handler = (_, _) =>
            {
                loginViewModel.LoginSuccessful -= handler;
                ShowMainView();
            };

            loginViewModel.LoginSuccessful += handler;

            root.Content = new LoginView
            {
                DataContext = loginViewModel
            };

            return root;
        }
    }
}
