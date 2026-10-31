using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Models;
using De.Openelp.Feuerwehr.App.Pages;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;

namespace De.Openelp.Feuerwehr.App.ViewModels
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
            get => _selectedPageIndex;
            set
            {
                if (SetProperty(ref _selectedPageIndex, value))
                {
                    OnPropertyChanged(nameof(SelectedPageIndex));
                    NavigateTo(value);
                    IsDrawerOpened = false;
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

        public string? UserEmail => _authService.UserEmail;
        public string? UserName => _authService.UserFullName;

        public IncidentSummaryDto Incident => _incident;

        public INavigation? Navigation { get; internal set; }

        public bool IsAuthenticated => _authService.IsAuthenticated;
        public bool CanViewSituationMap => IsAuthenticated && (_authService.UserRoles.Contains("SituationMapViewer") || _authService.UserRoles.Contains("SituationMapEditor"));
        public bool CanViewInventory => IsAuthenticated && _authService.UserRoles.Any(role => role is "Admin" or "EquipmentOfficer" or "InventoryViewer");

        public MainViewModel(IAuthService authService)
        { 
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _authService.AuthStateChanged += OnAuthStateChanged;
            OnPropertyChanged(nameof(Incident));

            PreparePages();
        }

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UserEmail));
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CanViewSituationMap));
            OnPropertyChanged(nameof(CanViewInventory));
        }

        private void PreparePages()
        {  
            Pages.Add(new PageItem("Hydranten", () => new MapView()));
            Pages.Add(new PageItem("Einsatzkarte", () => new SituationMapView(), "M 0,0 L 1,0 L 1,1 L 0,1 Z", () => CanViewSituationMap));
            Pages.Add(new PageItem("Inventar / Geräteprüfung", () => new InventoryView(), isVisible: () => CanViewInventory));
            this.SelectedPageIndex = 0;
        }

        private Task NavigateTo(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= Pages.Count || Navigation is null)
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));

            PageItem item = Pages[selectedIndex];

            if(item != null)
            {

                object view = item.Factory();
                if(view is Page page)
                    return Navigation.ReplaceAsync(page);
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
        }
    }
}

