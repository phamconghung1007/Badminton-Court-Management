using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
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

public partial class BookingListViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IBookingService _bookingService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;
    private readonly IPaymentService? _paymentService;
    private readonly IInvoiceService? _invoiceService;
    private readonly IInvoicePrinter? _invoicePrinter;
    private readonly IPricingService? _pricingService;

    [ObservableProperty] private DateTime? _filterDate = DateTime.Today;
    [ObservableProperty] private int _selectedStatusFilterIndex = 0; // 0: Tất cả
    [ObservableProperty] private CourtDto? _selectedCourtFilter;
    [ObservableProperty] private string _searchKeyword = string.Empty;
    [ObservableProperty] private BookingItemDto? _selectedBooking;

    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _confirmedCount;
    [ObservableProperty] private int _checkedInCount;
    [ObservableProperty] private decimal _totalRevenue;

    public ObservableCollection<CourtDto> CourtFilters { get; } = new();
    public ObservableCollection<BookingItemDto> Bookings { get; } = new();

    public BookingListViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IBookingService bookingService,
        IBranchCourtService branchCourtService,
        IDialogService dialogService,
        IPaymentService? paymentService = null,
        IInvoiceService? invoiceService = null,
        IInvoicePrinter? invoicePrinter = null,
        IPricingService? pricingService = null)
    {
        _factory = factory;
        _currentUser = currentUser;
        _bookingService = bookingService;
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;
        _paymentService = paymentService;
        _invoiceService = invoiceService;
        _invoicePrinter = invoicePrinter;
        _pricingService = pricingService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await RunAsync(async () =>
        {
            CourtFilters.Clear();
            int? branchId = _currentUser.BranchId;
            var courts = await _branchCourtService.GetAllCourtDtosAsync(branchId, onlyActive: false);
            foreach (var c in courts)
            {
                CourtFilters.Add(c);
            }

            await LoadBookingsInternalAsync();
        }, _dialogService);
    }

    partial void OnFilterDateChanged(DateTime? value)
    {
        _ = LoadBookingsAsync();
    }

    partial void OnSelectedStatusFilterIndexChanged(int value)
    {
        _ = LoadBookingsAsync();
    }

    partial void OnSelectedCourtFilterChanged(CourtDto? value)
    {
        _ = LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        await LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task ClearFiltersAsync()
    {
        FilterDate = null;
        SelectedStatusFilterIndex = 0;
        SelectedCourtFilter = null;
        SearchKeyword = string.Empty;
        await LoadBookingsAsync();
    }

    [RelayCommand]
    public async Task LoadBookingsAsync()
    {
        await RunAsync(LoadBookingsInternalAsync, _dialogService);
    }

    private async Task LoadBookingsInternalAsync()
    {
        int? branchId = _currentUser.BranchId;
        BookingStatus? status = SelectedStatusFilterIndex switch
        {
            1 => BookingStatus.Pending,
            2 => BookingStatus.Confirmed,
            3 => BookingStatus.CheckedIn,
            4 => BookingStatus.Completed,
            5 => BookingStatus.Cancelled,
            6 => BookingStatus.NoShow,
            _ => null
        };

        int? courtId = SelectedCourtFilter?.Id;

        var list = await _bookingService.GetBookingsAsync(branchId, FilterDate, status, courtId);

        // Apply keyword search on client side for code, guest, customer
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            string kw = SearchKeyword.Trim().ToLowerInvariant();
            list = list.Where(b =>
                b.Code.ToLowerInvariant().Contains(kw) ||
                (b.CustomerName != null && b.CustomerName.ToLowerInvariant().Contains(kw)) ||
                (b.GuestName != null && b.GuestName.ToLowerInvariant().Contains(kw)) ||
                (b.GuestPhone != null && b.GuestPhone.Contains(kw)) ||
                b.CourtName.ToLowerInvariant().Contains(kw)
            ).ToList();
        }

        Bookings.Clear();
        foreach (var b in list)
        {
            Bookings.Add(b);
        }

        TotalCount = Bookings.Count;
        PendingCount = Bookings.Count(b => b.Status == BookingStatus.Pending);
        ConfirmedCount = Bookings.Count(b => b.Status == BookingStatus.Confirmed);
        CheckedInCount = Bookings.Count(b => b.Status == BookingStatus.CheckedIn);
        TotalRevenue = Bookings.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.FinalAmount);
    }

    [RelayCommand]
    public async Task ConfirmBookingAsync(BookingItemDto? booking)
    {
        if (booking == null) return;
        try
        {
            await _bookingService.ConfirmBookingAsync(booking.Id);
            _dialogService.ShowInfo($"Đã duyệt đơn đặt sân #{booking.Code} thành công!");
            await LoadBookingsInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task CheckInBookingAsync(BookingItemDto? booking)
    {
        if (booking == null) return;
        try
        {
            await _bookingService.CheckInAsync(booking.Id);
            _dialogService.ShowInfo($"Check-in thành công cho đơn #{booking.Code}!");
            await LoadBookingsInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task CancelBookingAsync(BookingItemDto? booking)
    {
        if (booking == null) return;
        bool confirm = _dialogService.ShowConfirmation(
            $"Bạn có chắc chắn muốn hủy đơn đặt sân #{booking.Code} không?\n" +
            $"Nếu hủy trước 24 giờ, hệ thống sẽ tự động hoàn cọc theo quy định.",
            "Xác nhận hủy đơn");
        if (!confirm) return;

        try
        {
            await _bookingService.CancelBookingAsync(new CancelBookingRequest
            {
                BookingId = booking.Id,
                Reason = "Hủy từ danh sách đặt sân"
            });
            _dialogService.ShowInfo($"Đã hủy đơn đặt sân #{booking.Code} thành công.");
            await LoadBookingsInternalAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task OpenPaymentAsync(BookingItemDto? booking)
    {
        if (booking == null || _paymentService == null || _invoiceService == null || _invoicePrinter == null) return;

        var dlg = new BadmintonManager.Views.Dialogs.PaymentDialog(
            booking.Id,
            _paymentService,
            _invoiceService,
            _invoicePrinter,
            _dialogService);

        if (dlg.ShowDialog() == true)
        {
            await LoadBookingsInternalAsync();
        }
    }

    [RelayCommand]
    public async Task OpenEditBookingAsync(BookingItemDto? booking)
    {
        if (booking == null || _pricingService == null) return;

        var dlg = new BadmintonManager.Views.Dialogs.EditBookingDialog(
            booking.Id,
            _bookingService,
            _branchCourtService,
            _pricingService,
            _dialogService);

        if (dlg.ShowDialog() == true)
        {
            await LoadBookingsInternalAsync();
        }
    }

    [RelayCommand]
    public async Task PreviewInvoiceAsync(BookingItemDto? booking)
    {
        if (booking == null || _invoiceService == null || _invoicePrinter == null) return;

        try
        {
            var invoice = await _invoiceService.GetInvoiceDataAsync(booking.Id);
            var dlg = new BadmintonManager.Views.Dialogs.InvoicePreviewDialog(invoice, _invoicePrinter);
            dlg.ShowDialog();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tạo hóa đơn: {ex.Message}");
        }
    }
}
