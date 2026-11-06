using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App.Pages
{
    /// <summary>
    /// Displays the map interface and coordinates its view lifecycle.
    /// </summary>
    public partial class MapView : ContentPage
    {
        private CancellationTokenSource? locationInitialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapView" /> class with its required dependencies.
        /// </summary>
        public MapView()
        {
            this.InitializeComponent();

            this.DataContext = App.Services.GetRequiredService<MapViewModel>();
        }

        /// <summary>
        /// Activates the view model and starts loading data when the view is attached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            this.locationInitialization?.Cancel();
            this.locationInitialization?.Dispose();
            this.locationInitialization = new CancellationTokenSource();
            CancellationToken cancellationToken = this.locationInitialization.Token;
            try
            {
                if (this.DataContext is MapViewModel viewModel)
                {
                    await viewModel.InitializeLocationAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Navigating away cancels the pending location request.
            }
        }

        /// <summary>
        /// Deactivates the view model and cancels pending work when the view is detached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            this.locationInitialization?.Cancel();
            this.locationInitialization?.Dispose();
            this.locationInitialization = null;
            if (this.DataContext is MapViewModel viewModel)
            {
                viewModel.CenterOnLocationCommand.Cancel();
                viewModel.SettingsCommand.Cancel();
            }

            base.OnUnloaded(e);
        }
    }
}
