using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminCourtsPage : UserControl
{
    public AdminCourtsPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminCourtsViewModel vm)
        {
            await vm.LoadCourtsAsync();
        }
    }
}
