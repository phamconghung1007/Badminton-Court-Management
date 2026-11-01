using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.ViewModels.Shared;

public partial class ScheduleViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IBookingService _bookingService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IPricingService _pricingService;
    private readonly ICustomerService _customerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private BranchDto? _selectedBranch;
    [ObservableProperty] private bool _canSelectBranch;
    [ObservableProperty] private string _branchNameDisplay = string.Empty;
    [ObservableProperty] private string _scheduleTitle = string.Empty;
    [ObservableProperty] private ScheduleBookingBlock? _selectedBooking;

    // Collections for Grid
    public ObservableCollection<BranchDto> Branches { get; } = new();
    public ObservableCollection<ScheduleTimeSlot> TimeSlots { get; } = new();
    public ObservableCollection<ScheduleCourtHeader> Courts { get; } = new();
    public ObservableCollection<ScheduleBookingBlock> Bookings { get; } = new();
    public ObservableCollection<ScheduleEmptySlot> EmptySlots { get; } = new();

    public double CanvasWidth => Math.Max(Courts.Count, 1) * ScheduleCourtHeader.DefaultWidth;
    public double CanvasHeight => ScheduleMatrixHelper.TotalSlots * ScheduleBookingBlock.SlotHeight;

    // Time Dropdown Options
    public List<string> StartTimeOptions { get; } = new();
    public List<string> EndTimeOptions { get; } = new();

    #region Create Booking Form Properties

    [ObservableProperty] private bool _isBookingFormOpen;
    [ObservableProperty] private DateTime _formDate = DateTime.Today;
    [ObservableProperty] private ScheduleCourtHeader? _formSelectedCourt;
    [ObservableProperty] private string _formStartTimeStr = "08:00";
    [ObservableProperty] private string _formEndTimeStr = "09:00";
    [ObservableProperty] private int _formDurationMinutes = 60;
    [ObservableProperty] private bool _formIsWalkIn = true;
    [ObservableProperty] private bool _formIsMember;
    [ObservableProperty] private string _formGuestName = string.Empty;
    [ObservableProperty] private string _formGuestPhone = string.Empty;
    [ObservableProperty] private CustomerDto? _formSelectedCustomer;
    [ObservableProperty] private string _customerSearchKeyword = string.Empty;

    public ObservableCollection<CustomerDto> FoundCustomers { get; } = new();

    // Live Pricing Preview
    [ObservableProperty] private decimal _formSubTotal;
    [ObservableProperty] private decimal _formDiscountAmount;
    [ObservableProperty] private decimal _formFinalAmount;
    [ObservableProperty] private decimal _formDepositAmount;
    [ObservableProperty] private string _formPricingNote = string.Empty;
    [ObservableProperty] private PriceCalculationResult? _formPricingResult;

    #endregion

    #region Booking Details Properties

    [ObservableProperty] private bool _isDetailsOpen;
    [ObservableProperty] private BookingItemDto? _detailedBooking;

    #endregion

    public ScheduleViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IBookingService bookingService,
        IBranchCourtService branchCourtService,
        IPricingService pricingService,
        ICustomerService customerService,
        IDialogService dialogService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _bookingService = bookingService;
        _branchCourtService = branchCourtService;
        _pricingService = pricingService;
        _customerService = customerService;
        _dialogService = dialogService;

        CanSelectBranch = _currentUser.IsAdmin;

        // Sinh 36 time slots cố định (06:00 - 24:00)
        var slots = ScheduleMatrixHelper.GenerateTimeSlots();
        foreach (var s in slots)
        {
            TimeSlots.Add(s);
            StartTimeOptions.Add(s.TimeDisplay);
        }

        // EndTime có thêm mốc "24:00"
        for (int i = 1; i <= ScheduleMatrixHelper.TotalSlots; i++)
        {
            int end = ScheduleMatrixHelper.OpenMinute + i * ScheduleMatrixHelper.SlotInterval;
            int h = end / 60;
            int m = end % 60;
            string endStr = (h == 24 && m == 0) ? "24:00" : $"{h:D2}:{m:D2}";
            EndTimeOptions.Add(endStr);
        }
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            Branches.Clear();
            var branchList = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: true);
            foreach (var b in branchList)
            {
                Branches.Add(b);
            }

            if (_currentUser.BranchId.HasValue)
            {
                SelectedBranch = Branches.FirstOrDefault(b => b.Id == _currentUser.BranchId.Value);
            }
            else
            {
                SelectedBranch = Branches.FirstOrDefault();
            }
            BranchNameDisplay = SelectedBranch?.Name ?? string.Empty;

            await LoadScheduleDataInternalAsync();
        }, _dialogService);
    }

    partial void OnSelectedBranchChanged(BranchDto? value)
    {
        if (value != null)
        {
            BranchNameDisplay = value.Name;
            _ = LoadScheduleDataAsync();
        }
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        _ = LoadScheduleDataAsync();
    }

    [RelayCommand]
    public async Task PreviousDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(-1);
    }

    [RelayCommand]
    public async Task NextDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(1);
    }

    [RelayCommand]
    public async Task TodayAsync()
    {
        SelectedDate = DateTime.Today;
    }

    [RelayCommand]
    public async Task RefreshScheduleAsync()
    {
        await LoadScheduleDataAsync();
    }

    public async Task LoadScheduleDataAsync()
    {
        await RunAsync(LoadScheduleDataInternalAsync, _dialogService);
    }

    private async Task LoadScheduleDataInternalAsync()
    {
        if (SelectedBranch == null) return;
        int branchId = SelectedBranch.Id;
        DateTime date = SelectedDate.Date;

        string dayOfWeekStr = date.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thứ Hai",
            DayOfWeek.Tuesday => "Thứ Ba",
            DayOfWeek.Wednesday => "Thứ Tư",
            DayOfWeek.Thursday => "Thứ Năm",
            DayOfWeek.Friday => "Thứ Sáu",
            DayOfWeek.Saturday => "Thứ Bảy",
            DayOfWeek.Sunday => "Chủ Nhật",
            _ => string.Empty
        };

        ScheduleTitle = $"{SelectedBranch.Name} - {dayOfWeekStr}, {date:dd/MM/yyyy}";

        // 1. Nạp danh sách sân của cơ sở
        var courtDtos = await _branchCourtService.GetCourtDtosByBranchIdAsync(branchId, onlyActive: true);
        Courts.Clear();
        for (int i = 0; i < courtDtos.Count; i++)
        {
            Courts.Add(new ScheduleCourtHeader
            {
                CourtId = courtDtos[i].Id,
                CourtName = courtDtos[i].Name,
                ColumnIndex = i,
                IsActive = courtDtos[i].IsActive
            });
        }
        OnPropertyChanged(nameof(CanvasWidth));

        // 2. Nạp booking trong ngày của cơ sở
        var bookingDtos = await _bookingService.GetBookingsAsync(branchId, date);

        // 3. Map booking sang khối ma trận ScheduleBookingBlock
        Bookings.Clear();
        var courtHeaderDict = Courts.ToDictionary(c => c.CourtId);

        foreach (var b in bookingDtos)
        {
            if (courtHeaderDict.TryGetValue(b.CourtId, out var header))
            {
                var block = ScheduleMatrixHelper.BuildBookingBlock(b, header.ColumnIndex);
                Bookings.Add(block);
            }
        }

        // 4. Sinh danh sách các ô trống (ScheduleEmptySlot) để click đặt sân
        EmptySlots.Clear();
        var emptyList = ScheduleMatrixHelper.BuildEmptySlots(Courts.ToList(), Bookings.ToList(), date);
        foreach (var empty in emptyList)
        {
            EmptySlots.Add(empty);
        }
    }

    #region Create Booking Form Actions

    [RelayCommand]
    public async Task OpenCreateBookingFormAsync(ScheduleEmptySlot? slot = null)
    {
        IsDetailsOpen = false;
        FormDate = SelectedDate;

        if (slot != null)
        {
            FormSelectedCourt = Courts.FirstOrDefault(c => c.CourtId == slot.CourtId)
                ?? new ScheduleCourtHeader { CourtId = slot.CourtId, CourtName = slot.CourtName, ColumnIndex = slot.ColumnIndex };
            FormStartTimeStr = slot.SlotStartTime.ToString("HH:mm");

            DateTime defaultEnd = slot.SlotStartTime.AddHours(1);
            if (defaultEnd > slot.SlotStartTime.Date.AddDays(1))
            {
                defaultEnd = slot.SlotStartTime.Date.AddDays(1);
            }
            FormEndTimeStr = (defaultEnd.Hour == 0 && defaultEnd.Minute == 0) ? "24:00" : defaultEnd.ToString("HH:mm");
        }
        else
        {
            FormSelectedCourt = Courts.FirstOrDefault();
            FormStartTimeStr = "08:00";
            FormEndTimeStr = "09:00";
        }

        FormIsWalkIn = false;
        FormGuestName = string.Empty;
        FormGuestPhone = string.Empty;
        FormSelectedCustomer = null;
        CustomerSearchKeyword = string.Empty;
        FoundCustomers.Clear();

        // Nạp danh sách khách hàng ban đầu
        var initialCustomers = await _customerService.SearchCustomersAsync(null);
        foreach (var c in initialCustomers.Take(10))
        {
            FoundCustomers.Add(c);
        }

        IsBookingFormOpen = true;
        await RecalculatePricePreviewAsync();
    }

    [RelayCommand]
    public void CloseBookingForm()
    {
        IsBookingFormOpen = false;
    }

    partial void OnFormSelectedCourtChanged(ScheduleCourtHeader? value)
    {
        _ = RecalculatePricePreviewAsync();
    }

    partial void OnFormStartTimeStrChanged(string value)
    {
        UpdateFormDuration();
        _ = RecalculatePricePreviewAsync();
    }

    partial void OnFormEndTimeStrChanged(string value)
    {
        UpdateFormDuration();
        _ = RecalculatePricePreviewAsync();
    }

    partial void OnFormSelectedCustomerChanged(CustomerDto? value)
    {
        _ = RecalculatePricePreviewAsync();
    }

    partial void OnFormIsWalkInChanged(bool value)
    {
        if (FormIsMember == value)
        {
            FormIsMember = !value;
        }
        _ = RecalculatePricePreviewAsync();
    }

    partial void OnFormIsMemberChanged(bool value)
    {
        if (FormIsWalkIn == value)
        {
            FormIsWalkIn = !value;
        }
    }

    private void UpdateFormDuration()
    {
        if (string.IsNullOrEmpty(FormStartTimeStr) || string.IsNullOrEmpty(FormEndTimeStr)) return;
        try
        {
            DateTime start = ParseTimeToDateTime(FormDate, FormStartTimeStr);
            DateTime end = ParseTimeToDateTime(FormDate, FormEndTimeStr);
            FormDurationMinutes = (int)(end - start).TotalMinutes;
        }
        catch
        {
            FormDurationMinutes = 0;
        }
    }

    [RelayCommand]
    public async Task SearchCustomersAsync()
    {
        var list = await _customerService.SearchCustomersAsync(CustomerSearchKeyword);
        FoundCustomers.Clear();
        foreach (var c in list)
        {
            FoundCustomers.Add(c);
        }
    }

    [RelayCommand]
    public async Task RecalculatePricePreviewAsync()
    {
        if (FormSelectedCourt == null || string.IsNullOrEmpty(FormStartTimeStr) || string.IsNullOrEmpty(FormEndTimeStr))
        {
            FormSubTotal = 0;
            FormDiscountAmount = 0;
            FormFinalAmount = 0;
            FormDepositAmount = 0;
            FormPricingNote = string.Empty;
            return;
        }

        try
        {
            DateTime start = ParseTimeToDateTime(FormDate, FormStartTimeStr);
            DateTime end = ParseTimeToDateTime(FormDate, FormEndTimeStr);

            if (end <= start)
            {
                FormPricingNote = "Giờ kết thúc phải lớn hơn giờ bắt đầu.";
                return;
            }

            int? customerId = FormIsWalkIn ? null : FormSelectedCustomer?.Id;
            var pricing = await _pricingService.CalculatePriceAsync(FormSelectedCourt.CourtId, start, end, customerId);

            FormPricingResult = pricing;
            FormSubTotal = pricing.SubTotal;
            FormDiscountAmount = pricing.DiscountAmount;
            FormFinalAmount = pricing.FinalAmount;
            FormDepositAmount = pricing.DepositAmount;

            if (pricing.DiscountPercent > 0)
            {
                FormPricingNote = $"Áp dụng chiết khấu hội viên {pricing.TierName} ({pricing.DiscountPercent:G29}%): giảm {pricing.DiscountAmount:N0} đ";
            }
            else
            {
                FormPricingNote = "Không áp dụng giảm giá (Giá chuẩn).";
            }
        }
        catch (Exception ex)
        {
            FormPricingNote = ex.Message;
        }
    }

    [RelayCommand]
    public async Task ConfirmCreateBookingAsync()
    {
        if (FormSelectedCourt == null)
        {
            _dialogService.ShowError("Vui lòng chọn sân.");
            return;
        }

        if (string.IsNullOrEmpty(FormStartTimeStr) || string.IsNullOrEmpty(FormEndTimeStr))
        {
            _dialogService.ShowError("Vui lòng chọn thời gian bắt đầu và kết thúc.");
            return;
        }

        DateTime start = ParseTimeToDateTime(FormDate, FormStartTimeStr);
        DateTime end = ParseTimeToDateTime(FormDate, FormEndTimeStr);

        if (end <= start)
        {
            _dialogService.ShowError("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
            return;
        }

        if (!FormIsWalkIn && FormSelectedCustomer == null)
        {
            _dialogService.ShowError("Vui lòng chọn khách hàng hoặc chọn 'Khách vãng lai'.");
            return;
        }

        var request = new CreateBookingRequest
        {
            CourtId = FormSelectedCourt.CourtId,
            BookingType = FormIsWalkIn ? BookingType.WalkIn : BookingType.Single,
            CustomerId = FormIsWalkIn ? null : FormSelectedCustomer?.Id,
            GuestName = FormIsWalkIn ? (string.IsNullOrWhiteSpace(FormGuestName) ? "Khách vãng lai" : FormGuestName.Trim()) : null,
            GuestPhone = FormIsWalkIn ? FormGuestPhone?.Trim() : null,
            StartTime = start,
            EndTime = end
        };

        try
        {
            var result = await _bookingService.CreateBookingAsync(request);

            _dialogService.ShowInfo(
                $"Đặt sân thành công!\n" +
                $"• Mã đặt sân: {result.Code}\n" +
                $"• Sân: {result.CourtName}\n" +
                $"• Thời gian: {result.StartTime:HH:mm} - {result.EndTime:HH:mm}\n" +
                $"• Tổng tiền: {result.FinalAmount:N0} đ (Cọc: {result.DepositAmount:N0} đ)\n" +
                $"• Trạng thái: {result.Status}",
                "Thành công");

            IsBookingFormOpen = false;
            await LoadScheduleDataInternalAsync();
        }
        catch (BusinessException ex)
        {
            _dialogService.ShowError(ex.Message, "Không thể đặt sân");
        }
        catch (ForbiddenException ex)
        {
            _dialogService.ShowWarning(ex.Message, "Từ chối quyền truy cập");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Đã xảy ra lỗi: {ex.Message}", "Lỗi hệ thống");
        }
    }

    #endregion

    #region Booking Details Actions

    [RelayCommand]
    public async Task OpenBookingDetailsAsync(ScheduleBookingBlock? block)
    {
        if (block == null) return;
        SelectedBooking = block;

        try
        {
            DetailedBooking = await _bookingService.GetBookingByIdAsync(block.BookingId);
            IsBookingFormOpen = false;
            IsDetailsOpen = true;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tải chi tiết đặt sân: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CloseBookingDetails()
    {
        IsDetailsOpen = false;
    }

    [RelayCommand]
    public async Task ConfirmBookingFromDetailsAsync()
    {
        if (DetailedBooking == null) return;
        try
        {
            await _bookingService.ConfirmBookingAsync(DetailedBooking.Id);
            _dialogService.ShowInfo($"Đã xác nhận đơn đặt sân #{DetailedBooking.Code} thành công!");
            DetailedBooking = await _bookingService.GetBookingByIdAsync(DetailedBooking.Id);
            await LoadScheduleDataInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task CheckInBookingFromDetailsAsync()
    {
        if (DetailedBooking == null) return;
        try
        {
            await _bookingService.CheckInAsync(DetailedBooking.Id);
            _dialogService.ShowInfo($"Check-in thành công cho đơn #{DetailedBooking.Code}!");
            DetailedBooking = await _bookingService.GetBookingByIdAsync(DetailedBooking.Id);
            await LoadScheduleDataInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task CancelBookingFromDetailsAsync()
    {
        if (DetailedBooking == null) return;
        bool confirm = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn hủy đơn đặt sân #{DetailedBooking.Code} không?",
            "Xác nhận hủy đơn");
        if (!confirm) return;

        try
        {
            await _bookingService.CancelBookingAsync(new CancelBookingRequest
            {
                BookingId = DetailedBooking.Id,
                Reason = "Nhân viên hủy theo yêu cầu"
            });
            _dialogService.ShowInfo($"Đã hủy đơn đặt sân #{DetailedBooking.Code} thành công.");
            IsDetailsOpen = false;
            await LoadScheduleDataInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    #endregion

    private static DateTime ParseTimeToDateTime(DateTime date, string timeStr)
    {
        if (timeStr == "24:00")
        {
            return date.Date.AddDays(1);
        }
        var parts = timeStr.Split(':');
        int h = int.Parse(parts[0]);
        int m = int.Parse(parts[1]);
        return date.Date.AddHours(h).AddMinutes(m);
    }
}
