using System;
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

public class BookingListViewModelTests
{
    private class TestDialogService : IDialogService
    {
        public string? LastInfoMessage { get; private set; }
        public string? LastErrorMessage { get; private set; }

        public void ShowInfo(string message, string title = "Thông báo") => LastInfoMessage = message;
        public void ShowWarning(string message, string title = "Cảnh báo") { }
        public void ShowError(string message, string title = "Lỗi") => LastErrorMessage = message;
        public bool ShowConfirmation(string message, string title = "Xác nhận") => true;
    }

    private async Task<(IDbContextFactory<AppDbContext> factory, CurrentUserContext userContext, BookingService bookingService, BranchCourtService branchCourtService, TestDialogService dialogService)> CreateTestSetupAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(options);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var userContext = new CurrentUserContext();
        userContext.SetUser(3, "staff1", "Nhân viên Cầu Giấy", UserRole.Staff, 1, "Cơ sở Cầu Giấy");

        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, userContext);
        var bookingService = new BookingService(factory, userContext, pricingService, settingService);
        var branchCourtService = new BranchCourtService(factory, userContext);
        var dialogService = new TestDialogService();

        return (factory, userContext, bookingService, branchCourtService, dialogService);
    }

    [Fact]
    public async Task BookingListViewModel_InitializeAsync_LoadsCourtsAndDefaultBookings()
    {
        // Arrange
        var (factory, userContext, bookingService, branchCourtService, dialogService) = await CreateTestSetupAsync();
        var vm = new BookingListViewModel(factory, userContext, bookingService, branchCourtService, dialogService);

        // Act
        await vm.InitializeCommand.ExecuteAsync(null);

        // Assert
        vm.CourtFilters.Should().NotBeEmpty();
        vm.CourtFilters.Count.Should().Be(6); // Branch 1 has 6 courts
        vm.Bookings.Should().NotBeNull();
        vm.TotalCount.Should().Be(vm.Bookings.Count);
    }

    [Fact]
    public async Task BookingListViewModel_SearchKeyword_FiltersByCustomerOrCode()
    {
        // Arrange
        var (factory, userContext, bookingService, branchCourtService, dialogService) = await CreateTestSetupAsync();

        DateTime targetDay = DateTime.Today.AddDays(1);
        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = targetDay.AddHours(8),
            EndTime = targetDay.AddHours(9),
            GuestName = "Tran Thi Thu",
            GuestPhone = "0988776655"
        });

        var vm = new BookingListViewModel(factory, userContext, bookingService, branchCourtService, dialogService);
        vm.FilterDate = targetDay;
        await vm.InitializeCommand.ExecuteAsync(null);

        // Act: Search theo tên khách
        vm.SearchKeyword = "Tran Thi Thu";
        await vm.SearchCommand.ExecuteAsync(null);

        // Assert
        vm.Bookings.Should().ContainSingle();
        vm.Bookings[0].GuestName.Should().Be("Tran Thi Thu");
        vm.Bookings[0].Code.Should().Be(createResult.Code);

        // Act: Clear filter
        await vm.ClearFiltersCommand.ExecuteAsync(null);
        vm.SearchKeyword.Should().BeEmpty();
        vm.FilterDate.Should().BeNull();
    }

    [Fact]
    public async Task BookingListViewModel_ConfirmAndCheckInBooking_WorkflowSucceeds()
    {
        // Arrange
        var (factory, userContext, bookingService, branchCourtService, dialogService) = await CreateTestSetupAsync();

        DateTime targetDay = DateTime.Today.AddDays(1);
        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = targetDay.AddHours(10),
            EndTime = targetDay.AddHours(11),
            GuestName = "Khach Test Workflow",
            GuestPhone = "0933112233"
        });

        var vm = new BookingListViewModel(factory, userContext, bookingService, branchCourtService, dialogService);
        vm.FilterDate = targetDay;

        // Mô phỏng đơn do khách đặt online ở trạng thái Pending cần nhân viên duyệt
        await using (var db = await factory.CreateDbContextAsync())
        {
            var bookingEntity = await db.Bookings.FindAsync(createResult.Id);
            bookingEntity!.Status = BookingStatus.Pending;
            await db.SaveChangesAsync();
        }

        await vm.InitializeCommand.ExecuteAsync(null);

        var bookingItem = vm.Bookings.FirstOrDefault(b => b.Id == createResult.Id);
        bookingItem.Should().NotBeNull();
        bookingItem!.Status.Should().Be(BookingStatus.Pending);

        // Act 1: Duyệt đơn (Confirm)
        await vm.ConfirmBookingCommand.ExecuteAsync(bookingItem);

        // Assert 1
        dialogService.LastInfoMessage.Should().Contain("thành công");
        var updatedItem = vm.Bookings.First(b => b.Id == createResult.Id);
        updatedItem.Status.Should().Be(BookingStatus.Confirmed);

        // Act 2: Check-in vào sân
        await vm.CheckInBookingCommand.ExecuteAsync(updatedItem);

        // Assert 2
        dialogService.LastInfoMessage.Should().Contain("Check-in thành công");
        var checkedInItem = vm.Bookings.First(b => b.Id == createResult.Id);
        checkedInItem.Status.Should().Be(BookingStatus.CheckedIn);
    }

    [Fact]
    public async Task BookingListViewModel_CancelBooking_SetsStatusToCancelled()
    {
        // Arrange
        var (factory, userContext, bookingService, branchCourtService, dialogService) = await CreateTestSetupAsync();

        DateTime targetDay = DateTime.Today.AddDays(1);
        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 2,
            StartTime = targetDay.AddHours(14),
            EndTime = targetDay.AddHours(15),
            GuestName = "Khach Test Cancel",
            GuestPhone = "0944556677"
        });

        var vm = new BookingListViewModel(factory, userContext, bookingService, branchCourtService, dialogService);
        vm.FilterDate = targetDay;
        await vm.InitializeCommand.ExecuteAsync(null);

        var bookingItem = vm.Bookings.FirstOrDefault(b => b.Id == createResult.Id);
        bookingItem.Should().NotBeNull();

        // Act: Hủy đơn
        await vm.CancelBookingCommand.ExecuteAsync(bookingItem);

        // Assert
        dialogService.LastInfoMessage.Should().Contain("Đã hủy");
        var cancelledItem = vm.Bookings.First(b => b.Id == createResult.Id);
        cancelledItem.Status.Should().Be(BookingStatus.Cancelled);
    }
}