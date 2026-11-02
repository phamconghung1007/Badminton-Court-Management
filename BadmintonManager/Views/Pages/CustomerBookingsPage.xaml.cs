using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Customer;

namespace BadmintonManager.Views.Pages;

public partial class CustomerBookingsPage : UserControl
{
    public CustomerBookingsPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerBookingsViewModel vm)
        {
            await vm.LoadBookingsAsync();
        }
    }
}
