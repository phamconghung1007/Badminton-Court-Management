using System.Windows;
using BadmintonManager.ViewModels;

namespace BadmintonManager.Views;

public partial class RegisterWindow : Window
{
    public RegisterWindow(RegisterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }

    private void TxtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            vm.ConfirmPassword = pb.Password;
        }
    }
}
