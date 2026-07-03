using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Feuerwehr.App.Pages;

public partial class MapView : ContentPage
{
    public MapView()
    {
        InitializeComponent();

        MapViewModel vm = App.Services.GetService<MapViewModel>();
        if (vm is null)
        {
            throw new System.InvalidOperationException($"{nameof(MapViewModel)} is not registered in the application service provider.");
        }
        DataContext = vm;
    }
}