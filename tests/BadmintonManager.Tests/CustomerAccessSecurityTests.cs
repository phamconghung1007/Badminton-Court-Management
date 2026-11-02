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

public class CustomerAccessSecurityTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly PricingService _pricingService;
    private readonly SettingService _settingService;
    private readonly BookingService _bookingService;
    private readonly CustomerService _customerService;
    private readonly PaymentService _paymentService;
    private readonly FixedScheduleService _fixedScheduleService;
    private readonly MembershipService _membershipService;

    public CustomerAccessSecurityTests()
    {
        var dbName = $"Badminton_CustomerSec_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _settingService = new SettingService(_factory);
        _pricingService = new PricingService(_factory, _settingService, _currentUser);
        _bookingService = new BookingService(_factory, _currentUser, _pricingService, _settingService);
        _customerService = new CustomerService(_factory, _currentUser);
        _membershipService = new MembershipService(_factory);
        _paymentService = new PaymentService(_factory, _currentUser, _membershipService);
        _fixedScheduleService = new FixedScheduleService(_factory, _currentUser, _pricingService, _settingService);

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var branch1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Now };
        db.Branches.Add(branch1);

        var court1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        var court2 = new Court { Id = 2, BranchId = 1, Name = "Sân 2", IsActive = true };
        db.Courts.AddRange(court1, court2);

        var tierDong = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        var tierVang = new MembershipTier { Id = 2, Name = "Vàng", MinPoints = 1500, DiscountPercent = 10, SortOrder = 3 };
        db.MembershipTiers.AddRange(tierDong, tierVang);

        var userCust1 = new AppUser { Id = 10, Username = "cust1", PasswordHash = "hash", FullName = "Nguyễn Văn Cust1", Role = UserRole.Customer, IsActive = true, CreatedAt = DateTime.Now };
        var userCust2 = new AppUser { Id = 20, Username = "cust2", PasswordHash = "hash", FullName = "Trần Thị Cust2", Role = UserRole.Customer, IsActive = true, CreatedAt = DateTime.Now };
        db.Users.AddRange(userCust1, userCust2);

        var customer1 = new Customer { Id = 100, FullName = "Nguyễn Văn Cust1", Phone = "0900000001", UserId = 10, MembershipTierId = 1, TotalPoints = 100, IsActive = true, CreatedAt = DateTime.Now };
        var customer2 = new Customer { Id = 200, FullName = "Trần Thị Cust2", Phone = "0900000002", UserId = 20, MembershipTierId = 2, TotalPoints = 1600, IsActive = true, CreatedAt = DateTime.Now };
        db.Customers.AddRange(customer1, customer2);

        // Bảng giá phủ 06:00 - 24:00
        db.PriceRules.Add(new PriceRule { Id = 1, DayType = DayType.Weekday, StartMinute = 360, EndMinute = 1440, PricePerHour = 100000m, IsActive = true });
        db.PriceRules.Add(new PriceRule { Id = 2, DayType = DayType.Weekend, StartMinute = 360, EndMinute = 1440, PricePerHour = 120000m, IsActive = true });

        // Cài đặt
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.OpenMinute, Value = "360" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.CloseMinute, Value = "1440" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MinDurationMinutes, Value = "60" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.MaxDurationMinutes, Value = "240" });
        db.SystemSettings.Add(new SystemSetting { Key = Constants.Settings.FreeCancelHours, Value = "24" });

        // Đơn của Customer 1
        db.Bookings.Add(new Booking
        {
            Id = 1001,
            Code = "BK-CUST1-01",
            CourtId = 1,
            CustomerId = 100,
            BookingType = BookingType.Single,
            StartTime = DateTime.Today.AddDays(2).AddHours(8),
            EndTime = DateTime.Today.AddDays(2).AddHours(10),
            Status = BookingStatus.Confirmed,
            SubTotal = 200000m,
            DiscountPercent = 0,
            DiscountAmount = 0,
            FinalAmount = 200000m,
            DepositAmount = 60000m,
            CreatedByUserId = 10,
            CreatedAt = DateTime.Now
        });

        // Đơn của Customer 2
        db.Bookings.Add(new Booking
        {
            Id = 2001,
            Code = "BK-CUST2-01",
            CourtId = 2,
            CustomerId = 200,
            BookingType = BookingType.Single,
            StartTime = DateTime.Today.AddDays(2).AddHours(14),
            EndTime = DateTime.Today.AddDays(2).AddHours(16),
            Status = BookingStatus.Confirmed,
            SubTotal = 200000m,
            DiscountPercent = 10,
            DiscountAmount = 20000m,
            FinalAmount = 180000m,
            DepositAmount = 54000m,
            CreatedByUserId = 20,
            CreatedAt = DateTime.Now
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task Day8_DoD_GetAvailableCourts_ReturnsOnlyFreeCourts_ExcludesOverlapsAndGaps()
    {
        // Arrange: Sân 1 có booking 08:00 - 10:00 ngày mai
        DateTime targetDate = DateTime.Today.AddDays(1);
        using (var db = _factory.CreateDbContext())
        {
            db.Bookings.Add(new Booking
            {
                Id = 999,
                Code = "BK-TEST-01",
                CourtId = 1,
                CustomerId = 100,
                BookingType = BookingType.Single,
                StartTime = targetDate.AddHours(8),
                EndTime = targetDate.AddHours(10),
                Status = BookingStatus.Confirmed,
                SubTotal = 200000m,
                FinalAmount = 200000m,
                DepositAmount = 60000m,
                CreatedByUserId = 1,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
        }

        // Test 1: Tìm 08:30 - 09:30 (Trùng Sân 1) -> Sân 1 bị loại, Sân 2 khả dụng
        var req1 = new SearchAvailableCourtsRequest
        {
            BranchId = 1,
            Date = targetDate,
            StartTime = TimeSpan.FromHours(8.5),
            EndTime = TimeSpan.FromHours(9.5)
        };
        var freeCourts1 = await _bookingService.GetAvailableCourtsAsync(req1);
        freeCourts1.Should().ContainSingle(c => c.CourtId == 2);
        freeCourts1.Should().NotContain(c => c.CourtId == 1);

        // Test 2: Tìm 10:30 - 12:00 (Để hở 30p từ 10:00 đến 10:30 trên Sân 1 theo BR-04) -> Sân 1 bị loại, Sân 2 khả dụng
        var req2 = new SearchAvailableCourtsRequest
        {
            BranchId = 1,
            Date = targetDate,
            StartTime = TimeSpan.FromHours(10.5),
            EndTime = TimeSpan.FromHours(12.0)
        };
        var freeCourts2 = await _bookingService.GetAvailableCourtsAsync(req2);
        freeCourts2.Should().ContainSingle(c => c.CourtId == 2);
        freeCourts2.Should().NotContain(c => c.CourtId == 1);

        // Test 3: Tìm 10:00 - 12:00 (Sát giờ liền kề với Sân 1) -> Cả Sân 1 và Sân 2 đều khả dụng
        var req3 = new SearchAvailableCourtsRequest
        {
            BranchId = 1,
            Date = targetDate,
            StartTime = TimeSpan.FromHours(10.0),
            EndTime = TimeSpan.FromHours(12.0)
        };
        var freeCourts3 = await _bookingService.GetAvailableCourtsAsync(req3);
        freeCourts3.Should().HaveCount(2);
        freeCourts3.Select(c => c.CourtId).Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public async Task Day8_DoD_Customer_CanOnlyViewOwnBookings_InList()
    {
        // Giả lập phiên đăng nhập là Customer 1
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var list = await _bookingService.GetBookingsAsync(null, null);

        list.Should().NotBeEmpty();
        list.Should().OnlyContain(b => b.CustomerId == 100);
        list.Should().NotContain(b => b.CustomerId == 200);
    }

    [Fact]
    public async Task Day8_DoD_Customer_ThrowsForbidden_WhenViewingAnotherCustomerBookingDetail()
    {
        // Customer 1 cố xem chi tiết đơn 2001 (của Customer 2)
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var act = async () => await _bookingService.GetBookingByIdAsync(2001);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền xem*");
    }

    [Fact]
    public async Task Day8_DoD_Customer_ThrowsForbidden_WhenCancelingAnotherCustomerBooking()
    {
        // Customer 1 cố hủy đơn 2001 (của Customer 2)
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var act = async () => await _bookingService.CancelBookingAsync(new CancelBookingRequest
        {
            BookingId = 2001,
            Reason = "Cố tình hủy trộm đơn của người khác"
        });

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền hủy đơn đặt sân của khách hàng khác*");
    }

    [Fact]
    public async Task Day8_DoD_Customer_ThrowsForbidden_WhenUpdatingAnotherCustomerBooking()
    {
        // Customer 1 cố sửa đơn 2001 (của Customer 2)
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var act = async () => await _bookingService.UpdateBookingAsync(new UpdateBookingRequest
        {
            BookingId = 2001,
            CourtId = 1,
            StartTime = DateTime.Today.AddDays(3).AddHours(14),
            EndTime = DateTime.Today.AddDays(3).AddHours(16)
        });

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền chỉnh sửa đơn đặt sân của khách hàng khác*");
    }

    [Fact]
    public async Task Day8_DoD_Customer_ThrowsForbidden_WhenAttemptingStaffActions()
    {
        // Customer cố thực hiện các hành động quản trị / lễ tân
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        // Duyệt đơn
        var actConfirm = async () => await _bookingService.ConfirmBookingAsync(1001);
        await actConfirm.Should().ThrowAsync<ForbiddenException>();

        // Check-in
        var actCheckIn = async () => await _bookingService.CheckInAsync(1001);
        await actCheckIn.Should().ThrowAsync<ForbiddenException>();

        // Thu tiền
        var actPayment = async () => await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = 1001,
            Amount = 140000m,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final
        });
        await actPayment.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Day8_DoD_Customer_ThrowsForbidden_WhenAccessingOtherCustomerProfiles()
    {
        // Customer 1 cố xem chi tiết hồ sơ Customer 2
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var actDetail = async () => await _customerService.GetCustomerDetailAsync(200);
        await actDetail.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền xem thông tin hồ sơ của khách hàng khác*");

        // Customer cố tra cứu danh bạ khách hàng
        var actSearch = async () => await _customerService.SearchCustomersAsync("Cust2");
        await actSearch.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền xem danh sách khách hàng*");

        // Customer cố tạo khách hàng tại quầy
        var actCreate = async () => await _customerService.CreateCustomerAsync(new CustomerCreateDto("Test", "0999999999", null));
        await actCreate.Should().ThrowAsync<ForbiddenException>();

        // Customer cố xóa khách hàng
        var actDelete = async () => await _customerService.DeleteCustomerAsync(200);
        await actDelete.Should().ThrowAsync<ForbiddenException>();

        // Customer cố khóa tài khoản
        var actToggle = async () => await _customerService.ToggleCustomerStatusAsync(200);
        await actToggle.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Day8_DoD_Customer_FixedSchedule_CrossAccessForbidden()
    {
        // Customer 1 tạo lịch cố định cho Customer 2
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100);

        var actCreate = async () => await _fixedScheduleService.CreateFixedScheduleAsync(new CreateFixedScheduleRequest
        {
            CourtId = 1,
            CustomerId = 200, // Của người khác
            DayOfWeek = 1,
            StartMinute = 480,
            EndMinute = 600
        });

        await actCreate.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*chỉ có quyền tạo lịch cố định cho chính mình*");
    }
}
