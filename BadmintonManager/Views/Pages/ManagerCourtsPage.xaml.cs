using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Manager;

namespace BadmintonManager.Views.Pages;

public partial class ManagerCourtsPage : UserControl
{
    public ManagerCourtsPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManagerCourtsViewModel vm)
        {
            await vm.LoadCourtsAsync();
        }
    }
}
