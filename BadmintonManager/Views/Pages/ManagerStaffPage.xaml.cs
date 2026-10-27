using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Manager;

namespace BadmintonManager.Views.Pages;

public partial class ManagerStaffPage : UserControl
{
    public ManagerStaffPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManagerStaffViewModel vm)
        {
            await vm.LoadStaffAsync();
        }
    }

    private void StaffPassBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManagerStaffViewModel vm && sender is PasswordBox pb)
        {
            vm.FormPassword = pb.Password;
        }
    }

    private void NewStaffPassBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ManagerStaffViewModel vm && sender is PasswordBox pb)
        {
            vm.NewResetPassword = pb.Password;
        }
    }
}
