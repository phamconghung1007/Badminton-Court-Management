using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.DTOs;
using BadmintonManager.Services.Interfaces;

namespace BadmintonManager.Views.Dialogs;

public partial class FixedScheduleDialog : Window
{
    private readonly IFixedScheduleService _fixedScheduleService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly CurrentUserContext _currentUser;
    private readonly int _customerId;
    private readonly string _customerName;

    public class CourtSelectItem
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class DayItem
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public FixedScheduleDialog(
        IFixedScheduleService fixedScheduleService,
        IBranchCourtService branchCourtService,
        CurrentUserContext currentUser,
        int customerId,
        string customerName)
    {
        InitializeComponent();
        _fixedScheduleService = fixedScheduleService;
        _branchCourtService = branchCourtService;
        _currentUser = currentUser;
        _customerId = customerId;
        _customerName = customerName;

        Loaded += FixedScheduleDialog_Loaded;
    }

    private async void FixedScheduleDialog_Loaded(object sender, RoutedEventArgs e)
    {
        TxtCustomerName.Text = $"{_customerName} (ID: #{_customerId})";

        // Nạp các thứ trong tuần
        var days = new List<DayItem>
        {
            new() { Value = 1, Name = "Thứ Hai" },
            new() { Value = 2, Name = "Thứ Ba" },
            new() { Value = 3, Name = "Thứ Tư" },
            new() { Value = 4, Name = "Thứ Năm" },
            new() { Value = 5, Name = "Thứ Sáu" },
            new() { Value = 6, Name = "Thứ Bảy" },
            new() { Value = 0, Name = "Chủ Nhật" }
        };
        CboDayOfWeek.ItemsSource = days;
        CboDayOfWeek.SelectedValue = 1; // Mặc định Thứ Hai

        // Nạp khung giờ 30 phút từ 06:00 đến 24:00
        var times = new List<string>();
        for (int m = 360; m <= 1440; m += 30)
        {
            times.Add($"{m / 60:D2}:{m % 60:D2}");
        }
        CboStartTime.ItemsSource = times;
        CboEndTime.ItemsSource = times;
        CboStartTime.SelectedItem = "18:00";
        CboEndTime.SelectedItem = "20:00";

        // Ngày bắt đầu mặc định là ngày mai
        DpValidFrom.SelectedDate = DateTime.Today.AddDays(1);

        // Nạp danh sách sân
        try
        {
            int? branchId = (_currentUser.IsManager || _currentUser.IsStaff) ? _currentUser.BranchId : null;
            var courts = await _branchCourtService.GetAllCourtDtosAsync(branchId, onlyActive: true);
            var courtItems = courts.Where(c => c.IsActive).Select(c => new CourtSelectItem
            {
                Id = c.Id,
                DisplayName = $"{c.Name} - {c.BranchName}"
            }).ToList();

            CboCourts.ItemsSource = courtItems;
            if (courtItems.Any())
            {
                CboCourts.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách sân: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        if (CboCourts.SelectedItem is not CourtSelectItem selectedCourt)
        {
            MessageBox.Show("Vui lòng chọn sân thi đấu.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CboDayOfWeek.SelectedValue is not int dayOfWeek)
        {
            MessageBox.Show("Vui lòng chọn thứ trong tuần.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CboStartTime.SelectedItem is not string startTimeStr || CboEndTime.SelectedItem is not string endTimeStr)
        {
            MessageBox.Show("Vui lòng chọn giờ bắt đầu và kết thúc.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int startMinute = ParseTimeToMinutes(startTimeStr);
        int endMinute = ParseTimeToMinutes(endTimeStr);

        if (endMinute <= startMinute)
        {
            MessageBox.Show("Giờ kết thúc phải lớn hơn giờ bắt đầu.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DpValidFrom.SelectedDate.HasValue)
        {
            MessageBox.Show("Vui lòng chọn ngày bắt đầu hiệu lực.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int numberOfWeeks = 4;
        if (CboWeeks.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int weeks))
        {
            numberOfWeeks = weeks;
        }

        DateOnly validFrom = DateOnly.FromDateTime(DpValidFrom.SelectedDate.Value);

        try
        {
            BtnConfirm.IsEnabled = false;

            var request = new CreateFixedScheduleRequest
            {
                CustomerId = _customerId,
                CourtId = selectedCourt.Id,
                DayOfWeek = dayOfWeek,
                StartMinute = startMinute,
                EndMinute = endMinute,
                ValidFrom = validFrom,
                NumberOfWeeks = numberOfWeeks
            };

            var result = await _fixedScheduleService.CreateFixedScheduleAsync(request);

            if (result.SkippedDetails.Any())
            {
                string skippedMsg = string.Join("\n", result.SkippedDetails.Select(s => $"• {s.Date:dd/MM/yyyy} ({s.DayOfWeekName}): {s.Reason}"));
                MessageBox.Show(
                    $"{result.Message}\n\nCác tuần bị bỏ qua:\n{skippedMsg}",
                    "Kết quả tạo lịch cố định",
                    MessageBoxButton.OK,
                    result.CreatedCount > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "Tạo lịch cố định thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
        catch (BusinessException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi nghiệp vụ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (ForbiddenException ex)
        {
            MessageBox.Show(ex.Message, "Truy cập bị từ chối", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi không xác định: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnConfirm.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static int ParseTimeToMinutes(string timeStr)
    {
        var parts = timeStr.Split(':');
        return int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
    }
}
