using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Admin;

public partial class AdminCourtsViewModel : ViewModelBase
{
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<BranchDto> FilterBranches { get; } = new();
    public ObservableCollection<BranchDto> FormBranches { get; } = new();
    public ObservableCollection<CourtDto> Courts { get; } = new();

    [ObservableProperty]
    private BranchDto? _selectedFilterBranch;

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
    private BranchDto? _selectedFormBranch;

    [ObservableProperty]
    private string _courtName = string.Empty;

    [ObservableProperty]
    private string _courtNote = string.Empty;

    [ObservableProperty]
    private bool _courtIsActive = true;

    public AdminCourtsViewModel(IBranchCourtService branchCourtService, IDialogService dialogService)
    {
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadCourtsAsync()
    {
        await RunAsync(async () =>
        {
            // 1. Load branches if empty
            if (FilterBranches.Count == 0)
            {
                var branches = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: true);
                FilterBranches.Clear();
                FormBranches.Clear();
                // Add a "Tất cả cơ sở" dummy branch for filter
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

            var list = await _branchCourtService.GetAllCourtDtosAsync(targetBranchId, onlyActive: false);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                list = list.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || c.BranchName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || (c.Note != null && c.Note.Contains(SearchText, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            Courts.Clear();
            foreach (var c in list)
            {
                Courts.Add(c);
            }
        }, _dialogService, "Đang tải danh sách sân...");
    }

    partial void OnSelectedFilterBranchChanged(BranchDto? value)
    {
        if (value != null)
        {
            _ = LoadCourtsAsync();
        }
    }

    [RelayCommand]
    private void OpenAddForm()
    {
        EditingCourtId = null;
        CourtName = string.Empty;
        CourtNote = string.Empty;
        CourtIsActive = true;
        SelectedFormBranch = FormBranches.FirstOrDefault(b => b.Id == SelectedFilterBranch?.Id) ?? FormBranches.FirstOrDefault();
        FormTitle = "Thêm sân cầu lông mới";
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
        SelectedFormBranch = FormBranches.FirstOrDefault(b => b.Id == target.BranchId);
        FormTitle = $"Chỉnh sửa sân: {target.Name}";
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
        if (SelectedFormBranch == null)
        {
            _dialogService.ShowWarning("Vui lòng chọn cơ sở chi nhánh cho sân.");
            return;
        }

        if (string.IsNullOrWhiteSpace(CourtName))
        {
            _dialogService.ShowWarning("Vui lòng nhập tên sân.");
            return;
        }

        await RunAsync(async () =>
        {
            var req = new SaveCourtRequest(SelectedFormBranch.Id, CourtName, CourtIsActive, CourtNote);
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
        if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn {action} sân '{court.Name}' ({court.BranchName}) không?"))
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
