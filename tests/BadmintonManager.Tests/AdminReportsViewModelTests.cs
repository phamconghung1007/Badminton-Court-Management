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
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Admin;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class AdminReportsViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IReportService _reportService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly RecordingDialogService _dialogService;

    public AdminReportsViewModelTests()
    {
        var dbName = $"Badminton_AdminRepVM_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(1, "admin", "Hệ Thống Admin", UserRole.Admin, branchId: null);

        var settingService = new SettingService(_factory);
        _reportService = new ReportService(_factory, _currentUser);
        _branchCourtService = new BranchCourtService(_factory, _currentUser);
        _dialogService = new RecordingDialogService();

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        var b2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Xã Đàn", Phone = "0987654322", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        db.Branches.AddRange(b1, b2);

        var c1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        var c2 = new Court { Id = 2, BranchId = 2, Name = "Sân 2", IsActive = true };
        db.Courts.AddRange(c1, c2);

        var tier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0 };
        db.MembershipTiers.Add(tier);

        var cust = new Customer { Id = 1, FullName = "Nguyễn Văn VIP", Phone = "0988888888", MembershipTierId = 1, TotalPoints = 500, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        db.Customers.Add(cust);

        var staff = new AppUser { Id = 10, Username = "staff1", FullName = "Nhân viên", Role = UserRole.Staff, BranchId = 1, IsActive = true };
        db.Users.Add(staff);

        var today = DateTime.Today;

        var bk1 = new Booking
        {
            Id = 1,
            Code = "BK-REP-01",
            CourtId = 1,
            CustomerId = 1,
            StartTime = today.AddHours(8),
            EndTime = today.AddHours(10),
            Status = BookingStatus.Completed,
            SubTotal = 200000m,
            FinalAmount = 200000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        var bk2 = new Booking
        {
            Id = 2,
            Code = "BK-REP-02",
            CourtId = 2,
            CustomerId = 1,
            StartTime = today.AddHours(14),
            EndTime = today.AddHours(16),
            Status = BookingStatus.Completed,
            SubTotal = 250000m,
            FinalAmount = 250000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        db.Bookings.AddRange(bk1, bk2);
        db.SaveChanges();
    }

    [Fact]
    public async Task Initialize_LoadsBranches_AndInitialReports()
    {
        // Arrange & Act
        var vm = new AdminReportsViewModel(_reportService, _branchCourtService, _dialogService, _currentUser);
        await vm.InitializeAsync();

        // Assert
        vm.Branches.Should().HaveCount(2);
        vm.Summary.Should().NotBeNull();
        vm.Summary!.TotalRevenue.Should().Be(450000m); // 200k + 250k
        vm.Summary.TotalBookings.Should().Be(2);
        vm.DailyRevenues.Should().NotBeEmpty();
        vm.BranchRevenueComparisons.Should().HaveCount(2);
        vm.CourtUtilizations.Should().HaveCount(2);
        vm.TopCustomers.Should().HaveCount(1);
    }

    [Fact]
    public void FilterQuickButtons_UpdatesDateRanges()
    {
        // Arrange
        var vm = new AdminReportsViewModel(_reportService, _branchCourtService, _dialogService, _currentUser);

        // Act & Assert
        vm.FilterToday();
        vm.FromDate.Should().Be(DateTime.Today);
        vm.ToDate.Should().Be(DateTime.Today);

        vm.FilterThisMonth();
        vm.FromDate.Day.Should().Be(1);
        vm.ToDate.Should().Be(DateTime.Today);

        vm.FilterLast30Days();
        vm.FromDate.Should().Be(DateTime.Today.AddDays(-30));
        vm.ToDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task LoadReports_WhenFromDateGreaterThanToDate_ShowsWarning()
    {
        // Arrange
        var vm = new AdminReportsViewModel(_reportService, _branchCourtService, _dialogService, _currentUser);
        vm.FromDate = DateTime.Today.AddDays(5);
        vm.ToDate = DateTime.Today;

        // Act
        await vm.LoadReportsAsync();

        // Assert
        _dialogService.LastWarningMessage.Should().Contain("Ngày bắt đầu không được lớn hơn");
    }

    [Fact]
    public async Task FilterByBranch_LoadsScopedReports()
    {
        // Arrange
        var vm = new AdminReportsViewModel(_reportService, _branchCourtService, _dialogService, _currentUser);
        await vm.InitializeAsync();

        // Act: Filter to Branch 1 only
        vm.SelectedBranch = vm.Branches.First(b => b.Id == 1);
        await vm.LoadReportsAsync();

        // Assert
        vm.Summary.Should().NotBeNull();
        vm.Summary!.TotalRevenue.Should().Be(200000m); // Only Branch 1
        vm.Summary.TotalBookings.Should().Be(1);
        vm.CourtUtilizations.Should().HaveCount(1);
    }
}
