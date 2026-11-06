using System;
using Avalonia.Controls;
using De.Openelp.Feuerwehr.App.ViewModels;

namespace De.Openelp.Feuerwehr.App.Views
{
    /// <summary>
    /// Displays the main interface and coordinates its view lifecycle.
    /// </summary>
    public partial class MainView : DrawerPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainView" /> class with its required dependencies.
        /// </summary>
        public MainView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Updates the view's subscriptions when its bound view model changes.
        /// </summary>
        /// <param name="e">The event data supplied by the UI framework.</param>
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
