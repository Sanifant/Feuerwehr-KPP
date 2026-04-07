using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.desktop.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;

namespace de.openelp.feuerwehr.desktop
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                

                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                this.ConfigureServices(services);

                var login = new LoginWindow();
                var vm = _serviceProvider.GetRequiredService<LoginViewModel>();

                login.DataContext = vm;

                vm.OnLoginSuccess = () =>
                {

                    var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = mainWindowViewModel,
                    };

                    desktop.MainWindow.Show();
                    login.Close();
                };

                login.Show();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Configuration
            services.AddSingleton<IConfiguration>(configuration);
            services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

            // ViewModels
            services.AddTransient<ApiService>();
            services.AddSingleton<AuthTokenStore>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<LoginViewModel>();

            // Services
            //services.AddSingleton<INavigationService, NavigationService>();

            // HttpClient
#if DEBUG
            // Nur für Entwicklung: Selbstsignierte Zertifikate akzeptieren
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            services.AddHttpClient<ApiService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
            services.AddHttpClient<AuthApiService>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
#else
            services.AddHttpClient<ApiService>();
            services.AddHttpClient<AuthApiService>();
#endif
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}