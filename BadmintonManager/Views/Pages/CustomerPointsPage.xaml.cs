using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Customer;

namespace BadmintonManager.Views.Pages;

public partial class CustomerPointsPage : UserControl
{
    public CustomerPointsPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerPointsViewModel vm)
        {
            await vm.LoadPointsDataAsync();
        }
    }
}
