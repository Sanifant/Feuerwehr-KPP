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
            CurrentView = _serviceProvider.GetService<DashboardViewModel>() ?? new DashboardViewModel();
        }

        [RelayCommand]
        public void ShowDashboardCommand()
        {
            var vm = _serviceProvider.GetService<DashboardViewModel>();
            if (vm is null) return;

            CurrentView = vm;
            OnPropertyChanged(nameof(CurrentView));
        }

        [RelayCommand]
        public void ShowInventoryCommand()
        {
            var vm = _serviceProvider.GetService<InventoryViewModel>();
            if (vm is null) return;

            CurrentView = vm;
            OnPropertyChanged(nameof(CurrentView));
        }

        public object CurrentView { get; set; }

    }
}
