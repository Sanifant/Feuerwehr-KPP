using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using de.openelp.feuerwehr.desktop.ViewModels;
using de.openelp.feuerwehr.desktop.Views;
using de.openelp.feuerwehr.desktop.Service;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            services.AddHttpClient<ApiService>();
            services.AddTransient<ApiService>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<InventoryViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<AuthTokenStore>();
            services.AddSingleton<AuthApiService>();
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