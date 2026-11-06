using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App.Pages
{
    /// <summary>
    /// Displays the inventory interface and coordinates its view lifecycle.
    /// </summary>
    public partial class InventoryView : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryView" /> class with its required dependencies.
        /// </summary>
        public InventoryView()
        {
            this.InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<InventoryViewModel>();
        }

        /// <summary>
        /// Activates the view model and starts loading data when the view is attached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (this.DataContext is InventoryViewModel vm)
            {
                vm.Activate();
            }

            _ = this.BarcodeInput.Focus();
        }

        /// <summary>
        /// Deactivates the view model and cancels pending work when the view is detached.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
        protected override void OnUnloaded(RoutedEventArgs e)
        {
            if (this.DataContext is InventoryViewModel vm)
            {
                vm.Deactivate();
            }

            base.OnUnloaded(e);
        }

        private async void OnBarcodeKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.DataContext is InventoryViewModel vm && vm.LookupCommand.CanExecute(null))
            {
                e.Handled = true;
                await vm.LookupCommand.ExecuteAsync(null);
            }
        }
    }
}
