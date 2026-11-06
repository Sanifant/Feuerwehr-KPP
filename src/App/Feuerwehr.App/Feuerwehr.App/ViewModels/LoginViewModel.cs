using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using De.Openelp.Feuerwehr.App.Services;

namespace De.Openelp.Feuerwehr.App.ViewModels
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
            this._authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(this.Email) || string.IsNullOrWhiteSpace(this.Password))
            {
                this.ErrorMessage = "Bitte E-Mail und Passwort eingeben.";
                return;
            }

            this.IsLoggingIn = true;
            this.ErrorMessage = null;

            try
            {
                bool success = await this._authService.LoginAsync(this.Email, this.Password);

                if (success)
                {
                    // Clear password for security
                    this.Password = string.Empty;
                    this.LoginSuccessful?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    this.ErrorMessage = "Anmeldung fehlgeschlagen. Bitte überprüfen Sie Ihre Anmeldedaten.";
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Fehler bei der Anmeldung: {ex.Message}";
            }
            finally
            {
                this.IsLoggingIn = false;
            }
        }

        [RelayCommand]
        private void ClearError()
        {
            this.ErrorMessage = null;
        }
    }
}
