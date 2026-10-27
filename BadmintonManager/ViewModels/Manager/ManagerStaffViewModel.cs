using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Manager;

public partial class ManagerStaffViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;

    public ObservableCollection<UserItemDto> StaffList { get; } = new();

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private UserItemDto? _selectedStaff;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Form fields
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm nhân viên mới";

    [ObservableProperty]
    private int? _editingUserId;

    [ObservableProperty]
    private bool _isCreating = true;

    [ObservableProperty]
    private string _formUsername = string.Empty;

    [ObservableProperty]
    private string _formPassword = string.Empty;

    [ObservableProperty]
    private string _formFullName = string.Empty;

    [ObservableProperty]
    private bool _formIsActive = true;

    // Reset password
    [ObservableProperty]
    private bool _isResetPasswordOpen;

    [ObservableProperty]
    private int? _resetUserId;

    [ObservableProperty]
    private string _resetUsername = string.Empty;

    [ObservableProperty]
    private string _newResetPassword = string.Empty;

    public ManagerStaffViewModel(
        IUserService userService,
        CurrentUserContext currentUser,
        IDialogService dialogService)
    {
        _userService = userService;
        _currentUser = currentUser;
        _dialogService = dialogService;

        BranchName = _currentUser.BranchName ?? $"Cơ sở ID #{_currentUser.BranchId}";
    }

    [RelayCommand]
    public async Task LoadStaffAsync()
    {
        if (!_currentUser.BranchId.HasValue) return;

        await RunAsync(async () =>
        {
            // GetUserDtosAsync automatically filters by manager's branch and role=Staff in service!
            var list = await _userService.GetUserDtosAsync(_currentUser.BranchId.Value, UserRole.Staff);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                list = list.Where(u => u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            StaffList.Clear();
            foreach (var u in list)
            {
                StaffList.Add(u);
            }
        }, _dialogService, "Đang tải danh sách nhân viên cơ sở...");
    }

    [RelayCommand]
    private void OpenAddForm()
    {
        EditingUserId = null;
        IsCreating = true;
        FormUsername = string.Empty;
        FormPassword = string.Empty;
        FormFullName = string.Empty;
        FormIsActive = true;
        FormTitle = $"Thêm nhân viên mới cho {BranchName}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void OpenEditForm(UserItemDto? user)
    {
        var target = user ?? SelectedStaff;
        if (target == null) return;

        EditingUserId = target.Id;
        IsCreating = false;
        FormUsername = target.Username;
        FormPassword = string.Empty;
        FormFullName = target.FullName;
        FormIsActive = target.IsActive;
        FormTitle = $"Chỉnh sửa nhân viên: {target.Username}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
    }

    [RelayCommand]
    private async Task SaveStaffAsync()
    {
        if (!_currentUser.BranchId.HasValue)
        {
            _dialogService.ShowError("Tài khoản chưa được phân công chi nhánh quản lý.");
            return;
        }

        if (string.IsNullOrWhiteSpace(FormUsername))
        {
            _dialogService.ShowWarning("Vui lòng nhập tên đăng nhập.");
            return;
        }

        if (IsCreating && string.IsNullOrWhiteSpace(FormPassword))
        {
            _dialogService.ShowWarning("Vui lòng nhập mật khẩu khởi tạo.");
            return;
        }

        await RunAsync(async () =>
        {
            int branchId = _currentUser.BranchId.Value;

            if (IsCreating)
            {
                var req = new CreateUserRequest(FormUsername, FormPassword, FormFullName, UserRole.Staff, branchId);
                await _userService.CreateUserAsync(req);
                _dialogService.ShowInfo("Tạo nhân viên mới thành công!");
            }
            else if (EditingUserId.HasValue)
            {
                var req = new UpdateUserRequest(FormFullName, UserRole.Staff, branchId, FormIsActive);
                await _userService.UpdateUserAsync(EditingUserId.Value, req);
                _dialogService.ShowInfo("Cập nhật thông tin nhân viên thành công!");
            }

            IsFormOpen = false;
            await LoadStaffAsync();
        }, _dialogService, "Đang lưu nhân viên...");
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(UserItemDto? user)
    {
        if (user == null) return;

        string action = user.IsActive ? "khóa tài khoản" : "mở khóa";
        if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn {action} nhân viên '{user.Username}' không?"))
        {
            return;
        }

        await RunAsync(async () =>
        {
            await _userService.ToggleUserStatusAsync(user.Id);
            await LoadStaffAsync();
        }, _dialogService, "Đang xử lý...");
    }

    [RelayCommand]
    private void OpenResetPassword(UserItemDto? user)
    {
        var target = user ?? SelectedStaff;
        if (target == null) return;

        ResetUserId = target.Id;
        ResetUsername = target.Username;
        NewResetPassword = string.Empty;
        IsResetPasswordOpen = true;
    }

    [RelayCommand]
    private void CloseResetPassword()
    {
        IsResetPasswordOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmResetPasswordAsync()
    {
        if (!ResetUserId.HasValue) return;

        if (string.IsNullOrWhiteSpace(NewResetPassword))
        {
            _dialogService.ShowWarning("Vui lòng nhập mật khẩu mới.");
            return;
        }

        await RunAsync(async () =>
        {
            await _userService.ResetPasswordAsync(ResetUserId.Value, NewResetPassword);
            _dialogService.ShowInfo($"Đã đặt lại mật khẩu thành công cho nhân viên '{ResetUsername}'!");
            IsResetPasswordOpen = false;
        }, _dialogService, "Đang đặt lại mật khẩu...");
    }
}
