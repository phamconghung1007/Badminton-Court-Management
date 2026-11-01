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

public class ScheduleServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser, BookingService bookingService, BranchCourtService branchCourtService, StubDialogService dialogService) CreateTestSetup(
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
        var dialogService = new StubDialogService();

        return (factory, currentUser, bookingService, branchCourtService, dialogService);
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
        var (factory, currentUser, bookingService, branchCourtService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, dialogService);

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
        var (factory, currentUser, bookingService, branchCourtService, dialogService) = CreateTestSetup(UserRole.Admin, branchId: null);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, dialogService);

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
        var (factory, currentUser, bookingService, branchCourtService, dialogService) = CreateTestSetup(UserRole.Staff, branchId: 1);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var vm = new ScheduleViewModel(factory, currentUser, bookingService, branchCourtService, dialogService);
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

    #endregion
}
