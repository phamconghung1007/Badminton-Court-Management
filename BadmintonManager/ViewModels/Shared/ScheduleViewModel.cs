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

public partial class ScheduleViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IBookingService _bookingService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private BranchDto? _selectedBranch;
    [ObservableProperty] private bool _canSelectBranch;
    [ObservableProperty] private string _branchNameDisplay = string.Empty;
    [ObservableProperty] private string _scheduleTitle = string.Empty;
    [ObservableProperty] private ScheduleBookingBlock? _selectedBooking;

    public ObservableCollection<BranchDto> Branches { get; } = new();
    public ObservableCollection<ScheduleTimeSlot> TimeSlots { get; } = new();
    public ObservableCollection<ScheduleCourtHeader> Courts { get; } = new();
    public ObservableCollection<ScheduleBookingBlock> Bookings { get; } = new();
    public ObservableCollection<ScheduleEmptySlot> EmptySlots { get; } = new();

    // Event or Action callbacks for Dialogs (Giai đoạn 2)
    public Func<ScheduleEmptySlot, Task>? OnRequestCreateBookingFromSlot { get; set; }
    public Func<ScheduleBookingBlock, Task>? OnRequestViewBookingDetails { get; set; }

    public ScheduleViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IBookingService bookingService,
        IBranchCourtService branchCourtService,
        IDialogService dialogService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _bookingService = bookingService;
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;

        CanSelectBranch = _currentUser.IsAdmin;

        // Sinh 36 time slots cố định (06:00 - 24:00)
        var slots = ScheduleMatrixHelper.GenerateTimeSlots();
        foreach (var s in slots)
        {
            TimeSlots.Add(s);
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

    [RelayCommand]
    public async Task SelectBookingBlockAsync(ScheduleBookingBlock? block)
    {
        if (block == null) return;
        SelectedBooking = block;

        if (OnRequestViewBookingDetails != null)
        {
            await OnRequestViewBookingDetails(block);
        }
        else
        {
            _dialogService.ShowInfo(
                $"Đơn #{block.Code} - {block.CourtName}\n" +
                $"Khách: {block.CustomerDisplayName}\n" +
                $"Thời gian: {block.TimeRangeText}\n" +
                $"Trạng thái: {block.StatusText}\n" +
                $"Tổng tiền: {block.FinalAmount:N0} đ | Đã thanh toán: {block.PaidAmount:N0} đ",
                "Chi tiết lịch đặt sân");
        }
    }

    [RelayCommand]
    public async Task ClickEmptySlotAsync(ScheduleEmptySlot? slot)
    {
        if (slot == null) return;

        if (OnRequestCreateBookingFromSlot != null)
        {
            await OnRequestCreateBookingFromSlot(slot);
        }
        else
        {
            _dialogService.ShowInfo(
                $"Bạn đã chọn {slot.CourtName} vào lúc {slot.TimeRangeText} ngày {slot.SlotStartTime:dd/MM/yyyy}.\n" +
                $"Mở form đặt sân cho khung giờ này.",
                "Đặt sân");
        }
    }
}
