using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminBranchesPage : UserControl
{
    public AdminBranchesPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminBranchesViewModel vm)
        {
            await vm.LoadBranchesAsync();
        }
    }
}
