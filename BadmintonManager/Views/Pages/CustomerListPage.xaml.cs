using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Shared;

namespace BadmintonManager.Views.Pages;

public partial class CustomerListPage : UserControl
{
    public CustomerListPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomerListViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
