using System;
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

namespace De.Openelp.Feuerwehr.App.ViewModels
{
    /// <summary>
    /// Provides bindable state and commands for the main view.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private int _selectedPageIndex;

        private IncidentSummaryDto _incident = null;

        [ObservableProperty]
        private bool _isDrawerOpened;

        /// <summary>
        /// Gets the available navigation pages.
        /// </summary>
        public ObservableCollection<PageItem> Pages { get; } = new ObservableCollection<PageItem>();

        /// <summary>
        /// Gets or sets the selected navigation page index.
        /// </summary>
        public int SelectedPageIndex
        {
            get => this._selectedPageIndex;
            set
            {
                if (this.SetProperty(ref this._selectedPageIndex, value))
                {
                    this.OnPropertyChanged(nameof(this.SelectedPageIndex));
                    _ = this.NavigateTo(value);
                    this.IsDrawerOpened = false;
                }
            }
        }

        /// <summary>
        /// Gets the version of the running application.
        /// </summary>
        public string AppVersion => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "1.0.0";

        /// <summary>
        /// Gets the copyright notice from assembly metadata.
        /// </summary>
        public string? Copyright => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyCopyrightAttribute>()?
            .Copyright;

        /// <summary>
        /// Gets the application description from assembly metadata.
        /// </summary>
        public string? Description => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyDescriptionAttribute>()?
            .Description;

        /// <summary>
        /// Gets the application title from assembly metadata.
        /// </summary>
        public string? ProductTitle => Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyProductAttribute>()?
            .Product;

        /// <summary>
        /// Gets the signed-in user's email address.
        /// </summary>
        public string? UserEmail => this._authService.UserEmail;

        /// <summary>
        /// Gets the signed-in user's full display name.
        /// </summary>
        public string? UserName => this._authService.UserFullName;

        /// <summary>
        /// Gets the current incident summary.
        /// </summary>
        public IncidentSummaryDto Incident => this._incident;

        /// <summary>
        /// Gets the application navigation service.
        /// </summary>
        public INavigation? Navigation { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether a user is currently signed in.
        /// </summary>
        public bool IsAuthenticated => this._authService.IsAuthenticated;

        /// <summary>
        /// Gets a value indicating whether the signed-in user can open the situation map.
        /// </summary>
        public bool CanViewSituationMap => this.IsAuthenticated && (this._authService.UserRoles.Contains("SituationMapViewer") || this._authService.UserRoles.Contains("SituationMapEditor"));

        /// <summary>
        /// Gets a value indicating whether the signed-in user can open the equipment inventory.
        /// </summary>
        public bool CanViewInventory => this.IsAuthenticated && this._authService.UserRoles.Any(role => role is "Admin" or "EquipmentOfficer" or "InventoryViewer");

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class with its required dependencies.
        /// </summary>
        /// <param name="authService">The authentication service supplying session state and user roles.</param>
        public MainViewModel(IAuthService authService)
        {
            this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this._authService.AuthStateChanged += this.OnAuthStateChanged;
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
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }

            PageItem item = this.Pages[selectedIndex];

            if (item != null)
            {

                object view = item.Factory();
                if (view is Page page)
                {
                    return this.Navigation.ReplaceAsync(page);
                }
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
