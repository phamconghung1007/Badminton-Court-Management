using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Admin;

public partial class AdminUsersViewModel : ViewModelBase
{
    private readonly IUserService _userService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<BranchDto> FilterBranches { get; } = new();
    public ObservableCollection<BranchDto> FormBranches { get; } = new();
    public ObservableCollection<UserItemDto> Users { get; } = new();

    public ObservableCollection<UserRole> AvailableRoles { get; } = new()
    {
        UserRole.Admin,
        UserRole.Manager,
        UserRole.Staff
    };

    [ObservableProperty]
    private BranchDto? _selectedFilterBranch;

    [ObservableProperty]
    private UserRole? _selectedFilterRole;

    [ObservableProperty]
    private UserItemDto? _selectedUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Form fields
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm tài khoản nhân sự";

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
    private UserRole _formRole = UserRole.Staff;

    [ObservableProperty]
    private BranchDto? _selectedFormBranch;

    [ObservableProperty]
    private bool _formIsActive = true;

    // Reset password dialog
    [ObservableProperty]
    private bool _isResetPasswordOpen;

    [ObservableProperty]
    private int? _resetUserId;

    [ObservableProperty]
    private string _resetUsername = string.Empty;

    [ObservableProperty]
    private string _newResetPassword = string.Empty;

    public AdminUsersViewModel(
        IUserService userService,
        IBranchCourtService branchCourtService,
        IDialogService dialogService)
    {
        _userService = userService;
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        await RunAsync(async () =>
        {
            if (FilterBranches.Count == 0)
            {
                var branches = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: true);
                FilterBranches.Clear();
                FormBranches.Clear();

                FilterBranches.Add(new BranchDto(0, "Tất cả cơ sở", "", "", true, 0, DateTime.MinValue));
                foreach (var b in branches)
                {
                    FilterBranches.Add(b);
                    FormBranches.Add(b);
                }
                SelectedFilterBranch = FilterBranches[0];
            }

            int? targetBranchId = (SelectedFilterBranch != null && SelectedFilterBranch.Id > 0)
                ? SelectedFilterBranch.Id
                : null;

            var list = await _userService.GetUserDtosAsync(targetBranchId, SelectedFilterRole);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                list = list.Where(u => u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || (u.BranchName != null && u.BranchName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            Users.Clear();
            foreach (var u in list)
            {
                Users.Add(u);
            }
        }, _dialogService, "Đang tải danh sách người dùng...");
    }

    partial void OnSelectedFilterBranchChanged(BranchDto? value)
    {
        if (value != null)
        {
            _ = LoadUsersAsync();
        }
    }

    partial void OnSelectedFilterRoleChanged(UserRole? value)
    {
        _ = LoadUsersAsync();
    }

    [RelayCommand]
    private void OpenAddForm()
    {
        EditingUserId = null;
        IsCreating = true;
        FormUsername = string.Empty;
        FormPassword = string.Empty;
        FormFullName = string.Empty;
        FormRole = UserRole.Staff;
        SelectedFormBranch = FormBranches.FirstOrDefault();
        FormIsActive = true;
        FormTitle = "Tạo tài khoản nhân sự mới";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void OpenEditForm(UserItemDto? user)
    {
        var target = user ?? SelectedUser;
        if (target == null) return;

        EditingUserId = target.Id;
        IsCreating = false;
        FormUsername = target.Username;
        FormPassword = string.Empty;
        FormFullName = target.FullName;
        FormRole = target.Role;
        SelectedFormBranch = FormBranches.FirstOrDefault(b => b.Id == target.BranchId);
        FormIsActive = target.IsActive;
        FormTitle = $"Chỉnh sửa tài khoản: {target.Username}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
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

        if ((FormRole == UserRole.Manager || FormRole == UserRole.Staff) && SelectedFormBranch == null)
        {
            _dialogService.ShowWarning("Tài khoản Quản lý (Manager) và Nhân viên (Staff) bắt buộc phải gắn với chi nhánh cơ sở.");
            return;
        }

        await RunAsync(async () =>
        {
            int? branchId = (FormRole == UserRole.Admin) ? null : SelectedFormBranch?.Id;

            if (IsCreating)
            {
                var req = new CreateUserRequest(FormUsername, FormPassword, FormFullName, FormRole, branchId);
                await _userService.CreateUserAsync(req);
                _dialogService.ShowInfo("Tạo tài khoản mới thành công!");
            }
            else if (EditingUserId.HasValue)
            {
                var req = new UpdateUserRequest(FormFullName, FormRole, branchId, FormIsActive);
                await _userService.UpdateUserAsync(EditingUserId.Value, req);
                _dialogService.ShowInfo("Cập nhật thông tin tài khoản thành công!");
            }

            IsFormOpen = false;
            await LoadUsersAsync();
        }, _dialogService, "Đang lưu tài khoản...");
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(UserItemDto? user)
    {
        if (user == null) return;

        string action = user.IsActive ? "khóa" : "mở khóa";
        if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn {action} tài khoản '{user.Username}' không?"))
        {
            return;
        }

        await RunAsync(async () =>
        {
            await _userService.ToggleUserStatusAsync(user.Id);
            await LoadUsersAsync();
        }, _dialogService, "Đang xử lý...");
    }

    [RelayCommand]
    private void OpenResetPassword(UserItemDto? user)
    {
        var target = user ?? SelectedUser;
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
            _dialogService.ShowInfo($"Đã đặt lại mật khẩu thành công cho tài khoản '{ResetUsername}'!");
            IsResetPasswordOpen = false;
        }, _dialogService, "Đang đặt lại mật khẩu...");
    }
}
