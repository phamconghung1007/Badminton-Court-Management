using System.Windows;

namespace BadmintonManager.Infrastructure;

public interface IDialogService
{
    void ShowInfo(string message, string title = "Thông báo");
    void ShowError(string message, string title = "Lỗi");
    void ShowWarning(string message, string title = "Cảnh báo");
    bool ShowConfirmation(string message, string title = "Xác nhận");
}

public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Thông báo")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title = "Lỗi")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowWarning(string message, string title = "Cảnh báo")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public bool ShowConfirmation(string message, string title = "Xác nhận")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
}
