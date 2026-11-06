using Avalonia.Controls;
using Avalonia.Interactivity;
using de.openelp.feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace de.openelp.feuerwehr.App.Pages
{
    public partial class SituationMapView : ContentPage
    {
        public SituationMapView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SituationMapViewModel>();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (this.DataContext is SituationMapViewModel viewModel)
            {
                viewModel.Activate();
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (this.DataContext is SituationMapViewModel viewModel)
                viewModel.Deactivate();
            base.OnUnloaded(e);
        }
    }
}
