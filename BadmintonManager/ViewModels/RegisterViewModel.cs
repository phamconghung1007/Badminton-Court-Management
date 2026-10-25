using System;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    public event Action? RegistrationSucceeded;
    public event Action? CancelRequested;

    public RegisterViewModel(IAuthService authService, IDialogService dialogService)
    {
        _authService = authService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) ||
            string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone))
        {
            _dialogService.ShowWarning("Vui lòng nhập đầy đủ các thông tin bắt buộc.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            _dialogService.ShowWarning("Mật khẩu xác nhận không khớp.");
            return;
        }

        await RunAsync(async () =>
        {
            await _authService.RegisterCustomerAsync(new RegisterCustomerRequest(
                Username.Trim(),
                Password,
                FullName.Trim(),
                Phone.Trim()
            ));

            _dialogService.ShowInfo("Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bằng tài khoản mới.");
            RegistrationSucceeded?.Invoke();
        }, _dialogService, "Đang xử lý đăng ký...");
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelRequested?.Invoke();
    }
}
