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
using BadmintonManager.ViewModels.Customer;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class CustomerBookingsViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly BranchCourtService _branchCourtService;
    private readonly SettingService _settingService;
    private readonly PricingService _pricingService;
    private readonly BookingService _bookingService;
    private readonly RecordingDialogService _dialogService;

    public CustomerBookingsViewModelTests()
    {
        var dbName = $"Badminton_CustBookingsVm_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100, "Vàng");

        _branchCourtService = new BranchCourtService(_factory, _currentUser);
        _settingService = new SettingService(_factory);
        _pricingService = new PricingService(_factory, _settingService, _currentUser);
        _bookingService = new BookingService(_factory, _currentUser, _pricingService, _settingService);
        _dialogService = new RecordingDialogService();

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Now };
        db.Branches.Add(b1);

        var c1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        db.Courts.Add(c1);

        var tier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        db.MembershipTiers.Add(tier);

        var cust1 = new Customer { Id = 100, FullName = "Nguyễn Văn Cust1", Phone = "0900000001", UserId = 10, MembershipTierId = 1, TotalPoints = 100, IsActive = true, CreatedAt = DateTime.Now };
        var cust2 = new Customer { Id = 200, FullName = "Trần Thị Cust2", Phone = "0900000002", UserId = 20, MembershipTierId = 1, TotalPoints = 200, IsActive = true, CreatedAt = DateTime.Now };
        db.Customers.AddRange(cust1, cust2);

        // Booking 1 của Customer 1: Còn > 24 giờ (sau 2 ngày)
        db.Bookings.Add(new Booking
        {
            Id = 101,
            Code = "BK-101",
            CourtId = 1,
            CustomerId = 100,
            BookingType = BookingType.Single,
            StartTime = DateTime.Today.AddDays(2).AddHours(18),
            EndTime = DateTime.Today.AddDays(2).AddHours(20),
            Status = BookingStatus.Confirmed,
            SubTotal = 200000m,
            FinalAmount = 200000m,
            DepositAmount = 60000m,
            CreatedByUserId = 10,
            CreatedAt = DateTime.Now
        });

        // Booking 2 của Customer 1: Còn < 24 giờ (sau 2 giờ)
        db.Bookings.Add(new Booking
        {
            Id = 102,
            Code = "BK-102",
            CourtId = 1,
            CustomerId = 100,
            BookingType = BookingType.Single,
            StartTime = DateTime.Now.AddHours(2),
            EndTime = DateTime.Now.AddHours(4),
            Status = BookingStatus.Confirmed,
            SubTotal = 200000m,
            FinalAmount = 200000m,
            DepositAmount = 60000m,
            CreatedByUserId = 10,
            CreatedAt = DateTime.Now
        });

        // Booking của Customer 2 (không được xuất hiện trong list của Customer 1)
        db.Bookings.Add(new Booking
        {
            Id = 201,
            Code = "BK-201",
            CourtId = 1,
            CustomerId = 200,
            BookingType = BookingType.Single,
            StartTime = DateTime.Today.AddDays(3).AddHours(18),
            EndTime = DateTime.Today.AddDays(3).AddHours(20),
            Status = BookingStatus.Confirmed,
            SubTotal = 200000m,
            FinalAmount = 200000m,
            DepositAmount = 60000m,
            CreatedByUserId = 20,
            CreatedAt = DateTime.Now
        });

        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.FreeCancelHours, Value = "24" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.OpenMinute, Value = "360" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.CloseMinute, Value = "1440" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MinDurationMinutes, Value = "60" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MaxDurationMinutes, Value = "240" });

        db.SaveChanges();
    }

    [Fact]
    public async Task LoadBookings_LoadsOnlyCurrentCustomerBookings_AndCalculatesKPIs()
    {
        var vm = new CustomerBookingsViewModel(_bookingService, _currentUser, _dialogService, _branchCourtService);

        await vm.LoadBookingsAsync();

        vm.Bookings.Should().HaveCount(2);
        vm.Bookings.Should().OnlyContain(b => b.CustomerId == 100);
        vm.TotalBookingsCount.Should().Be(2);
        vm.ConfirmedBookingsCount.Should().Be(2);
    }

    [Fact]
    public async Task CancelBooking_Before24Hours_CancelsAndShowsFullRefundMessage()
    {
        var vm = new CustomerBookingsViewModel(_bookingService, _currentUser, _dialogService, _branchCourtService);
        await vm.LoadBookingsAsync();

        var booking101 = vm.Bookings.First(b => b.Id == 101);

        await vm.CancelBookingAsync(booking101);

        _dialogService.LastInfoMessage.Should().NotBeNull();
        _dialogService.LastInfoMessage.Should().Contain("thành công");

        using var db = _factory.CreateDbContext();
        var updated = await db.Bookings.FindAsync(101);
        updated!.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task RescheduleBooking_Under24Hours_ShowsWarning()
    {
        var vm = new CustomerBookingsViewModel(_bookingService, _currentUser, _dialogService, _branchCourtService);
        await vm.LoadBookingsAsync();

        var booking102 = vm.Bookings.First(b => b.Id == 102); // Còn 2 giờ (< 24h)

        vm.OpenRescheduleDrawer(booking102);

        _dialogService.LastWarningMessage.Should().NotBeNull();
        _dialogService.LastWarningMessage.Should().Contain("trước giờ chơi ít nhất 24 giờ");
        vm.IsRescheduleDrawerOpen.Should().BeFalse();
    }
}
