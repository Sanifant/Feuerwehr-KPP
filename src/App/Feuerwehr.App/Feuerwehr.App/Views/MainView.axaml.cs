using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using de.openelp.feuerwehr.App.Pages;
using de.openelp.feuerwehr.App.ViewModels;
using System;

namespace De.Openelp.Feuerwehr.App.Views
{
    public partial class MainView : DrawerPage
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is MainViewModel vm)
            {
                vm.Navigation = NavPage;
            }
        }
    }
}

