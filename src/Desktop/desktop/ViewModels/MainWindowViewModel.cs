using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.desktop.Views;
using de.openelp.feuerwehr.domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            CurrentView = new DashboardView();
        }

        [RelayCommand]
        public void ShowDashboardCommand()
        {
            CurrentView = _serviceProvider.GetRequiredService<DashboardViewModel>();
            this.OnPropertyChanged(nameof(CurrentView));
        }

        [RelayCommand]
        public void ShowInventoryCommand()
        {
            CurrentView = _serviceProvider.GetRequiredService<InventoryViewModel>();
            this.OnPropertyChanged(nameof(CurrentView));
        }

        public object CurrentView { get; set; }

    }
}
