using System;
using System.Collections.Generic;
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

public record TimeSlotOption(int Minute, string Display);

public partial class AdminPricingViewModel : ViewModelBase
{
    private readonly IPricingService _pricingService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<PriceRuleDto> Rules { get; } = new();

    public List<TimeSlotOption> StartTimeOptions { get; } = new();
    public List<TimeSlotOption> EndTimeOptions { get; } = new();

    // 0 = Tất cả, 1 = Ngày thường, 2 = Cuối tuần
    [ObservableProperty]
    private int _selectedDayTypeFilterIndex = 0;

    [ObservableProperty]
    private PriceRuleDto? _selectedRule;

    // Coverage status banners
    [ObservableProperty]
    private bool _weekdayCoverageValid = false;

    [ObservableProperty]
    private string _weekdayCoverageSummary = string.Empty;

    [ObservableProperty]
    private string _weekdayCoverageErrorsText = string.Empty;

    [ObservableProperty]
    private bool _weekendCoverageValid = false;

    [ObservableProperty]
    private string _weekendCoverageSummary = string.Empty;

    [ObservableProperty]
    private string _weekendCoverageErrorsText = string.Empty;

    // Editor form properties
    [ObservableProperty]
    private bool _isFormOpen;

    [ObservableProperty]
    private string _formTitle = "Thêm khung giá mới";

    [ObservableProperty]
    private int? _editingRuleId;

    [ObservableProperty]
    private DayType _formDayType = DayType.Weekday;

    [ObservableProperty]
    private TimeSlotOption? _selectedStartOption;

    [ObservableProperty]
    private TimeSlotOption? _selectedEndOption;

    [ObservableProperty]
    private string _formPricePerHourText = "80000";

    [ObservableProperty]
    private bool _formIsActive = true;

    [ObservableProperty]
    private string _formErrorMessage = string.Empty;

    public AdminPricingViewModel(IPricingService pricingService, IDialogService dialogService)
    {
        _pricingService = pricingService;
        _dialogService = dialogService;

        InitializeTimeOptions();
    }

    private void InitializeTimeOptions()
    {
        // 06:00 (360) -> 23:30 (1410) for start
        for (int m = 360; m <= 1410; m += 30)
        {
            string display = $"{m / 60:D2}:{m % 60:D2}";
            StartTimeOptions.Add(new TimeSlotOption(m, display));
        }

        // 06:30 (390) -> 24:00 (1440) for end
        for (int m = 390; m <= 1440; m += 30)
        {
            string display = m == 1440 ? "24:00" : $"{m / 60:D2}:{m % 60:D2}";
            EndTimeOptions.Add(new TimeSlotOption(m, display));
        }

        SelectedStartOption = StartTimeOptions.FirstOrDefault(o => o.Minute == 360);
        SelectedEndOption = EndTimeOptions.FirstOrDefault(o => o.Minute == 960);
    }

    [RelayCommand]
    public async Task LoadRulesAsync()
    {
        try
        {
            IsBusy = true;

            DayType? filterType = SelectedDayTypeFilterIndex switch
            {
                1 => DayType.Weekday,
                2 => DayType.Weekend,
                _ => null
            };

            var items = await _pricingService.GetAllPriceRulesAsync(filterType);
            Rules.Clear();
            foreach (var item in items)
            {
                Rules.Add(item);
            }

            // Always validate coverage for both types to keep banners current
            await RefreshCoverageStatusAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tải danh sách bảng giá: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedDayTypeFilterIndexChanged(int value)
    {
        LoadRulesCommand.Execute(null);
    }

    private async Task RefreshCoverageStatusAsync()
    {
        var weekdayRes = await _pricingService.ValidateCoverageAsync(DayType.Weekday);
        WeekdayCoverageValid = weekdayRes.IsValid;
        WeekdayCoverageSummary = weekdayRes.Summary;
        WeekdayCoverageErrorsText = weekdayRes.IsValid ? string.Empty : string.Join(" • ", weekdayRes.Errors);

        var weekendRes = await _pricingService.ValidateCoverageAsync(DayType.Weekend);
        WeekendCoverageValid = weekendRes.IsValid;
        WeekendCoverageSummary = weekendRes.Summary;
        WeekendCoverageErrorsText = weekendRes.IsValid ? string.Empty : string.Join(" • ", weekendRes.Errors);
    }

    [RelayCommand]
    public void OpenAddForm()
    {
        EditingRuleId = null;
        FormTitle = "Thêm khung giá mới";
        FormDayType = SelectedDayTypeFilterIndex == 2 ? DayType.Weekend : DayType.Weekday;
        SelectedStartOption = StartTimeOptions.FirstOrDefault(o => o.Minute == 360);
        SelectedEndOption = EndTimeOptions.FirstOrDefault(o => o.Minute == 960);
        FormPricePerHourText = "80000";
        FormIsActive = true;
        FormErrorMessage = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand]
    public void OpenEditForm(PriceRuleDto? rule)
    {
        if (rule == null) return;

        EditingRuleId = rule.Id;
        FormTitle = $"Chỉnh sửa khung giá #{rule.Id} ({rule.DayTypeName})";
        FormDayType = rule.DayType;
        SelectedStartOption = StartTimeOptions.FirstOrDefault(o => o.Minute == rule.StartMinute);
        SelectedEndOption = EndTimeOptions.FirstOrDefault(o => o.Minute == rule.EndMinute);
        FormPricePerHourText = rule.PricePerHour.ToString("0");
        FormIsActive = rule.IsActive;
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
    public async Task SaveRuleAsync()
    {
        try
        {
            FormErrorMessage = string.Empty;

            if (SelectedStartOption == null || SelectedEndOption == null)
            {
                FormErrorMessage = "Vui lòng chọn đầy đủ giờ bắt đầu và giờ kết thúc.";
                return;
            }

            if (SelectedEndOption.Minute <= SelectedStartOption.Minute)
            {
                FormErrorMessage = "Giờ kết thúc phải sau giờ bắt đầu.";
                return;
            }

            if (!decimal.TryParse(FormPricePerHourText?.Replace(",", "").Replace(".", "").Trim(), out decimal price) || price <= 0)
            {
                FormErrorMessage = "Đơn giá theo giờ phải là số nguyên dương hợp lệ.";
                return;
            }

            var request = new SavePriceRuleRequest
            {
                Id = EditingRuleId,
                DayType = FormDayType,
                StartMinute = SelectedStartOption.Minute,
                EndMinute = SelectedEndOption.Minute,
                PricePerHour = price,
                IsActive = FormIsActive
            };

            await _pricingService.SavePriceRuleAsync(request);

            _dialogService.ShowInfo(
                EditingRuleId.HasValue ? "Đã cập nhật khung giá thành công!" : "Đã thêm khung giá mới thành công!",
                "Thành công");

            IsFormOpen = false;
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task ToggleStatusAsync(PriceRuleDto? rule)
    {
        if (rule == null) return;

        try
        {
            await _pricingService.TogglePriceRuleStatusAsync(rule.Id);
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể thay đổi trạng thái khung giá: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteRuleAsync(PriceRuleDto? rule)
    {
        if (rule == null) return;

        bool confirmed = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn xoá khung giá {rule.TimeRange} ({rule.DayTypeName}) - {rule.PricePerHour:N0} đ/h không?",
            "Xác nhận xoá khung giá");

        if (!confirmed) return;

        try
        {
            await _pricingService.DeletePriceRuleAsync(rule.Id);
            _dialogService.ShowInfo("Đã xoá khung giá thành công.", "Thông báo");
            await LoadRulesAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể xoá khung giá: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task CheckCoverageAsync()
    {
        try
        {
            IsBusy = true;
            await RefreshCoverageStatusAsync();

            bool allValid = WeekdayCoverageValid && WeekendCoverageValid;
            string message = allValid
                ? "Tất cả bảng giá Ngày thường và Cuối tuần đều đã phủ kín 06:00 - 24:00 mà không bị chồng lấn (Đạt tiêu chuẩn BR-10)!"
                : $"Cảnh báo BR-10:\n" +
                  (WeekdayCoverageValid ? "• Ngày thường: ĐẠT CHUẨN\n" : $"• Ngày thường: {WeekdayCoverageErrorsText}\n") +
                  (WeekendCoverageValid ? "• Cuối tuần: ĐẠT CHUẨN" : $"• Cuối tuần: {WeekendCoverageErrorsText}");

            if (allValid)
            {
                _dialogService.ShowInfo(message, "Kiểm tra BR-10 Hoàn tất");
            }
            else
            {
                _dialogService.ShowWarning(message, "Cảnh báo Tiêu chuẩn BR-10");
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Lỗi kiểm tra BR-10: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
