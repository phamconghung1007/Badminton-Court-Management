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
}
