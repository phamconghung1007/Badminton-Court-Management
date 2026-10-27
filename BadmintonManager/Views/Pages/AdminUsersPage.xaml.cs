using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminUsersPage : UserControl
{
    public AdminUsersPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminUsersViewModel vm)
        {
            await vm.LoadUsersAsync();
        }
    }

    private void FormPassBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminUsersViewModel vm && sender is PasswordBox pb)
        {
            vm.FormPassword = pb.Password;
        }
    }

    private void NewPassBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminUsersViewModel vm && sender is PasswordBox pb)
        {
            vm.NewResetPassword = pb.Password;
        }
    }
}
