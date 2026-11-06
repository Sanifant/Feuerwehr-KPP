using System;
using Avalonia.Controls;
using De.Openelp.Feuerwehr.App.ViewModels;

namespace De.Openelp.Feuerwehr.App.Views
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
