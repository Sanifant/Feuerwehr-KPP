using CommunityToolkit.Mvvm.Input;
using de.openelp.feuerwehr.desktop.Service;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
            var token = "token"; // await _authApi.Login(Username, Password);

            if (token == null)
            {
                ErrorMessage = "Invalid credentials";
                return;
            }

            _tokenStore.Token = token;

            OnLoginSuccess?.Invoke();
        }
    }

    public class AuthTokenStore
    {
        public string Token { get; internal set; } = string.Empty;
    }
}
