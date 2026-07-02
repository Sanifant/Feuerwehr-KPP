using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Feuerwehr.App.Services;
using Mapsui;
using Mapsui.Tiling;
using Mapsui.Projections;
using Mapsui.Layers;
using Mapsui.Styles;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Feuerwehr.App.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private int _selectedPageIndex;

        [ObservableProperty]
        private bool _isDrawerOpened;

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
        public bool IsAuthenticated => _authService.IsAuthenticated;

        public MainViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _authService.AuthStateChanged += OnAuthStateChanged;
        }

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UserEmail));
            OnPropertyChanged(nameof(UserName));
            OnPropertyChanged(nameof(IsAuthenticated));
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
        }
    }
}
