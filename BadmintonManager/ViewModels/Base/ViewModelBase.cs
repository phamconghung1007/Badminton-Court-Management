using System;
using System.Threading.Tasks;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BadmintonManager.ViewModels.Base;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    protected async Task RunAsync(Func<Task> action, IDialogService? dialog = null, string? busyMessage = null)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BusyMessage = busyMessage;
            await action();
        }
        catch (BusinessException ex)
        {
            dialog?.ShowError(ex.Message);
        }
        catch (ForbiddenException ex)
        {
            dialog?.ShowWarning(ex.Message);
        }
        catch (Exception)
        {
            // Log error here if logger is available, show user-friendly message
            dialog?.ShowError("Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }
}
