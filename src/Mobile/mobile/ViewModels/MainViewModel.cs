using CommunityToolkit.Mvvm.ComponentModel;

namespace de.openelp.feuerwehr.mobile.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";
    }
}
