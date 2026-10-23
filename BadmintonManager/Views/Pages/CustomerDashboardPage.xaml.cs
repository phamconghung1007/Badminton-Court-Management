using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Customer;

namespace BadmintonManager.Views.Pages;

public partial class CustomerDashboardPage : UserControl
{
    public CustomerDashboardPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerDashboardViewModel vm)
        {
            await vm.LoadDashboardAsync();
        }
    }
}
