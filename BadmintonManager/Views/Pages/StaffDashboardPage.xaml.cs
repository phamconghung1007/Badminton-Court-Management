using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Staff;

namespace BadmintonManager.Views.Pages;

public partial class StaffDashboardPage : UserControl
{
    public StaffDashboardPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StaffDashboardViewModel vm)
        {
            await vm.LoadDashboardAsync();
        }
    }
}
