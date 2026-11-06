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
            this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this._mainViewModelFactory = mainViewModelFactory ?? throw new ArgumentNullException(nameof(mainViewModelFactory));
            this._loginViewModelFactory = loginViewModelFactory ?? throw new ArgumentNullException(nameof(loginViewModelFactory));

            this.InitializeComponent();

            // Subscribe to auth state changes
            this._authService.AuthStateChanged += this.OnAuthStateChanged;

            // Set initial view based on authentication state
            this.UpdateView();
        }

        protected override void OnClosed(EventArgs e)
        {
            this._authService.AuthStateChanged -= this.OnAuthStateChanged;
            base.OnClosed(e);
        }

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            this.UpdateView();
        }

        private void UpdateView()
        {
            if (this._authService.IsAuthenticated)
            {
                this.ShowMainView();
            }
            else
            {
                this.ShowLoginView();
            }
        }

        private void ShowLoginView()
        {
            LoginViewModel loginViewModel = this._loginViewModelFactory();
            loginViewModel.LoginSuccessful += this.OnLoginSuccessful;

            LoginView loginView = new LoginView
            {
                DataContext = loginViewModel
            };

            this.Content = loginView;
        }

        private void ShowMainView()
        {
            MainViewModel mainViewModel = this._mainViewModelFactory();

            MainView mainView = new MainView
            {
                DataContext = mainViewModel
            };

            this.Content = mainView;
        }

        private void OnLoginSuccessful(object? sender, EventArgs e)
        {
            if (sender is LoginViewModel loginViewModel)
            {
                loginViewModel.LoginSuccessful -= this.OnLoginSuccessful;
            }
            this.UpdateView();
        }
    }
}
