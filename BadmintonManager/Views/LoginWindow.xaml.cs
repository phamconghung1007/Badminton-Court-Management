using System.Windows;
using BadmintonManager.ViewModels;

namespace BadmintonManager.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }

    private void QuickFillAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string username)
        {
            TxtUsername.Text = username;
            TxtPassword.Password = "123456";
            if (DataContext is LoginViewModel vm)
            {
                vm.Username = username;
                vm.Password = "123456";
            }
        }
    }
}
