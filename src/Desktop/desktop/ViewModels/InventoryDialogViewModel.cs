using CommunityToolkit.Mvvm.Input;
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
        private readonly ApiService _api;

        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public string Condition { get; set; }
        public string Location { get; set; }



        public Action OnSaved { get; set; }

        public InventoryDialogViewModel(ApiService api)
        {
            _api = api;

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
