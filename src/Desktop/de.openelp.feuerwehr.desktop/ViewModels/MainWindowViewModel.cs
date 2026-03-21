using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.domain;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private HttpClient httpClient;
        public MainWindowViewModel()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://localhost:49402");
        }

        private async Task LoadInventoryAsync(HttpClient httpClient)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/InventoryItem");
                if (response.IsSuccessStatusCode)
                {
                    var items = await response.Content.ReadFromJsonAsync<InventoryItem[]>();
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            Items.Add(item);
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Server ist nicht erreichbar - entsprechend behandeln
                System.Diagnostics.Debug.WriteLine($"API-Fehler: {ex.Message}");
            }
        }

        public ObservableCollection<InventoryItem> Items { get; } = new();

        public string Greeting { get; } = "Welcome to Avalonia!";

        [RelayCommand]
        public void LoadItems()
        {
                _ = LoadInventoryAsync(httpClient);
        }
    }
}
