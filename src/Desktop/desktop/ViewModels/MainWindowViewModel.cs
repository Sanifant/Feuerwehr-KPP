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

        public string Title { get; set; } = "Feuerwehrverwaltung";

        public ObservableCollection<NavigationItem> NavigationItems { get; set; } = new()
        {
            new NavigationItem { Name = "Dashboard", ViewModelType = typeof(DashboardViewModel) },
            new NavigationItem { Name = "Inventory", ViewModelType = typeof(InventoryViewModel) },
            // Weitere Navigationselemente hier hinzufügen
        };

        public MainWindowViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            foreach (var item in NavigationItems)
            {
                item.ShowViewCommand = ShowViewCommand;
            }

            CurrentView = _serviceProvider.GetService<DashboardViewModel>() ?? new DashboardViewModel();
        }

        [RelayCommand]
        public void ShowView(NavigationItem item)
        {
            var vm = _serviceProvider.GetService(item.ViewModelType);
            if (vm is null) return;

            CurrentView = vm;
            OnPropertyChanged(nameof(CurrentView));
        }

        public object CurrentView { get; set; }

    }
}
