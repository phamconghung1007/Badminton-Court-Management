using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Shared;

namespace BadmintonManager.Views.Pages;

public partial class SchedulePage : UserControl
{
    public SchedulePage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScheduleViewModel vm)
        {
            await vm.InitializeCommand.ExecuteAsync(null);
        }
    }
}