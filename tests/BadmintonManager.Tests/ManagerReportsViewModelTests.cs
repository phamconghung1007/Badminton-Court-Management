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
using BadmintonManager.ViewModels.Manager;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class ManagerReportsViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IReportService _reportService;
    private readonly RecordingDialogService _dialogService;

    public ManagerReportsViewModelTests()
    {
        var dbName = $"Badminton_MgrRepVM_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(2, "manager1", "Nguyễn Văn Quản Lý", UserRole.Manager, branchId: 1, branchName: "Cơ sở Cầu Giấy");

        _reportService = new ReportService(_factory, _currentUser);
        _dialogService = new RecordingDialogService();

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        var b2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Xã Đàn", Phone = "0987654322", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        db.Branches.AddRange(b1, b2);

        var c1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1 - Cầu Giấy", IsActive = true };
        var c2 = new Court { Id = 2, BranchId = 1, Name = "Sân 2 - Cầu Giấy", IsActive = true };
        var c3 = new Court { Id = 3, BranchId = 2, Name = "Sân 1 - Đống Đa", IsActive = true };
        db.Courts.AddRange(c1, c2, c3);

        var tier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0 };
        db.MembershipTiers.Add(tier);

        var cust1 = new Customer { Id = 1, FullName = "Khách A", Phone = "0911111111", MembershipTierId = 1, TotalPoints = 100, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        var cust2 = new Customer { Id = 2, FullName = "Khách B", Phone = "0922222222", MembershipTierId = 1, TotalPoints = 200, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        db.Customers.AddRange(cust1, cust2);

        var staff1 = new AppUser { Id = 10, Username = "staff1", FullName = "Nhân viên 1", Role = UserRole.Staff, BranchId = 1, IsActive = true };
        var staff2 = new AppUser { Id = 11, Username = "staff2", FullName = "Nhân viên 2", Role = UserRole.Staff, BranchId = 2, IsActive = true };
        db.Users.AddRange(staff1, staff2);

        var today = DateTime.Today;

        // Booking at Branch 1: 150.000 VNĐ
        var bk1 = new Booking
        {
            Id = 1,
            Code = "BK-B1-01",
            CourtId = 1,
            CustomerId = 1,
            StartTime = today.AddHours(8),
            EndTime = today.AddHours(10),
            Status = BookingStatus.Completed,
            SubTotal = 150000m,
            FinalAmount = 150000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        // Booking at Branch 2: 500.000 VNĐ (Manager 1 should NEVER see this)
        var bk2 = new Booking
        {
            Id = 2,
            Code = "BK-B2-01",
            CourtId = 3,
            CustomerId = 2,
            StartTime = today.AddHours(14),
            EndTime = today.AddHours(17),
            Status = BookingStatus.Completed,
            SubTotal = 500000m,
            FinalAmount = 500000m,
            CreatedByUserId = 11,
            CreatedAt = today.AddDays(-1)
        };

        db.Bookings.AddRange(bk1, bk2);
        db.SaveChanges();
    }

    [Fact]
    public async Task Initialize_LoadsBranchReports_LockedToOwnBranch()
    {
        // Arrange & Act
        var vm = new ManagerReportsViewModel(_reportService, _dialogService, _currentUser);
        await vm.InitializeAsync();

        // Assert
        vm.BranchName.Should().Be("Cơ sở Cầu Giấy");
        vm.Summary.Should().NotBeNull();
        vm.Summary!.TotalRevenue.Should().Be(150000m); // ONLY Branch 1 revenue, NOT 650.000
        vm.Summary.TotalBookings.Should().Be(1);
        vm.DailyRevenues.Should().HaveCount(1);
        vm.DailyRevenues[0].TotalRevenue.Should().Be(150000m);
        vm.CourtUtilizations.Should().HaveCount(2); // Court 1 and Court 2 in branch 1
        vm.TopCustomers.Should().HaveCount(1);
        vm.TopCustomers[0].CustomerId.Should().Be(1);
    }

    [Fact]
    public async Task Manager_DoesNotSeeOtherBranchData()
    {
        // Arrange
        var vm = new ManagerReportsViewModel(_reportService, _dialogService, _currentUser);
        await vm.InitializeAsync();

        // Assert: Sân của cơ sở khác (Id=3) tuyệt đối không xuất hiện
        vm.CourtUtilizations.Select(c => c.CourtId).Should().NotContain(3);
        // Khách B (chỉ đặt sân ở cơ sở 2) không xuất hiện trong Top khách của cơ sở 1
        vm.TopCustomers.Select(c => c.CustomerId).Should().NotContain(2);
    }

    [Fact]
    public void FilterQuickButtons_UpdatesDateRanges()
    {
        // Arrange
        var vm = new ManagerReportsViewModel(_reportService, _dialogService, _currentUser);

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
        var vm = new ManagerReportsViewModel(_reportService, _dialogService, _currentUser);
        vm.FromDate = DateTime.Today.AddDays(10);
        vm.ToDate = DateTime.Today;

        // Act
        await vm.LoadReportsAsync();

        // Assert
        _dialogService.LastWarningMessage.Should().Contain("Ngày bắt đầu không được lớn hơn");
    }
}
