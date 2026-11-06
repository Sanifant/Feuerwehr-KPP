using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.App.Services;
using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using de.openelp.feuerwehr.Common.Models.IncidentModules;
using System.Collections.ObjectModel;
using de.openelp.feuerwehr.App.Models;
using de.openelp.feuerwehr.App.Pages;
using Avalonia.Controls;

namespace de.openelp.feuerwehr.App.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private int _selectedPageIndex;

        private IncidentSummaryDto _incident = null;

        [ObservableProperty]
        private bool _isDrawerOpened;

        public ObservableCollection<PageItem> Pages { get; } = new ObservableCollection<PageItem>();

        public int SelectedPageIndex
        {
            get => this._selectedPageIndex;
            set
            {
                if (this.SetProperty(ref this._selectedPageIndex, value))
                {
                    this.OnPropertyChanged(nameof(this.SelectedPageIndex));
                    this.NavigateTo(value);
                    this.IsDrawerOpened = false;
                }
            }
        }

        public string AppVersion => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "1.0.0";

        public string? Copyright => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?
            .Copyright;

        public string? Description => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyDescriptionAttribute>()?
            .Description;

        public string? ProductTitle => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product;

        public string? UserEmail => this._authService.UserEmail;
        public string? UserName => this._authService.UserFullName;

        public IncidentSummaryDto Incident => this._incident;

        public INavigation? Navigation { get; internal set; }

        public bool IsAuthenticated => this._authService.IsAuthenticated;
        public bool CanViewSituationMap => this.IsAuthenticated && (this._authService.UserRoles.Contains("SituationMapViewer") || this._authService.UserRoles.Contains("SituationMapEditor"));
        public bool CanViewInventory => this.IsAuthenticated && this._authService.UserRoles.Any(role => role is "Admin" or "EquipmentOfficer" or "InventoryViewer");

        public MainViewModel(IAuthService authService)
        {
            this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this._authService.AuthStateChanged += OnAuthStateChanged;
            this.OnPropertyChanged(nameof(this.Incident));

            this.PreparePages();
        }

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            this.OnPropertyChanged(nameof(this.UserEmail));
            this.OnPropertyChanged(nameof(this.UserName));
            this.OnPropertyChanged(nameof(this.IsAuthenticated));
            this.OnPropertyChanged(nameof(this.CanViewSituationMap));
            this.OnPropertyChanged(nameof(this.CanViewInventory));
        }

        private void PreparePages()
        {
            this.Pages.Add(new PageItem("Hydranten", () => new MapView()));
            this.Pages.Add(new PageItem("Einsatzkarte", () => new SituationMapView(), "M 0,0 L 1,0 L 1,1 L 0,1 Z", () => this.CanViewSituationMap));
            this.Pages.Add(new PageItem("Inventar / Geräteprüfung", () => new InventoryView(), isVisible: () => this.CanViewInventory));
            this.SelectedPageIndex = 0;
        }

        private Task NavigateTo(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= this.Pages.Count || this.Navigation is null)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            PageItem item = this.Pages[selectedIndex];

            if (item != null)
            {

                var view = item.Factory();
                if (view is Page page)
                    return this.Navigation.ReplaceAsync(page);
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await this._authService.LogoutAsync();
        }
    }
}

