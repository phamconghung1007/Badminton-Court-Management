using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Shared;

namespace BadmintonManager.Views.Pages;

public partial class UserProfilePage : UserControl
{
    public UserProfilePage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserProfileViewModel vm)
        {
            await vm.LoadProfileAsync();
        }
    }

    private void TxtCurrentPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserProfileViewModel vm && sender is PasswordBox pb)
        {
            vm.CurrentPassword = pb.Password;
        }
    }

    private void TxtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserProfileViewModel vm && sender is PasswordBox pb)
        {
            vm.NewPassword = pb.Password;
        }
    }

    private void TxtConfirmNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserProfileViewModel vm && sender is PasswordBox pb)
        {
            vm.ConfirmNewPassword = pb.Password;
        }
    }
}
