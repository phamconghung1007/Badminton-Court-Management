using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Manager;

namespace BadmintonManager.Views.Pages;

public partial class ManagerDashboardPage : UserControl
{
    public ManagerDashboardPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManagerDashboardViewModel vm)
        {
            await vm.LoadDashboardAsync();
        }
    }
}
