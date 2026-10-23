using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminDashboardPage : UserControl
{
    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminDashboardViewModel vm)
        {
            await vm.LoadDashboardAsync();
        }
    }
}
