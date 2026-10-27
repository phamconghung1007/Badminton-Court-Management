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

public partial class AdminBranchesViewModel : ViewModelBase
{
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<BranchDto> Branches { get; } = new();

    [ObservableProperty]
    private BranchDto? _selectedBranch;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // Form fields
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm cơ sở mới";

    [ObservableProperty]
    private int? _editingBranchId;

    [ObservableProperty]
    private string _branchName = string.Empty;

    [ObservableProperty]
    private string _branchAddress = string.Empty;

    [ObservableProperty]
    private string _branchPhone = string.Empty;

    [ObservableProperty]
    private bool _branchIsActive = true;

    public AdminBranchesViewModel(IBranchCourtService branchCourtService, IDialogService dialogService)
    {
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadBranchesAsync()
    {
        await RunAsync(async () =>
        {
            var list = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: false);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                list = list.Where(b => b.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || b.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                                    || b.Phone.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            Branches.Clear();
            foreach (var b in list)
            {
                Branches.Add(b);
            }
        }, _dialogService, "Đang tải danh sách cơ sở...");
    }

    [RelayCommand]
    private void OpenAddForm()
    {
        EditingBranchId = null;
        BranchName = string.Empty;
        BranchAddress = string.Empty;
        BranchPhone = string.Empty;
        BranchIsActive = true;
        FormTitle = "Thêm cơ sở chi nhánh mới";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void OpenEditForm(BranchDto? branch)
    {
        var target = branch ?? SelectedBranch;
        if (target == null) return;

        EditingBranchId = target.Id;
        BranchName = target.Name;
        BranchAddress = target.Address;
        BranchPhone = target.Phone;
        BranchIsActive = target.IsActive;
        FormTitle = $"Chỉnh sửa cơ sở: {target.Name}";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CloseForm()
    {
        IsFormOpen = false;
    }

    [RelayCommand]
    private async Task SaveBranchAsync()
    {
        if (string.IsNullOrWhiteSpace(BranchName))
        {
            _dialogService.ShowWarning("Vui lòng nhập tên cơ sở.");
            return;
        }

        await RunAsync(async () =>
        {
            var req = new SaveBranchRequest(BranchName, BranchAddress, BranchPhone, BranchIsActive);
            if (EditingBranchId.HasValue)
            {
                await _branchCourtService.UpdateBranchAsync(EditingBranchId.Value, req);
                _dialogService.ShowInfo("Cập nhật thông tin cơ sở thành công!");
            }
            else
            {
                await _branchCourtService.CreateBranchAsync(req);
                _dialogService.ShowInfo("Thêm cơ sở mới thành công!");
            }

            IsFormOpen = false;
            await LoadBranchesAsync();
        }, _dialogService, "Đang lưu cơ sở...");
    }

    [RelayCommand]
    private async Task ToggleStatusAsync(BranchDto? branch)
    {
        if (branch == null) return;

        string action = branch.IsActive ? "vô hiệu hóa" : "kích hoạt lại";
        if (!_dialogService.ShowConfirmation($"Bạn có chắc chắn muốn {action} cơ sở '{branch.Name}' không?"))
        {
            return;
        }

        await RunAsync(async () =>
        {
            await _branchCourtService.ToggleBranchStatusAsync(branch.Id);
            await LoadBranchesAsync();
        }, _dialogService, "Đang xử lý...");
    }
}
