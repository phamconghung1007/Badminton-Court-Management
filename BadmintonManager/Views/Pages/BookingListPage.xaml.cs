using System.Windows;
using System.Windows.Controls;
using BadmintonManager.ViewModels.Shared;

namespace BadmintonManager.Views.Pages;

public partial class BookingListPage : UserControl
{
    public BookingListPage()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BookingListViewModel vm)
        {
            await vm.InitializeCommand.ExecuteAsync(null);
        }
    }
}