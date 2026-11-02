using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using BadmintonManager.ViewModels.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class RecordingDialogService : IDialogService
{
    public string? LastInfoMessage { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string? LastWarningMessage { get; private set; }

    public void ShowInfo(string message, string title = "Thông báo") => LastInfoMessage = message;
    public void ShowWarning(string message, string title = "Cảnh báo") => LastWarningMessage = message;
    public void ShowError(string message, string title = "Lỗi") => LastErrorMessage = message;
    public bool ShowConfirmation(string message, string title = "Xác nhận") => true;
}

public class ScheduleServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser, BookingService bookingService, BranchCourtService branchCourtService, PricingService pricingService, CustomerService customerService, RecordingDialogService dialogService) CreateTestSetup(
        UserRole role = UserRole.Staff,
        int? branchId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        currentUser.SetUser(1, "testuser", "Test User", role, branchId, "Cơ sở Cầu Giấy");

        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, currentUser);
        var bookingService = new BookingService(factory, currentUser, pricingService, settingService);
        var branchCourtService = new BranchCourtService(factory, currentUser);
        var customerService = new CustomerService(factory, currentUser);
        var dialogService = new RecordingDialogService();

        return (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
    }

    #region ScheduleMatrixHelper Unit Tests

    [Fact]
    public void GenerateTimeSlots_GeneratesExactly36Slots_Covering0600To2400()
    {
        // Act
        var slots = ScheduleMatrixHelper.GenerateTimeSlots();

        // Assert
        slots.Should().HaveCount(36);
        slots[0].SlotIndex.Should().Be(0);
        slots[0].StartMinute.Should().Be(360); // 06:00
        slots[0].EndMinute.Should().Be(390);   // 06:30
        slots[0].TimeDisplay.Should().Be("06:00");
        slots[0].TimeRangeDisplay.Should().Be("06:00 - 06:30");

        slots[35].SlotIndex.Should().Be(35);
        slots[35].StartMinute.Should().Be(1410); // 23:30
        slots[35].EndMinute.Should().Be(1440);   // 24:00
        slots[35].TimeDisplay.Should().Be("23:30");
        slots[35].TimeRangeDisplay.Should().Be("23:30 - 24:00");
    }

    [Fact]
    public void CalculateRowIndex_CalculatesCorrectSlotIndex()
    {
        DateTime baseDate = new DateTime(2026, 11, 2);

        // 06:00 -> 0
        ScheduleMatrixHelper.CalculateRowIndex(baseDate.AddHours(6)).Should().Be(0);

        // 08:30 -> (510 - 360) / 30 = 5
        ScheduleMatrixHelper.CalculateRowIndex(baseDate.AddHours(8).AddMinutes(30)).Should().Be(5);

        // 17:00 -> (1020 - 360) / 30 = 22
        ScheduleMatrixHelper.CalculateRowIndex(baseDate.AddHours(17)).Should().Be(22);

        // 23:30 -> (1410 - 360) / 30 = 35
        ScheduleMatrixHelper.CalculateRowIndex(baseDate.AddHours(23).AddMinutes(30)).Should().Be(35);
    }

    [Fact]
    public void CalculateRowSpan_CalculatesCorrectSlotCount()
    {
        DateTime baseDate = new DateTime(2026, 11, 2);

        // 08:00 - 09:30 (90 min = 3 slots)
        ScheduleMatrixHelper.CalculateRowSpan(baseDate.AddHours(8), baseDate.AddHours(9).AddMinutes(30)).Should().Be(3);

        // 08:00 - 10:00 (120 min = 4 slots)
        ScheduleMatrixHelper.CalculateRowSpan(baseDate.AddHours(8), baseDate.AddHours(10)).Should().Be(4);

        // 22:00 - 24:00 (120 min, crossing to next day 00:00)
        ScheduleMatrixHelper.CalculateRowSpan(baseDate.AddHours(22), baseDate.AddDays(1)).Should().Be(4);
    }

    [Fact]
    public void GetStatusStyle_MapsColorsCorrectly()
    {
        // Pending -> Yellow/Amber
        var pending = ScheduleMatrixHelper.GetStatusStyle(BookingStatus.Pending);
        pending.BackgroundHex.Should().Be("#F59E0B");
        pending.StatusText.Should().Be("Chờ duyệt");

        // Confirmed -> Blue
        var confirmed = ScheduleMatrixHelper.GetStatusStyle(BookingStatus.Confirmed);
        confirmed.BackgroundHex.Should().Be("#2563EB");
        confirmed.StatusText.Should().Be("Đã xác nhận");

        // CheckedIn -> Green
        var checkedIn = ScheduleMatrixHelper.GetStatusStyle(BookingStatus.CheckedIn);
        checkedIn.BackgroundHex.Should().Be("#10B981");
        checkedIn.StatusText.Should().Be("Đang chơi");

        // Completed -> Gray
        var completed = ScheduleMatrixHelper.GetStatusStyle(BookingStatus.Completed);
        completed.BackgroundHex.Should().Be("#6B7280");
        completed.StatusText.Should().Be("Hoàn thành");

        // NoShow -> Red
        var noShow = ScheduleMatrixHelper.GetStatusStyle(BookingStatus.NoShow);
        noShow.BackgroundHex.Should().Be("#EF4444");
        noShow.StatusText.Should().Be("Vắng mặt");
    }

    [Fact]
    public void BuildBookingBlock_CreatesCorrectBlockWithRowAndSpan()
    {
        DateTime date = new DateTime(2026, 11, 2, 8, 0, 0);
        var bookingDto = new BookingItemDto
        {
            Id = 10,
            Code = "BK20261102-001",
            CourtId = 1,
            CourtName = "Sân 1",
            CustomerName = "Nguyễn Văn A",
            StartTime = date,
            EndTime = date.AddHours(2), // 08:00 - 10:00
            Status = BookingStatus.Confirmed,
            FinalAmount = 160000m,
            DepositAmount = 48000m,
            PaidAmount = 48000m
        };

        // Act
        var block = ScheduleMatrixHelper.BuildBookingBlock(bookingDto, columnIndex: 0);

        // Assert
        block.BookingId.Should().Be(10);
        block.ColumnIndex.Should().Be(0);
        block.RowIndex.Should().Be(4); // 08:00
        block.RowSpan.Should().Be(4);  // 2 hours = 4 slots
        block.BackgroundHex.Should().Be("#2563EB");
        block.TimeRangeText.Should().Be("08:00 - 10:00");
        block.CustomerDisplayName.Should().Be("Nguyễn Văn A");
    }

    [Fact]
    public void BuildEmptySlots_ExcludesOccupiedSlotsAndIncludesFreeSlots()
    {
        DateTime date = new DateTime(2026, 11, 2);
        var courts = new List<ScheduleCourtHeader>
        {
            new() { CourtId = 1, CourtName = "Sân 1", ColumnIndex = 0, IsActive = true }
        };

        // 1 active booking at 08:00 - 10:00 (RowIndex 4, RowSpan 4 -> slots 4, 5, 6, 7)
        var bookings = new List<ScheduleBookingBlock>
        {
            new()
            {
                BookingId = 1,
                CourtId = 1,
                ColumnIndex = 0,
                RowIndex = 4,
                RowSpan = 4,
                Status = BookingStatus.Confirmed
            }
        };

        // Act
        var emptySlots = ScheduleMatrixHelper.BuildEmptySlots(courts, bookings, date);

        // Assert
        // Total slots = 36, occupied = 4 -> empty = 32
        emptySlots.Should().HaveCount(32);
        emptySlots.Any(s => s.RowIndex >= 4 && s.RowIndex <= 7).Should().BeFalse();
        emptySlots.Any(s => s.RowIndex == 0).Should().BeTrue(); // 06:00 is empty
        emptySlots.Any(s => s.RowIndex == 8).Should().BeTrue(); // 10:00 is empty
    }

    #endregion

    #region ScheduleViewModel Unit Tests

    [Fact]
    public async Task ScheduleViewModel_StaffRole_BranchIsLockedToStaffBranch()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);

        // Act
        await vm.InitializeAsync();

        // Assert
        vm.CanSelectBranch.Should().BeFalse(); // Staff cannot switch branch
        vm.SelectedBranch.Should().NotBeNull();
        vm.SelectedBranch!.Id.Should().Be(1);
        vm.Courts.Should().HaveCount(6); // Branch 1 has 6 courts
    }

    [Fact]
    public async Task ScheduleViewModel_AdminRole_CanSelectBranch()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Admin, branchId: null);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);

        // Act
        await vm.InitializeAsync();

        // Assert
        vm.CanSelectBranch.Should().BeTrue(); // Admin can switch branch
        vm.Branches.Should().HaveCount(3);    // 3 branches seeded

        // Admin switches to Branch 2
        var branch2 = vm.Branches.First(b => b.Id == 2);
        vm.SelectedBranch = branch2;
        await vm.LoadScheduleDataAsync();

        vm.Courts.Should().HaveCount(6);
        vm.Courts.All(c => c.CourtId >= 7 && c.CourtId <= 12).Should().BeTrue(); // Courts of Branch 2
    }

    [Fact]
    public async Task ScheduleViewModel_DateNavigation_UpdatesDateAndTitle()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        await vm.InitializeAsync();

        var initialDate = vm.SelectedDate;

        // Act
        await vm.NextDayAsync();
        vm.SelectedDate.Date.Should().Be(initialDate.AddDays(1).Date);

        await vm.PreviousDayAsync();
        vm.SelectedDate.Date.Should().Be(initialDate.Date);

        await vm.TodayAsync();
        vm.SelectedDate.Date.Should().Be(DateTime.Today.Date);
    }

    [Fact]
    public async Task ScheduleViewModel_OpenCreateBookingForm_FromSlot_PrefillsCourtAndTime()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        vm.SelectedDate = DateTime.Today.AddDays(3);
        await vm.InitializeAsync();

        var slot = new ScheduleEmptySlot
        {
            CourtId = 2,
            CourtName = "Sân 2",
            ColumnIndex = 1,
            RowIndex = 4,
            SlotStartTime = vm.SelectedDate.Date.AddHours(8),
            SlotEndTime = vm.SelectedDate.Date.AddHours(8).AddMinutes(30)
        };

        // Act
        await vm.OpenCreateBookingFormAsync(slot);

        // Assert
        vm.IsBookingFormOpen.Should().BeTrue();
        vm.FormSelectedCourt.Should().NotBeNull();
        vm.FormSelectedCourt!.CourtId.Should().Be(2);
        vm.FormStartTimeStr.Should().Be("08:00");
        vm.FormEndTimeStr.Should().Be("09:00"); // Mặc định thời lượng 60 phút
        vm.FormDurationMinutes.Should().Be(60);
        vm.FormSubTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScheduleViewModel_RecalculatePricePreview_WithGoldCustomer_AppliesDiscount()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        vm.SelectedDate = DateTime.Today.AddDays(7); // Future weekday or weekend
        await vm.InitializeAsync();

        await vm.OpenCreateBookingFormAsync();
        vm.FormStartTimeStr = "08:00";
        vm.FormEndTimeStr = "10:00"; // 2 hours

        // Act: Chọn Customer 1 (Gold tier, 10% discount)
        var goldCustomer = (await customerService.SearchCustomersAsync("0911000001")).First();
        vm.FormSelectedCustomer = goldCustomer;
        await vm.RecalculatePricePreviewAsync();

        // Assert
        vm.FormPricingResult.Should().NotBeNull();
        vm.FormPricingResult!.DiscountPercent.Should().Be(10m);
        vm.FormDiscountAmount.Should().BeGreaterThan(0);
        vm.FormFinalAmount.Should().Be(vm.FormSubTotal - vm.FormDiscountAmount);
        vm.FormPricingNote.Should().Contain("10%");
    }

    [Fact]
    public async Task ScheduleViewModel_DoD_StaffCreatesBookingOnSchedule_AppearsOnGridCorrectly()
    {
        // DoD: "Staff đặt được trên lịch, thấy khối booking đúng chỗ"
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        vm.SelectedDate = DateTime.Today.AddDays(5);
        await vm.InitializeAsync();

        // Khung giờ 08:00 - 10:00 trên Sân 1
        var slot = vm.EmptySlots.First(s => s.CourtId == 1 && s.RowIndex == 4);
        await vm.OpenCreateBookingFormAsync(slot);
        vm.FormEndTimeStr = "10:00"; // 08:00 - 10:00
        vm.FormIsWalkIn = true;
        vm.FormGuestName = "Trần Khách";
        vm.FormGuestPhone = "0988776655";

        // Act
        await vm.ConfirmCreateBookingAsync();

        // Assert
        dialogService.LastInfoMessage.Should().Contain("Đặt sân thành công");
        vm.IsBookingFormOpen.Should().BeFalse();

        // Khối booking mới xuất hiện trên lịch đúng toạ độ:
        var newBookingBlock = vm.Bookings.FirstOrDefault(b => b.CourtId == 1 && b.RowIndex == 4);
        newBookingBlock.Should().NotBeNull();
        newBookingBlock!.RowSpan.Should().Be(4); // 2 hours = 4 slots
        newBookingBlock.TimeRangeText.Should().Be("08:00 - 10:00");
        newBookingBlock.Status.Should().Be(BookingStatus.CheckedIn); // WalkIn -> CheckedIn
    }

    [Fact]
    public async Task ScheduleViewModel_DoD_StaffCreatesOverlappingBooking_BlockedWithVietnameseMessage()
    {
        // DoD: "đặt trùng bị chặn với thông báo tiếng Việt"
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        vm.SelectedDate = DateTime.Today.AddDays(5);
        await vm.InitializeAsync();

        // Tạo đơn 1: 08:00 - 10:00 trên Sân 1
        var slot = vm.EmptySlots.First(s => s.CourtId == 1 && s.RowIndex == 4);
        await vm.OpenCreateBookingFormAsync(slot);
        vm.FormEndTimeStr = "10:00";
        vm.FormIsWalkIn = true;
        await vm.ConfirmCreateBookingAsync();

        // Act: Đặt đè lên đơn 1 (08:00 - 10:00) trên cùng Sân 1
        await vm.OpenCreateBookingFormAsync();
        vm.FormSelectedCourt = vm.Courts.First(c => c.CourtId == 1);
        vm.FormStartTimeStr = "08:00";
        vm.FormEndTimeStr = "10:00";
        vm.FormIsWalkIn = true;
        await vm.ConfirmCreateBookingAsync();

        // Assert: Bị chặn với thông báo lỗi tiếng Việt và form vẫn mở
        dialogService.LastErrorMessage.Should().Contain("Khung giờ này đã có người đặt trên sân đã chọn");
        vm.IsBookingFormOpen.Should().BeTrue();
    }

    [Fact]
    public async Task ScheduleViewModel_OpenBookingDetails_ShowsBookingInformation()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService);
        vm.SelectedDate = DateTime.Today.AddDays(5);
        await vm.InitializeAsync();

        // Tạo 1 đơn để click
        var slot = vm.EmptySlots.First(s => s.CourtId == 1 && s.RowIndex == 4);
        await vm.OpenCreateBookingFormAsync(slot);
        vm.FormIsWalkIn = true;
        vm.FormGuestName = "Khách Test";
        await vm.ConfirmCreateBookingAsync();

        var block = vm.Bookings.First(b => b.CourtId == 1 && b.RowIndex == 4);

        // Act: Click xem chi tiết đơn
        await vm.OpenBookingDetailsAsync(block);

        // Assert
        vm.IsDetailsOpen.Should().BeTrue();
        vm.DetailedBooking.Should().NotBeNull();
        vm.DetailedBooking!.Code.Should().Be(block.Code);
    }

    [Fact]
    public async Task Day7_DoD_ScheduleViewModel_QuickWalkIn_PrefillsNowAndCreatesCheckedInBooking()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var settingService = new SettingService(factory);
        var fixedScheduleService = new FixedScheduleService(factory, currentUser, pricingService, settingService);

        var vm = new ScheduleViewModel(
            factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService,
            fixedScheduleService: fixedScheduleService);

        await vm.InitializeAsync();

        // Act: Lễ tân bấm nút Khách vãng lai check-in ngay (A12)
        await vm.OpenQuickWalkInAsync();

        // Assert: Form được chuẩn bị sẵn với giờ hiện tại làm tròn 30 phút (BR-05) và chọn sân trống
        vm.IsBookingFormOpen.Should().BeTrue();
        vm.FormIsWalkIn.Should().BeTrue();
        vm.FormGuestName.Should().Be("Khách vãng lai");
        vm.FormSelectedCourt.Should().NotBeNull();

        // Act 2: Bấm xác nhận đặt sân & check-in vào sân
        await vm.ConfirmCreateBookingAsync();

        // Assert 2: Booking được tạo với trạng thái CheckedIn ngay lập tức (A7 & A12)
        dialogService.LastInfoMessage.Should().Contain("Đặt sân thành công");
        vm.IsBookingFormOpen.Should().BeFalse();

        // Kiểm tra booking xuất hiện trên grid với trạng thái CheckedIn
        var created = vm.Bookings.FirstOrDefault(b => b.CourtId == vm.FormSelectedCourt!.CourtId);
        created.Should().NotBeNull();
        created!.Status.Should().Be(BookingStatus.CheckedIn);
        created.BackgroundHex.Should().Be("#10B981"); // Màu xanh lá CheckedIn
    }

    [Fact]
    public async Task Day7_DoD_ScheduleViewModel_CancelFixedSchedule_CancelsFutureBookings()
    {
        // Arrange
        var (factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var settingService = new SettingService(factory);
        var fixedScheduleService = new FixedScheduleService(factory, currentUser, pricingService, settingService);

        // Tạo 1 lịch cố định 4 tuần
        DateOnly startDay = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var fixReq = new CreateFixedScheduleRequest
        {
            CustomerId = 1,
            CourtId = 1,
            DayOfWeek = (int)startDay.DayOfWeek,
            StartMinute = 480,
            EndMinute = 600,
            ValidFrom = startDay,
            NumberOfWeeks = 4
        };
        var fixResult = await fixedScheduleService.CreateFixedScheduleAsync(fixReq);
        fixResult.CreatedCount.Should().Be(4);

        var vm = new ScheduleViewModel(
            factory, currentUser, bookingService, branchCourtService, pricingService, customerService, dialogService,
            fixedScheduleService: fixedScheduleService);

        vm.SelectedDate = startDay.ToDateTime(TimeOnly.MinValue);
        await vm.InitializeAsync();

        // Chọn 1 booking thuộc lịch cố định
        var block = vm.Bookings.First(b => b.CourtId == 1 && b.StartTime.Date == startDay.ToDateTime(TimeOnly.MinValue).Date);
        await vm.OpenBookingDetailsAsync(block);

        vm.DetailedBooking.Should().NotBeNull();
        vm.DetailedBooking!.FixedScheduleId.Should().Be(fixResult.ScheduleId);
        vm.DetailedBooking.IsFixed.Should().BeTrue();

        // Act: Hủy toàn bộ lịch cố định (BR-19)
        await vm.CancelFixedScheduleAsync();

        // Assert: Toàn bộ booking tương lai thuộc lịch cố định bị hủy
        dialogService.LastInfoMessage.Should().Contain("Đã hủy toàn bộ lịch cố định thành công");
        using (var db = await factory.CreateDbContextAsync())
        {
            var schedule = await db.FixedSchedules.FindAsync(fixResult.ScheduleId);
            schedule!.IsActive.Should().BeFalse();

            var futureBookings = await db.Bookings.Where(b => b.FixedScheduleId == fixResult.ScheduleId).ToListAsync();
            futureBookings.All(b => b.Status == BookingStatus.Cancelled).Should().BeTrue();
        }
    }

    #endregion
}
