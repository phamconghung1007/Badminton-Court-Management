using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Customer;

namespace BadmintonManager.Views.Pages;

public partial class CustomerBookCourtPage : UserControl
{
    public CustomerBookCourtPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerBookCourtViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
