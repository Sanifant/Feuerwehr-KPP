using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using de.openelp.feuerwehr.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace de.openelp.feuerwehr.App;

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