using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class ReportServiceTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        var dbName = $"Badminton_ReportSec_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _reportService = new ReportService(_factory, _currentUser);

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var branch1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        var branch2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Xã Đàn", Phone = "0987654322", IsActive = true, CreatedAt = DateTime.Today.AddDays(-60) };
        db.Branches.AddRange(branch1, branch2);

        var court1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1 - Cầu Giấy", IsActive = true };
        var court2 = new Court { Id = 2, BranchId = 1, Name = "Sân 2 - Cầu Giấy", IsActive = true };
        var court3 = new Court { Id = 3, BranchId = 2, Name = "Sân 1 - Đống Đa", IsActive = true };
        db.Courts.AddRange(court1, court2, court3);

        var tier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0 };
        db.MembershipTiers.Add(tier);

        var cust1 = new Customer { Id = 1, FullName = "Khách Hàng A", Phone = "0911111111", MembershipTierId = 1, TotalPoints = 100, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        var cust2 = new Customer { Id = 2, FullName = "Khách Hàng B", Phone = "0922222222", MembershipTierId = 1, TotalPoints = 200, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        db.Customers.AddRange(cust1, cust2);

        var staff = new AppUser { Id = 10, Username = "staff1", Role = UserRole.Staff, FullName = "Nhân viên", BranchId = 1, IsActive = true };
        db.Users.Add(staff);

        var today = DateTime.Today;

        // Booking 1: Branch 1, Court 1, 2 hours (120 min), Customer 1, FinalAmount = 200.000
        var b1 = new Booking
        {
            Id = 1,
            Code = "BK-001",
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

        // Booking 2: Branch 1, Court 2, 1 hour (60 min), Customer 1, FinalAmount = 100.000
        var b2 = new Booking
        {
            Id = 2,
            Code = "BK-002",
            CourtId = 2,
            CustomerId = 1,
            StartTime = today.AddHours(10),
            EndTime = today.AddHours(11),
            Status = BookingStatus.CheckedIn,
            SubTotal = 100000m,
            FinalAmount = 100000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        // Booking 3: Branch 2, Court 3, 3 hours (180 min), Customer 2, FinalAmount = 350.000
        var b3 = new Booking
        {
            Id = 3,
            Code = "BK-003",
            CourtId = 3,
            CustomerId = 2,
            StartTime = today.AddHours(14),
            EndTime = today.AddHours(17),
            Status = BookingStatus.Completed,
            SubTotal = 350000m,
            FinalAmount = 350000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        // Booking 4: Cancelled booking (should not count towards revenue)
        var b4 = new Booking
        {
            Id = 4,
            Code = "BK-004",
            CourtId = 1,
            CustomerId = 2,
            StartTime = today.AddHours(18),
            EndTime = today.AddHours(20),
            Status = BookingStatus.Cancelled,
            SubTotal = 200000m,
            FinalAmount = 200000m,
            CreatedByUserId = 10,
            CreatedAt = today.AddDays(-1)
        };

        db.Bookings.AddRange(b1, b2, b3, b4);
        db.SaveChanges();
    }

    [Fact]
    public async Task Admin_CanGetOverviewSummary_AndDailyRevenue_AcrossAllBranches()
    {
        // Arrange
        _currentUser.SetUser(1, "admin", "Hệ Thống Admin", UserRole.Admin, branchId: null, branchName: null, customerId: null);
        var today = DateTime.Today;

        // Act
        var summary = await _reportService.GetOverviewSummaryAsync(null, today, today);
        var daily = await _reportService.GetDailyRevenueAsync(null, today, today);

        // Assert
        summary.TotalRevenue.Should().Be(650000m); // 200k + 100k + 350k (cancelled 200k excluded)
        summary.TotalBookings.Should().Be(3);
        summary.TotalBookedHours.Should().Be(6.0); // 2h + 1h + 3h = 6h
        summary.AverageUtilizationRate.Should().BeGreaterThan(0);

        daily.Should().HaveCount(1);
        daily[0].TotalRevenue.Should().Be(650000m);
        daily[0].BookingCount.Should().Be(3);
    }

    [Fact]
    public async Task Admin_CanGetBranchRevenueComparison_AndOccupancyComparison()
    {
        // Arrange
        _currentUser.SetUser(1, "admin", "Hệ Thống Admin", UserRole.Admin, branchId: null, branchName: null, customerId: null);
        var today = DateTime.Today;

        // Act
        var revComparison = await _reportService.GetBranchRevenueComparisonAsync(today, today);
        var occComparison = await _reportService.GetBranchOccupancyComparisonAsync(today, today);

        // Assert
        revComparison.Should().HaveCount(2);
        var b1Rev = revComparison.First(x => x.BranchId == 1);
        b1Rev.TotalRevenue.Should().Be(300000m);
        b1Rev.BookingCount.Should().Be(2);

        var b2Rev = revComparison.First(x => x.BranchId == 2);
        b2Rev.TotalRevenue.Should().Be(350000m);
        b2Rev.BookingCount.Should().Be(1);

        occComparison.Should().HaveCount(2);
        var b1Occ = occComparison.First(x => x.BranchId == 1);
        b1Occ.BookedHours.Should().Be(3.0); // 2h + 1h = 3h

        var b2Occ = occComparison.First(x => x.BranchId == 2);
        b2Occ.BookedHours.Should().Be(3.0);
    }

    [Fact]
    public async Task Manager_CanGetReports_ForOwnBranch()
    {
        // Arrange
        _currentUser.SetUser(2, "manager1", "Quản Lý 1", UserRole.Manager, branchId: 1, branchName: "Cơ sở Cầu Giấy", customerId: null);
        var today = DateTime.Today;

        // Act - manager passes null or branch 1
        var summary = await _reportService.GetOverviewSummaryAsync(null, today, today);
        var daily = await _reportService.GetDailyRevenueAsync(1, today, today);

        // Assert - only Branch 1 data is returned
        summary.TotalRevenue.Should().Be(300000m); // 200k + 100k
        summary.TotalBookings.Should().Be(2);
        summary.TotalBookedHours.Should().Be(3.0);

        daily.Should().HaveCount(1);
        daily[0].TotalRevenue.Should().Be(300000m);
    }

    [Fact]
    public async Task Manager_AccessingOtherBranch_ThrowsForbiddenException()
    {
        // Arrange: Manager of Branch 1
        _currentUser.SetUser(2, "manager1", "Quản Lý 1", UserRole.Manager, branchId: 1, branchName: "Cơ sở Cầu Giấy", customerId: null);
        var today = DateTime.Today;

        // Act & Assert: attempts to query branch 2
        var act = async () => await _reportService.GetDailyRevenueAsync(2, today, today);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*cơ sở mình trực thuộc*");
    }

    [Fact]
    public async Task Manager_CallingBranchComparison_ThrowsForbiddenException()
    {
        // Arrange: Manager of Branch 1
        _currentUser.SetUser(2, "manager1", "Quản Lý 1", UserRole.Manager, branchId: 1, branchName: "Cơ sở Cầu Giấy", customerId: null);
        var today = DateTime.Today;

        // Act & Assert
        var actRev = async () => await _reportService.GetBranchRevenueComparisonAsync(today, today);
        await actRev.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Chỉ Quản trị viên*");

        var actOcc = async () => await _reportService.GetBranchOccupancyComparisonAsync(today, today);
        await actOcc.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Chỉ Quản trị viên*");
    }

    [Fact]
    public async Task Staff_Or_Customer_CallingReports_ThrowsUnauthorizedAccessException()
    {
        // Staff
        _currentUser.SetUser(3, "staff1", "Nhân Viên", UserRole.Staff, branchId: 1, branchName: "Cơ sở Cầu Giấy", customerId: null);
        var actStaff = async () => await _reportService.GetDailyRevenueAsync(1, DateTime.Today, DateTime.Today);
        await actStaff.Should().ThrowAsync<UnauthorizedAccessException>();

        // Customer
        _currentUser.SetUser(4, "customer1", "Khách Hàng", UserRole.Customer, branchId: null, branchName: null, customerId: 1);
        var actCustomer = async () => await _reportService.GetOverviewSummaryAsync(null, DateTime.Today, DateTime.Today);
        await actCustomer.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CourtUtilization_CalculatesCorrectRates()
    {
        // Arrange
        _currentUser.SetUser(1, "admin", "Hệ Thống Admin", UserRole.Admin, branchId: null, branchName: null, customerId: null);
        var today = DateTime.Today;

        // Act
        var list = await _reportService.GetCourtUtilizationAsync(1, today, today);

        // Assert
        list.Should().HaveCount(2); // Court 1 and Court 2 in branch 1
        var c1 = list.First(x => x.CourtId == 1);
        c1.BookedMinutes.Should().Be(120); // 2 hours
        c1.TotalOpenMinutes.Should().Be(18 * 60); // 1080 min

        var c2 = list.First(x => x.CourtId == 2);
        c2.BookedMinutes.Should().Be(60); // 1 hour
    }

    [Fact]
    public async Task TopCustomers_ReturnsOrderedByTotalSpent()
    {
        // Arrange
        _currentUser.SetUser(1, "admin", "Hệ Thống Admin", UserRole.Admin, branchId: null, branchName: null, customerId: null);

        // Act
        var top = await _reportService.GetTopCustomersAsync(null, 10);

        // Assert - Only Completed bookings count: Cust 2 has 350.000, Cust 1 has 200.000 (Booking 2 was CheckedIn, not completed yet)
        top.Should().HaveCount(2);
        top[0].CustomerId.Should().Be(2);
        top[0].TotalSpent.Should().Be(350000m);
        top[1].CustomerId.Should().Be(1);
        top[1].TotalSpent.Should().Be(200000m);
    }
}
