using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace de.openelp.feuerwehr.desktop;

public partial class InventoryDialog : Window
{
    public InventoryDialog()
    {
        InitializeComponent();

    }

    /*
    public override void Show()
    {
        base.Show();

        var context = DataContext as ViewModels.InventoryDialogViewModel;
        if (context != null)
        {
            context.OnSaved += () =>
            {
                this.Close();
            };
        }
    }
    */
}