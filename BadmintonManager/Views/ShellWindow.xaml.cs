using System.Windows;
using BadmintonManager.ViewModels;

namespace BadmintonManager.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
