using System;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public event Action? LoginSucceeded;
    public event Action? RegisterRequested;

    public LoginViewModel(IAuthService authService, IDialogService dialogService)
    {
        _authService = authService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            _dialogService.ShowWarning("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
            return;
        }

        await RunAsync(async () =>
        {
            await _authService.LoginAsync(new LoginRequest(Username.Trim(), Password));
            LoginSucceeded?.Invoke();
        }, _dialogService, "Đang xác thực tài khoản...");
    }

    [RelayCommand]
    private void OpenRegister()
    {
        RegisterRequested?.Invoke();
    }
}
