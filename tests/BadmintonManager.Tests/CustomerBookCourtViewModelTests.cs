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
using BadmintonManager.ViewModels.Customer;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class CustomerBookCourtViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly BranchCourtService _branchCourtService;
    private readonly SettingService _settingService;
    private readonly PricingService _pricingService;
    private readonly BookingService _bookingService;
    private readonly RecordingDialogService _dialogService;
    private readonly NavigationService _navigationService;

    public CustomerBookCourtViewModelTests()
    {
        var dbName = $"Badminton_CustBookVm_{Guid.NewGuid():N}";
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
        _navigationService = new NavigationService();

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Now };
        var b2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Đống Đa", Phone = "0987654322", IsActive = true, CreatedAt = DateTime.Now };
        db.Branches.AddRange(b1, b2);

        var c1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        var c2 = new Court { Id = 2, BranchId = 1, Name = "Sân 2", IsActive = true };
        db.Courts.AddRange(c1, c2);

        var tierVang = new MembershipTier { Id = 3, Name = "Vàng", MinPoints = 1500, DiscountPercent = 10, SortOrder = 3 };
        db.MembershipTiers.Add(tierVang);

        var user = new AppUser { Id = 10, Username = "cust1", PasswordHash = "hash", FullName = "Nguyễn Văn Cust1", Role = UserRole.Customer, IsActive = true, CreatedAt = DateTime.Now };
        db.Users.Add(user);

        var customer = new Customer { Id = 100, FullName = "Nguyễn Văn Cust1", Phone = "0900000001", UserId = 10, MembershipTierId = 3, TotalPoints = 1600, IsActive = true, CreatedAt = DateTime.Now };
        db.Customers.Add(customer);

        db.PriceRules.Add(new PriceRule { Id = 1, DayType = DayType.Weekday, StartMinute = 360, EndMinute = 1440, PricePerHour = 100000m, IsActive = true });
        db.PriceRules.Add(new PriceRule { Id = 2, DayType = DayType.Weekend, StartMinute = 360, EndMinute = 1440, PricePerHour = 120000m, IsActive = true });

        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.OpenMinute, Value = "360" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.CloseMinute, Value = "1440" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MinDurationMinutes, Value = "60" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MaxDurationMinutes, Value = "240" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.DepositPercent, Value = "30" });

        db.SaveChanges();
    }

    [Fact]
    public async Task Initialize_LoadsBranches_AndSelectsFirst()
    {
        var vm = new CustomerBookCourtViewModel(_branchCourtService, _bookingService, _currentUser, _dialogService, _navigationService);

        await vm.InitializeAsync();

        vm.Branches.Should().HaveCount(2);
        vm.SelectedBranch.Should().NotBeNull();
        vm.SelectedBranch!.Name.Should().Be("Cơ sở Cầu Giấy");
        vm.CustomerTierName.Should().Be("Vàng");
        vm.TimeSlots.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchCourts_ValidInput_PopulatesAvailableCourts_AndSelectsFirst()
    {
        var vm = new CustomerBookCourtViewModel(_branchCourtService, _bookingService, _currentUser, _dialogService, _navigationService);
        await vm.InitializeAsync();

        vm.SelectedDate = DateTime.Today.AddDays(1);
        vm.SelectedStartTime = "17:00";
        vm.SelectedEndTime = "19:00"; // 2 tiếng = 120 phút

        await vm.SearchCourtsAsync();

        vm.HasSearched.Should().BeTrue();
        vm.AvailableCourts.Should().HaveCount(2); // Sân 1 và Sân 2 đều trống
        vm.SelectedCourt.Should().NotBeNull();
        vm.SelectedCourt!.CourtName.Should().Be("Sân 1");
        vm.SubTotal.Should().Be(200000m);
        vm.DiscountPercent.Should().Be(10m);
        vm.DiscountAmount.Should().Be(20000m);
        vm.FinalAmount.Should().Be(180000m);
        vm.DepositAmount.Should().Be(54000m); // 30% của 180k = 54k
    }

    [Fact]
    public async Task SearchCourts_PastDate_ShowsAlert()
    {
        var vm = new CustomerBookCourtViewModel(_branchCourtService, _bookingService, _currentUser, _dialogService, _navigationService);
        await vm.InitializeAsync();

        vm.SelectedDate = DateTime.Today.AddDays(-1); // Quá khứ

        await vm.SearchCourtsAsync();

        _dialogService.LastWarningMessage.Should().NotBeNull();
        _dialogService.LastWarningMessage.Should().Contain("quá khứ");
        vm.HasSearched.Should().BeFalse();
    }

    [Fact]
    public async Task SearchCourts_DurationUnder60Minutes_ShowsAlert()
    {
        var vm = new CustomerBookCourtViewModel(_branchCourtService, _bookingService, _currentUser, _dialogService, _navigationService);
        await vm.InitializeAsync();

        vm.SelectedDate = DateTime.Today.AddDays(1);
        vm.SelectedStartTime = "17:00";
        vm.SelectedEndTime = "17:30"; // 30 phút

        await vm.SearchCourtsAsync();

        _dialogService.LastWarningMessage.Should().NotBeNull();
        _dialogService.LastWarningMessage.Should().Contain("từ 60 phút");
    }

    [Fact]
    public async Task BookCourt_WhenConfirmed_CreatesBooking_AndShowsSuccessAlert()
    {
        var vm = new CustomerBookCourtViewModel(_branchCourtService, _bookingService, _currentUser, _dialogService, _navigationService);
        await vm.InitializeAsync();

        vm.SelectedDate = DateTime.Today.AddDays(1);
        vm.SelectedStartTime = "18:00";
        vm.SelectedEndTime = "20:00";

        await vm.SearchCourtsAsync();
        vm.SelectedCourt.Should().NotBeNull();

        await vm.BookCourtAsync();

        _dialogService.LastInfoMessage.Should().NotBeNull();
        _dialogService.LastInfoMessage.Should().Contain("thành công");
        _dialogService.LastInfoMessage.Should().Contain("Chờ duyệt (Pending)");
        _dialogService.LastInfoMessage.Should().Contain("2 giờ");

        // Kiểm tra trong database
        using var db = _factory.CreateDbContext();
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.CustomerId == 100);
        booking.Should().NotBeNull();
        booking!.Status.Should().Be(BookingStatus.Pending);
        booking.FinalAmount.Should().Be(180000m);
    }
}
