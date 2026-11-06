using System;
using Avalonia.Controls;
using de.openelp.feuerwehr.App.ViewModels;

namespace de.openelp.feuerwehr.App.Views
{
    public partial class MainView : DrawerPage
    {
        public MainView()
        {
            this.InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (this.DataContext is MainViewModel vm)
            {
                vm.Navigation = this.NavPage;
            }
        }
    }
}

