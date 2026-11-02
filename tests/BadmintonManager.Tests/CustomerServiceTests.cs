using System;
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

public class CustomerServiceTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public CustomerServiceTests()
    {
        var dbName = $"Badminton_CustTest_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(1, "staff1", "Staff One", UserRole.Staff, 1);

        SeedInitialData();
    }

    private void SeedInitialData()
    {
        using var db = _factory.CreateDbContext();
        var bronze = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        var silver = new MembershipTier { Id = 2, Name = "Bạc", MinPoints = 500, DiscountPercent = 5, SortOrder = 2 };
        var gold = new MembershipTier { Id = 3, Name = "Vàng", MinPoints = 1500, DiscountPercent = 10, SortOrder = 3 };
        db.MembershipTiers.AddRange(bronze, silver, gold);

        var branch = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Now };
        db.Branches.Add(branch);

        var court = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        db.Courts.Add(court);

        var cust1 = new Customer
        {
            Id = 1,
            FullName = "Nguyễn Văn A",
            Phone = "0911222333",
            MembershipTierId = 1,
            TotalPoints = 120,
            MembershipCardNo = "VIP-001",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var cust2 = new Customer
        {
            Id = 2,
            FullName = "Trần Thị B",
            Phone = "0922333444",
            MembershipTierId = 2,
            TotalPoints = 750,
            MembershipCardNo = "VIP-002",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        db.Customers.AddRange(cust1, cust2);
        db.SaveChanges();
    }

    [Fact]
    public async Task CreateCustomer_ValidRequest_Success_WithDefaultTierAndZeroPoints()
    {
        var service = new CustomerService(_factory, _currentUser);
        var request = new CustomerCreateDto("Lê Hoàng Nam", "0933444555", "VIP-003");

        var result = await service.CreateCustomerAsync(request);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Lê Hoàng Nam");
        result.Phone.Should().Be("0933444555");
        result.TierName.Should().Be("Đồng");
        result.TotalPoints.Should().Be(0);
        result.IsActive.Should().BeTrue();

        using var db = _factory.CreateDbContext();
        var saved = await db.Customers.FirstOrDefaultAsync(c => c.Phone == "0933444555");
        saved.Should().NotBeNull();
        saved!.FullName.Should().Be("Lê Hoàng Nam");
    }

    [Fact]
    public async Task CreateCustomer_DuplicatePhone_ThrowsBusinessException()
    {
        var service = new CustomerService(_factory, _currentUser);
        var request = new CustomerCreateDto("Người Khác", "0911222333", null);

        var act = async () => await service.CreateCustomerAsync(request);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã tồn tại trong danh sách khách hàng*");
    }

    [Fact]
    public async Task UpdateCustomer_ValidRequest_UpdatesInfo()
    {
        var service = new CustomerService(_factory, _currentUser);
        var request = new CustomerUpdateDto(1, "Nguyễn Văn A Đã Đổi Tên", "0911999888", "CARD-NEW", true);

        var result = await service.UpdateCustomerAsync(request);

        result.FullName.Should().Be("Nguyễn Văn A Đã Đổi Tên");
        result.Phone.Should().Be("0911999888");

        using var db = _factory.CreateDbContext();
        var saved = await db.Customers.FindAsync(1);
        saved!.FullName.Should().Be("Nguyễn Văn A Đã Đổi Tên");
        saved.Phone.Should().Be("0911999888");
    }

    [Fact]
    public async Task UpdateCustomer_PhoneTakenByAnotherCustomer_ThrowsBusinessException()
    {
        var service = new CustomerService(_factory, _currentUser);
        // Customer 1 cố tình đổi sang số điện thoại của Customer 2
        var request = new CustomerUpdateDto(1, "Nguyễn Văn A", "0922333444", null, true);

        var act = async () => await service.UpdateCustomerAsync(request);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã được sử dụng bởi khách hàng khác*");
    }

    [Fact]
    public async Task DeleteCustomer_SoftDeletes_SetsIsActiveFalse()
    {
        var service = new CustomerService(_factory, _currentUser);

        await service.DeleteCustomerAsync(1);

        using var db = _factory.CreateDbContext();
        var customer = await db.Customers.FindAsync(1);
        customer.Should().NotBeNull();
        customer!.IsActive.Should().BeFalse(); // BR-21: Xóa mềm
    }

    [Fact]
    public async Task GetCustomerDetail_ReturnsFullSummary_WithPointHistoryAndBookings()
    {
        using (var db = _factory.CreateDbContext())
        {
            var user = new AppUser { Id = 1, Username = "staff1", PasswordHash = "hash", Role = UserRole.Staff, FullName = "Staff 1", BranchId = 1, IsActive = true };
            db.Users.Add(user);

            var booking = new Booking
            {
                Id = 1,
                Code = "BK20261102-001",
                CourtId = 1,
                CustomerId = 1,
                StartTime = DateTime.Now.AddHours(-2),
                EndTime = DateTime.Now.AddHours(-1),
                Status = BookingStatus.Completed,
                SubTotal = 150000,
                FinalAmount = 150000,
                CreatedByUserId = 1,
                CreatedAt = DateTime.Now.AddHours(-3)
            };
            db.Bookings.Add(booking);

            var pt = new PointTransaction
            {
                Id = 1,
                CustomerId = 1,
                BookingId = 1,
                Points = 15,
                Reason = "Tích điểm hoàn thành đơn BK20261102-001",
                CreatedAt = DateTime.Now.AddHours(-1)
            };
            db.PointTransactions.Add(pt);
            db.SaveChanges();
        }

        var service = new CustomerService(_factory, _currentUser);
        var detail = await service.GetCustomerDetailAsync(1);

        detail.Should().NotBeNull();
        detail!.Customer.FullName.Should().Be("Nguyễn Văn A");
        detail.TotalBookings.Should().Be(1);
        detail.CompletedBookings.Should().Be(1);
        detail.TotalSpent.Should().Be(150000);
        detail.PointHistory.Should().HaveCount(1);
        detail.PointHistory.First().Points.Should().Be(15);
        detail.RecentBookings.Should().HaveCount(1);
        detail.RecentBookings.First().Code.Should().Be("BK20261102-001");
    }
}
