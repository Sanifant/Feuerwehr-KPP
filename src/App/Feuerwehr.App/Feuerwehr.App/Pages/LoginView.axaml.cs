using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App
{
    /// <summary>
    /// Displays the login interface and coordinates its view lifecycle.
    /// </summary>
    public partial class LoginView : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginView" /> class with its required dependencies.
        /// </summary>
        public LoginView()
        {
            this.InitializeComponent();

            ViewModelBase vm = App.Services.GetService<LoginViewModel>();
            if (vm is null)
            {
                throw new System.InvalidOperationException($"{nameof(LoginViewModel)} is not registered in the application service provider.");
            }

            this.DataContext = vm;
        }
    }
}
