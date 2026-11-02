using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels.Customer;

public partial class CustomerBookingsViewModel : ViewModelBase
{
    private readonly IBookingService _bookingService;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;
    private readonly IBranchCourtService _branchCourtService;

    public ObservableCollection<BookingItemDto> Bookings { get; } = new();
    public ObservableCollection<BookingStatusFilterOption> StatusFilterOptions { get; } = new();

    [ObservableProperty] private BookingStatusFilterOption? _selectedStatusFilter;
    [ObservableProperty] private string _searchKeyword = string.Empty;
    [ObservableProperty] private BookingItemDto? _selectedBooking;
    [ObservableProperty] private int _totalBookingsCount;
    [ObservableProperty] private int _pendingBookingsCount;
    [ObservableProperty] private int _confirmedBookingsCount;
    [ObservableProperty] private int _completedBookingsCount;

    // Reschedule Drawer
    [ObservableProperty] private bool _isRescheduleDrawerOpen;
    [ObservableProperty] private DateTime _newDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _newStartTime = "17:00";
    [ObservableProperty] private string _newEndTime = "19:00";
    [ObservableProperty] private ObservableCollection<string> _timeSlots = new();

    public record BookingStatusFilterOption(string DisplayText, BookingStatus? Value);

    public CustomerBookingsViewModel(
        IBookingService bookingService,
        CurrentUserContext currentUser,
        IDialogService dialogService,
        IBranchCourtService branchCourtService)
    {
        _bookingService = bookingService;
        _currentUser = currentUser;
        _dialogService = dialogService;
        _branchCourtService = branchCourtService;

        InitFilters();
        InitTimeSlots();
    }

    private void InitFilters()
    {
        StatusFilterOptions.Clear();
        StatusFilterOptions.Add(new BookingStatusFilterOption("Tất cả trạng thái", null));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Chờ duyệt / cọc (Pending)", BookingStatus.Pending));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Đã xác nhận (Confirmed)", BookingStatus.Confirmed));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Đang chơi (Checked-in)", BookingStatus.CheckedIn));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Đã hoàn tất (Completed)", BookingStatus.Completed));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Đã hủy (Cancelled)", BookingStatus.Cancelled));
        StatusFilterOptions.Add(new BookingStatusFilterOption("Vắng mặt (No-Show)", BookingStatus.NoShow));

        SelectedStatusFilter = StatusFilterOptions[0];
    }

    private void InitTimeSlots()
    {
        TimeSlots.Clear();
        for (int m = 360; m <= 1440; m += 30)
        {
            int h = m / 60;
            int min = m % 60;
            TimeSlots.Add($"{h:D2}:{min:D2}");
        }
    }

    [RelayCommand]
    public async Task LoadBookingsAsync()
    {
        if (!_currentUser.CustomerId.HasValue) return;
        int customerId = _currentUser.CustomerId.Value;

        await RunAsync(async () =>
        {
            var list = await _bookingService.GetBookingsAsync(
                branchId: null,
                date: null,
                status: SelectedStatusFilter?.Value,
                courtId: null,
                customerId: customerId);

            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLower();
                list = list.Where(b => b.Code.ToLower().Contains(kw) || b.CourtName.ToLower().Contains(kw) || b.BranchName.ToLower().Contains(kw)).ToList();
            }

            Bookings.Clear();
            foreach (var b in list.OrderByDescending(b => b.StartTime))
            {
                Bookings.Add(b);
            }

            TotalBookingsCount = Bookings.Count;
            PendingBookingsCount = Bookings.Count(b => b.Status == BookingStatus.Pending);
            ConfirmedBookingsCount = Bookings.Count(b => b.Status == BookingStatus.Confirmed);
            CompletedBookingsCount = Bookings.Count(b => b.Status == BookingStatus.Completed);
        }, _dialogService);
    }

    [RelayCommand]
    public async Task FilterAsync()
    {
        await LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task CancelBookingAsync(BookingItemDto? booking)
    {
        if (booking == null) return;

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
        {
            _dialogService.ShowWarning("Chỉ có thể hủy đơn đặt sân ở trạng thái Chờ duyệt (Pending) hoặc Đã xác nhận (Confirmed).", "Không thể hủy");
            return;
        }

        double hoursUntilStart = (booking.StartTime - DateTime.Now).TotalHours;
        string refundMessage = hoursUntilStart >= 24
            ? $"• Thời gian đến giờ chơi: {hoursUntilStart:0.#} giờ (>= 24 giờ).\n• Bạn SẼ ĐƯỢC HOÀN TOÀN BỘ TIỀN CỌC: {booking.DepositAmount:N0} VNĐ vào tài khoản theo quy định BR-14."
            : $"• Thời gian đến giờ chơi: {hoursUntilStart:0.#} giờ (< 24 giờ).\n• Theo quy định hủy sân dưới 24 giờ (BR-14 / A9), TIỀN CỌC ({booking.DepositAmount:N0} VNĐ) SẼ KHÔNG ĐƯỢC HOÀN LẠI.";

        bool confirmed = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn hủy đơn đặt sân {booking.Code}?\n\n" +
            $"• Sân: {booking.CourtName} ({booking.BranchName})\n" +
            $"• Giờ chơi: {booking.StartTime:HH:mm} - {booking.EndTime:HH:mm} ({booking.StartTime:dd/MM/yyyy})\n\n" +
            $"{refundMessage}",
            "Xác nhận hủy đơn đặt sân");

        if (!confirmed) return;

        await RunAsync(async () =>
        {
            await _bookingService.CancelBookingAsync(new CancelBookingRequest
            {
                BookingId = booking.Id,
                Reason = "Khách hàng tự hủy trên cổng thông tin"
            });

            _dialogService.ShowInfo($"Đã hủy thành công đơn đặt sân {booking.Code}.", "Hủy thành công");
            await LoadBookingsAsync();
        }, _dialogService);
    }

    [RelayCommand]
    public void OpenRescheduleDrawer(BookingItemDto? booking)
    {
        if (booking == null) return;

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
        {
            _dialogService.ShowWarning("Chỉ có thể đổi lịch cho đơn ở trạng thái Chờ duyệt (Pending) hoặc Đã xác nhận (Confirmed).", "Không thể đổi lịch");
            return;
        }

        double hoursUntilStart = (booking.StartTime - DateTime.Now).TotalHours;
        if (hoursUntilStart < 24)
        {
            _dialogService.ShowWarning($"Thời gian đến giờ chơi chỉ còn {hoursUntilStart:0.#} giờ. Theo quy định A9, chỉ được đổi lịch trước giờ chơi ít nhất 24 giờ.", "Không đủ điều kiện đổi lịch");
            return;
        }

        SelectedBooking = booking;
        NewDate = booking.StartTime.Date;
        NewStartTime = $"{booking.StartTime:HH:mm}";
        NewEndTime = $"{booking.EndTime:HH:mm}";
        IsRescheduleDrawerOpen = true;
    }

    [RelayCommand]
    public void CloseRescheduleDrawer()
    {
        IsRescheduleDrawerOpen = false;
        SelectedBooking = null;
    }

    [RelayCommand]
    public async Task ConfirmRescheduleAsync()
    {
        if (SelectedBooking == null) return;

        if (NewDate.Date < DateTime.Today)
        {
            _dialogService.ShowWarning("Không thể đổi lịch sang ngày trong quá khứ.", "Lỗi thời gian");
            return;
        }

        if (!TryParseTime(NewStartTime, out var startTime) || !TryParseTime(NewEndTime, out var endTime))
        {
            _dialogService.ShowWarning("Khung giờ không hợp lệ.", "Lỗi thời gian");
            return;
        }

        if (endTime <= startTime)
        {
            _dialogService.ShowWarning("Giờ kết thúc phải lớn hơn giờ bắt đầu.", "Lỗi thời gian");
            return;
        }

        DateTime start = NewDate.Date.Add(startTime);
        DateTime end = NewDate.Date.Add(endTime);

        await RunAsync(async () =>
        {
            await _bookingService.UpdateBookingAsync(new UpdateBookingRequest
            {
                BookingId = SelectedBooking.Id,
                CourtId = SelectedBooking.CourtId,
                StartTime = start,
                EndTime = end
            });

            _dialogService.ShowInfo($"Đổi lịch thành công cho đơn {SelectedBooking.Code} sang {NewStartTime} - {NewEndTime} ngày {NewDate:dd/MM/yyyy}!", "Thành công");
            IsRescheduleDrawerOpen = false;
            SelectedBooking = null;
            await LoadBookingsAsync();
        }, _dialogService);
    }

    private static bool TryParseTime(string timeStr, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(timeStr)) return false;

        if (timeStr == "24:00")
        {
            time = TimeSpan.FromHours(24);
            return true;
        }

        return TimeSpan.TryParse(timeStr, out time);
    }
}
