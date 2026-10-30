using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using De.Openelp.Feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace De.Openelp.Feuerwehr.App;

public partial class LoginView : ContentPage
{
    public LoginView()
    {
        InitializeComponent();

        ViewModelBase vm = App.Services.GetService<LoginViewModel>();
        if (vm is null)
        {
            throw new System.InvalidOperationException($"{nameof(LoginViewModel)} is not registered in the application service provider.");
        }
        DataContext = vm;
    }
}
