using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Admin;

namespace BadmintonManager.Views.Pages;

public partial class AdminPricingPage : UserControl
{
    public AdminPricingPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminPricingViewModel vm)
        {
            await vm.LoadRulesCommand.ExecuteAsync(null);
        }
    }
}
