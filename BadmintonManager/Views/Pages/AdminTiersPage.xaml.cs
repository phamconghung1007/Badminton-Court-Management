using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminTiersPage : UserControl
{
    public AdminTiersPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminTiersViewModel vm)
        {
            await vm.LoadTiersCommand.ExecuteAsync(null);
        }
    }
}
