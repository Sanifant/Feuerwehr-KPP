using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Feuerwehr.App.Services;
using System;
using System.Threading.Tasks;

namespace Feuerwehr.App.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isLoggingIn = false;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _rememberMe = true;

        public event EventHandler? LoginSuccessful;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Bitte E-Mail und Passwort eingeben.";
                return;
            }

            IsLoggingIn = true;
            ErrorMessage = null;

            try
            {
                var success = await _authService.LoginAsync(Email, Password);

                if (success)
                {
                    // Clear password for security
                    Password = string.Empty;
                    LoginSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    ErrorMessage = "Anmeldung fehlgeschlagen. Bitte überprüfen Sie Ihre Anmeldedaten.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Fehler bei der Anmeldung: {ex.Message}";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        [RelayCommand]
        private void ClearError()
        {
            ErrorMessage = null;
        }
    }
}
