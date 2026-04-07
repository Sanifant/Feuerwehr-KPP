using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Service;
using de.openelp.feuerwehr.domain;
using System;
using System.Threading.Tasks;

namespace de.openelp.feuerwehr.desktop.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly AuthApiService _authApi;
        private readonly AuthTokenStore _tokenStore;

        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;



        public Action OnLoginSuccess { get; set; }

        public LoginViewModel(AuthApiService authApi, AuthTokenStore tokenStore)
        {
            _authApi = authApi;
            _tokenStore = tokenStore;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void LoginCommand()
        {
            _ = Login();
        }

        private async Task Login()
        {
            var token = await _authApi.Login(Username, Password);

            if(!token.Success)
            {
                ErrorMessage = token?.ErrorMessage ?? "Login failed";
                OnPropertyChanged(nameof(ErrorMessage));
                return;
            }

            _tokenStore.Token = token.User;

            OnLoginSuccess?.Invoke();
        }
    }

    public class AuthTokenStore
    {
        public ApplicationUser Token { get; internal set; }
    }
}
