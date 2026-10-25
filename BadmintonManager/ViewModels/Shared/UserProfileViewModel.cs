using System;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Shared;

public partial class UserProfileViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private UserProfileDto? _profile;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    public UserProfileViewModel(IAuthService authService, IDialogService dialogService)
    {
        _authService = authService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        await RunAsync(async () =>
        {
            Profile = await _authService.GetCurrentUserProfileAsync();
        }, _dialogService, "Đang tải hồ sơ...");
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (Profile == null) return;

        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
        {
            _dialogService.ShowWarning("Vui lòng nhập đầy đủ các ô mật khẩu.");
            return;
        }

        if (NewPassword.Length < 6)
        {
            _dialogService.ShowWarning("Mật khẩu mới phải có ít nhất 6 ký tự.");
            return;
        }

        if (NewPassword != ConfirmNewPassword)
        {
            _dialogService.ShowWarning("Mật khẩu xác nhận không khớp.");
            return;
        }

        await RunAsync(async () =>
        {
            await _authService.ChangePasswordAsync(Profile.UserId, new ChangePasswordRequest(CurrentPassword, NewPassword));
            _dialogService.ShowInfo("Đổi mật khẩu thành công!");
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
        }, _dialogService, "Đang đổi mật khẩu...");
    }
}
