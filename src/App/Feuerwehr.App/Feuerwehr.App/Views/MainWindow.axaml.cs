using Avalonia.Controls;
using Feuerwehr.App.Services;
using Feuerwehr.App.ViewModels;
using System;

namespace Feuerwehr.App.Views
{
    public partial class MainWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly Func<MainViewModel> _mainViewModelFactory;
        private readonly Func<LoginViewModel> _loginViewModelFactory;

        public MainWindow(
            IAuthService authService,
            Func<MainViewModel> mainViewModelFactory,
            Func<LoginViewModel> loginViewModelFactory)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _mainViewModelFactory = mainViewModelFactory ?? throw new ArgumentNullException(nameof(mainViewModelFactory));
            _loginViewModelFactory = loginViewModelFactory ?? throw new ArgumentNullException(nameof(loginViewModelFactory));

            InitializeComponent();

            // Subscribe to auth state changes
            _authService.AuthStateChanged += OnAuthStateChanged;

            // Set initial view based on authentication state
            UpdateView();
        }

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            UpdateView();
        }

        private void UpdateView()
        {
            if (_authService.IsAuthenticated)
            {
                ShowMainView();
            }
            else
            {
                ShowLoginView();
            }
        }

        private void ShowLoginView()
        {
            var loginViewModel = _loginViewModelFactory();
            loginViewModel.LoginSuccessful += OnLoginSuccessful;

            var loginView = new LoginView
            {
                DataContext = loginViewModel
            };

            Content = loginView;
        }

        private void ShowMainView()
        {
            var mainViewModel = _mainViewModelFactory();

            var mainView = new MainView
            {
                DataContext = mainViewModel
            };

            Content = mainView;
        }

        private void OnLoginSuccessful(object? sender, EventArgs e)
        {
            if (sender is LoginViewModel loginViewModel)
            {
                loginViewModel.LoginSuccessful -= OnLoginSuccessful;
            }
            UpdateView();
        }

        protected override void OnClosed(EventArgs e)
        {
            _authService.AuthStateChanged -= OnAuthStateChanged;
            base.OnClosed(e);
        }
    }
}