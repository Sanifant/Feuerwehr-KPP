using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Interfaces;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace de.openelp.feuerwehr.desktop.ViewModels
{
    public partial class InventoryViewModel : ViewModelBase
    {
        private readonly IApiService _apiService;

        public InventoryViewModel(IApiService apiService) 
        { 
            _apiService = apiService;

            this.LoadItems();
            this.SelectedItem = new InventoryItem();
        }

        public ObservableCollection<InventoryItem> Items { get; set; } = new();
           

        [ObservableProperty]
        private InventoryItem _selectedItem;

        [RelayCommand]
        public void LoadItems()
        {
            var items = _apiService.GetAll();
            Items.Clear();
            foreach (var item in items.Result)
            {
                Items.Add(item);
            }

            OnPropertyChanged(nameof(Items));
        }

        [RelayCommand]
        public void AddItem()
        {
            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dialog = new InventoryDialog
                {
                    DataContext = new InventoryDialogViewModel(_apiService)
                    {
                        OnSaved = LoadItems
                    }
                };

                dialog.ShowDialog(desktop.MainWindow);
            }
        }
    }
}
