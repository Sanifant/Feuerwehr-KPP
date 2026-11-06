using Avalonia.Controls;
using Avalonia.Interactivity;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App.Pages
{
    /// <summary>
    /// Displays the situation map interface and coordinates its view lifecycle.
    /// </summary>
    public partial class SituationMapView : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SituationMapView" /> class with its required dependencies.
        /// </summary>
        public SituationMapView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SituationMapViewModel>();
        }

        /// <summary>
        /// Activates the view model and starts loading data when the view is attached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (this.DataContext is SituationMapViewModel viewModel)
            {
                viewModel.Activate();
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Deactivates the view model and cancels pending work when the view is detached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (this.DataContext is SituationMapViewModel viewModel)
            {
                viewModel.Deactivate();
            }

            base.OnUnloaded(e);
        }
    }
}
