using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using de.openelp.feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace de.openelp.feuerwehr.App.Pages;

public partial class InventoryView : ContentPage
{
    public InventoryView()
    {
        this.InitializeComponent();
        this.DataContext = App.Services.GetRequiredService<InventoryViewModel>();
    }
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (this.DataContext is InventoryViewModel vm)
            vm.Activate();
        this.BarcodeInput.Focus();
    }
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (this.DataContext is InventoryViewModel vm)
            vm.Deactivate();
        base.OnUnloaded(e);
    }
    private async void OnBarcodeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && this.DataContext is InventoryViewModel vm && vm.LookupCommand.CanExecute(null))
        { e.Handled = true; await vm.LookupCommand.ExecuteAsync(null); }
    }
}
