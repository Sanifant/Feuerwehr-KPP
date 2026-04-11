using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Interfaces;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace de.openelp.feuerwehr.desktop.ViewModels
{

    public partial class InventoryDialogViewModel : ViewModelBase
    {
        private readonly IApiService _api;

        public string Name { get; set; } = string.Empty;
        public InventoryCategory? Category { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public InventoryCategory[] Categories => _api.GetInventoryCategories();

        public Action OnSaved { get; set; }

        public InventoryDialogViewModel(IApiService api)
        {
            _api = api;
            OnSaved = () => { };
        }

        [RelayCommand]
        public void SaveCommand()
        {
            _api.Create(new InventoryItem
            {
                Name = Name,
                Category = Category,
                Quantity = Quantity,
                Condition = Condition,
                Location = Location
            }).Wait();

            OnSaved?.Invoke();
        }
    }
}
