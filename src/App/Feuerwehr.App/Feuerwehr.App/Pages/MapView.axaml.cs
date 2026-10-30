using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Interactivity;
using System;
using System.Threading;

namespace De.Openelp.Feuerwehr.App.Pages;

public partial class MapView : ContentPage
{
    private CancellationTokenSource? locationInitialization;

    public MapView()
    {
        InitializeComponent();

        DataContext = App.Services.GetRequiredService<MapViewModel>();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        locationInitialization?.Cancel();
        locationInitialization?.Dispose();
        locationInitialization = new CancellationTokenSource();
        var cancellationToken = locationInitialization.Token;
        try
        {
            if (DataContext is MapViewModel viewModel)
                await viewModel.InitializeLocationAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Navigating away cancels the pending location request.
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        locationInitialization?.Cancel();
        locationInitialization?.Dispose();
        locationInitialization = null;
        if (DataContext is MapViewModel viewModel)
        {
            viewModel.CenterOnLocationCommand.Cancel();
            viewModel.SettingsCommand.Cancel();
        }
        base.OnUnloaded(e);
    }
}

