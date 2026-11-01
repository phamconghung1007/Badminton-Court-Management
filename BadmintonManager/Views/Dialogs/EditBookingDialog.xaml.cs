using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BadmintonManager.Common;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;

namespace BadmintonManager.Views.Dialogs;

public partial class EditBookingDialog : Window
{
    private readonly int _bookingId;
    private readonly IBookingService _bookingService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IPricingService _pricingService;
    private readonly IDialogService _dialogService;

    private BookingItemDto? _booking;
    private bool _isInitializing = true;

    public EditBookingDialog(
        int bookingId,
        IBookingService bookingService,
        IBranchCourtService branchCourtService,
        IPricingService pricingService,
        IDialogService dialogService)
    {
        InitializeComponent();
        _bookingId = bookingId;
        _bookingService = bookingService;
        _branchCourtService = branchCourtService;
        _pricingService = pricingService;
        _dialogService = dialogService;

        Loaded += EditBookingDialog_Loaded;
    }

    private async void EditBookingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _booking = await _bookingService.GetBookingByIdAsync(_bookingId);
            if (_booking == null)
            {
                _dialogService.ShowError("Không tìm thấy đơn đặt sân.");
                Close();
                return;
            }

            TxtHeaderTitle.Text = $"Chỉnh Sửa Đơn #{_booking.Code}";
            string custName = _booking.CustomerName ?? _booking.GuestName ?? "Khách vãng lai";
            TxtCustomerInfo.Text = $"Khách hàng: {custName} - SĐT: {_booking.GuestPhone ?? "---"}";
            TxtOriginalTime.Text = $"Lịch hiện tại: {_booking.StartTime:dd/MM/yyyy HH:mm} - {_booking.EndTime:HH:mm} ({_booking.CourtName})";

            // Sinh khung giờ 30 phút
            var startOptions = new List<string>();
            var endOptions = new List<string>();
            for (int h = 6; h <= 23; h++)
            {
                startOptions.Add($"{h:D2}:00");
                startOptions.Add($"{h:D2}:30");
            }
            for (int h = 6; h <= 23; h++)
            {
                endOptions.Add($"{h:D2}:30");
                endOptions.Add($"{h + 1:D2}:00");
            }

            CbStartTime.ItemsSource = startOptions;
            CbEndTime.ItemsSource = endOptions;

            // Nạp danh sách sân
            var courts = await _branchCourtService.GetCourtDtosByBranchIdAsync(_booking.BranchId, onlyActive: true);
            CbCourt.ItemsSource = courts;

            // Prefill dữ liệu cũ
            CbCourt.SelectedItem = courts.FirstOrDefault(c => c.Id == _booking.CourtId);
            DpDate.SelectedDate = _booking.StartTime.Date;
            CbStartTime.SelectedItem = _booking.StartTime.ToString("HH:mm");
            CbEndTime.SelectedItem = _booking.EndTime.ToString("HH:mm");

            _isInitializing = false;
            await RecalculatePriceAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Lỗi tải thông tin đơn: {ex.Message}");
            Close();
        }
    }

    private async void Input_Changed(object sender, EventArgs e)
    {
        if (_isInitializing) return;
        await RecalculatePriceAsync();
    }

    private async System.Threading.Tasks.Task RecalculatePriceAsync()
    {
        if (CbCourt.SelectedItem is not CourtDto court ||
            !DpDate.SelectedDate.HasValue ||
            CbStartTime.SelectedItem is not string startStr ||
            CbEndTime.SelectedItem is not string endStr)
        {
            return;
        }

        try
        {
            DateTime date = DpDate.SelectedDate.Value.Date;
            DateTime start = ParseTimeToDateTime(date, startStr);
            DateTime end = ParseTimeToDateTime(date, endStr);

            int durationMinutes = (int)(end - start).TotalMinutes;
            TxtDuration.Text = $"{durationMinutes} phút";

            if (durationMinutes <= 0)
            {
                TxtPricingNote.Text = "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
                return;
            }

            int? customerId = _booking?.CustomerId;
            var pricing = await _pricingService.CalculatePriceAsync(court.Id, start, end, customerId);

            TxtSubTotal.Text = $"{pricing.SubTotal:N0} đ";
            TxtDiscount.Text = $"-{pricing.DiscountAmount:N0} đ";
            TxtNewFinalAmount.Text = $"{pricing.FinalAmount:N0} đ";
            TxtPricingNote.Text = pricing.DiscountAmount > 0 
                ? $"Áp dụng chiết khấu {pricing.TierName} ({pricing.DiscountPercent}%): -{pricing.DiscountAmount:N0} đ" 
                : string.Empty;
        }
        catch (Exception ex)
        {
            TxtPricingNote.Text = ex.Message;
        }
    }

    private static DateTime ParseTimeToDateTime(DateTime date, string timeStr)
    {
        var parts = timeStr.Split(':');
        int hour = int.Parse(parts[0]);
        int minute = int.Parse(parts[1]);

        if (hour == 24 && minute == 0)
        {
            return date.AddDays(1).Date;
        }

        return date.AddHours(hour).AddMinutes(minute);
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (CbCourt.SelectedItem is not CourtDto court)
        {
            _dialogService.ShowWarning("Vui lòng chọn sân thi đấu.");
            return;
        }

        if (!DpDate.SelectedDate.HasValue ||
            CbStartTime.SelectedItem is not string startStr ||
            CbEndTime.SelectedItem is not string endStr)
        {
            _dialogService.ShowWarning("Vui lòng chọn đầy đủ ngày và khung giờ chơi.");
            return;
        }

        DateTime date = DpDate.SelectedDate.Value.Date;
        DateTime start = ParseTimeToDateTime(date, startStr);
        DateTime end = ParseTimeToDateTime(date, endStr);

        try
        {
            await _bookingService.UpdateBookingAsync(new UpdateBookingRequest
            {
                BookingId = _bookingId,
                CourtId = court.Id,
                StartTime = start,
                EndTime = end
            });

            _dialogService.ShowInfo($"Đã cập nhật lịch đặt sân thành công!", "Thành công");
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
