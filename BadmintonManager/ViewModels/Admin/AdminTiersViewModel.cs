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

public partial class AdminTiersViewModel : ViewModelBase
{
    private readonly IMembershipService _membershipService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<MembershipTierDto> Tiers { get; } = new();

    [ObservableProperty]
    private MembershipTierDto? _selectedTier;

    // Form fields
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm hạng hội viên mới";

    [ObservableProperty]
    private int? _editingTierId;

    [ObservableProperty]
    private string _formName = string.Empty;

    [ObservableProperty]
    private string _formMinPointsText = "0";

    [ObservableProperty]
    private string _formDiscountPercentText = "0";

    [ObservableProperty]
    private int _formSortOrder = 1;

    [ObservableProperty]
    private string _formErrorMessage = string.Empty;

    public AdminTiersViewModel(IMembershipService membershipService, IDialogService dialogService)
    {
        _membershipService = membershipService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadTiersAsync()
    {
        try
        {
            IsBusy = true;

            var items = await _membershipService.GetAllTiersWithStatsAsync();
            Tiers.Clear();
            foreach (var item in items)
            {
                Tiers.Add(item);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tải danh sách hạng hội viên: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void OpenAddForm()
    {
        EditingTierId = null;
        FormTitle = "Thêm hạng hội viên mới";
        FormName = string.Empty;
        FormMinPointsText = "0";
        FormDiscountPercentText = "0";
        FormSortOrder = (Tiers.Count > 0 ? Tiers.Max(t => t.SortOrder) + 1 : 1);
        FormErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(MembershipTierDto? tier)
    {
        if (tier == null) return;

        EditingTierId = tier.Id;
        FormTitle = $"Chỉnh sửa chính sách hạng: {tier.Name}";
        FormName = tier.Name;
        FormMinPointsText = tier.MinPoints.ToString();
        FormDiscountPercentText = tier.DiscountPercent.ToString("0.##");
        FormSortOrder = tier.SortOrder;
        FormErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void CloseForm()
    {
        IsFormOpen = false;
        FormErrorMessage = string.Empty;
    }

    [RelayCommand]
    public async Task SaveTierAsync()
    {
        try
        {
            FormErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FormName))
            {
                FormErrorMessage = "Tên hạng hội viên không được để trống.";
                return;
            }

            if (!int.TryParse(FormMinPointsText?.Trim(), out int minPoints) || minPoints < 0)
            {
                FormErrorMessage = "Điểm tối thiểu phải là số nguyên không âm (>= 0).";
                return;
            }

            if (!decimal.TryParse(FormDiscountPercentText?.Trim(), out decimal discount) || discount < 0 || discount > 100)
            {
                FormErrorMessage = "Mức giảm giá phải nằm trong khoảng từ 0% đến 100%.";
                return;
            }

            var request = new SaveMembershipTierRequest
            {
                Id = EditingTierId,
                Name = FormName.Trim(),
                MinPoints = minPoints,
                DiscountPercent = discount,
                SortOrder = FormSortOrder
            };

            await _membershipService.SaveTierAsync(request);

            _dialogService.ShowInfo(
                EditingTierId.HasValue ? "Đã cập nhật chính sách hạng thành công!" : "Đã thêm hạng hội viên mới thành công!",
                "Thành công");

            IsFormOpen = false;
            await LoadTiersAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task DeleteTierAsync(MembershipTierDto? tier)
    {
        if (tier == null) return;

        if (tier.CustomerCount > 0)
        {
            _dialogService.ShowWarning(
                $"Không thể xoá hạng '{tier.Name}' vì hiện đang có {tier.CustomerCount} khách hàng đang thuộc hạng này.",
                "Thao tác không được phép");
            return;
        }

        bool confirmed = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn xoá hạng hội viên '{tier.Name}' không?",
            "Xác nhận xoá hạng hội viên");

        if (!confirmed) return;

        try
        {
            await _membershipService.DeleteTierAsync(tier.Id);
            _dialogService.ShowInfo($"Đã xoá hạng '{tier.Name}'.", "Thông báo");
            await LoadTiersAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Lỗi khi xoá hạng hội viên: {ex.Message}");
        }
    }
}
