using System;
using Avalonia.Controls;
using De.Openelp.Feuerwehr.App.Services;
using De.Openelp.Feuerwehr.App.ViewModels;

namespace De.Openelp.Feuerwehr.App.Views
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
            LoginViewModel loginViewModel = _loginViewModelFactory();
            loginViewModel.LoginSuccessful += OnLoginSuccessful;

            LoginView loginView = new LoginView
            {
                DataContext = loginViewModel
            };

            Content = loginView;
        }

        private void ShowMainView()
        {
            MainViewModel mainViewModel = _mainViewModelFactory();

            MainView mainView = new MainView
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
