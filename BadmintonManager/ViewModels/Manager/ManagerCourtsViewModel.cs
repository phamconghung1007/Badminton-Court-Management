using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Manager;

public partial class ManagerCourtsViewModel : ViewModelBase
{
    private readonly IBranchCourtService _branchCourtService;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;

    public ObservableCollection<CourtDto> Courts { get; } = new();

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private CourtDto? _selectedCourt;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Form fields
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm sân mới";

    [ObservableProperty]
    private int? _editingCourtId;

    [ObservableProperty]
    private string _courtName = string.Empty;

    [ObservableProperty]
    private string _courtNote = string.Empty;

    [ObservableProperty]
    private bool _courtIsActive = true;

    public ManagerCourtsViewModel(
        IBranchCourtService branchCourtService,
        CurrentUserContext currentUser,
        IDialogService dialogService)
    {
        _branchCourtService = branchCourtService;
        _currentUser = currentUser;
        _dialogService = dialogService;

        BranchName = _currentUser.BranchName ?? $"Cơ sở ID #{_currentUser.BranchId}";
    }

    [RelayCommand]
    public async Task LoadCourtsAsync()
    {
        if (!_currentUser.BranchId.HasValue) return;

        await RunAsync(async () =>
        {
            // GetAllCourtDtosAsync enforces currentUser.BranchId internally!
            var list = await _branchCourtService.GetAllCourtDtosAsync(_currentUser.BranchId.Value, onlyActive: false);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                list = list.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || (c.Note != null && c.Note.Contains(SearchText, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            Courts.Clear();
            foreach (var c in list)
            {
                Courts.Add(c);
            }
        }, _dialogService, "Đang tải danh sách sân cơ sở...");
    }

    [RelayCommand]
    private void OpenAddForm()
    {
        EditingCourtId = null;
        CourtName = string.Empty;
        CourtNote = string.Empty;
        CourtIsActive = true;
        FormTitle = $"Thêm sân mới cho {BranchName}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void OpenEditForm(CourtDto? court)
    {
        var target = court ?? SelectedCourt;
        if (target == null) return;

        EditingCourtId = target.Id;
        CourtName = target.Name;
        CourtNote = target.Note ?? string.Empty;
        CourtIsActive = target.IsActive;
        FormTitle = $"Chỉnh sửa: {target.Name}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
    }

    [RelayCommand]
    private async Task SaveCourtAsync()
    {
        if (!_currentUser.BranchId.HasValue)
        {
            _dialogService.ShowError("Tài khoản chưa được phân công chi nhánh quản lý.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CourtName))
        {
            _dialogService.ShowWarning("Vui lòng nhập tên sân.");
            return;
        }

        await RunAsync(async () =>
        {
            int branchId = _currentUser.BranchId.Value;
            var req = new SaveCourtRequest(branchId, CourtName, CourtIsActive, CourtNote);

            if (EditingCourtId.HasValue)
            {
                await _branchCourtService.UpdateCourtAsync(EditingCourtId.Value, req);
                _dialogService.ShowInfo("Cập nhật thông tin sân thành công!");
            }
            else
            {
                await _branchCourtService.CreateCourtAsync(req);
                _dialogService.ShowInfo("Thêm sân mới thành công!");
            }

            IsFormOpen = false;
            await LoadCourtsAsync();
        }, _dialogService, "Đang lưu sân...");
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(CourtDto? court)
    {
        if (court == null) return;

        string action = court.IsActive ? "tạm dừng hoạt động" : "mở lại";
        if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn {action} sân '{court.Name}' không?"))
        {
            return;
        }

        await RunAsync(async () =>
        {
            await _branchCourtService.ToggleCourtStatusAsync(court.Id);
            await LoadCourtsAsync();
        }, _dialogService, "Đang cập nhật trạng thái...");
    }
}
