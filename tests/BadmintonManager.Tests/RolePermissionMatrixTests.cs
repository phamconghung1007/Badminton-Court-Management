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

public class RolePermissionMatrixTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly BranchCourtService _branchCourtService;
    private readonly UserService _userService;
    private readonly SettingService _settingService;
    private readonly PricingService _pricingService;
    private readonly MembershipService _membershipService;
    private readonly BookingService _bookingService;
    private readonly PaymentService _paymentService;
    private readonly CustomerService _customerService;
    private readonly ReportService _reportService;

    public RolePermissionMatrixTests()
    {
        var dbName = $"Badminton_MatrixSec_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();

        _branchCourtService = new BranchCourtService(_factory, _currentUser);
        _userService = new UserService(_factory, _currentUser);
        _settingService = new SettingService(_factory);
        _pricingService = new PricingService(_factory, _settingService, _currentUser);
        _membershipService = new MembershipService(_factory, _currentUser);
        _bookingService = new BookingService(_factory, _currentUser, _pricingService, _settingService);
        _paymentService = new PaymentService(_factory, _currentUser, _membershipService);
        _customerService = new CustomerService(_factory, _currentUser);
        _reportService = new ReportService(_factory, _currentUser);

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

        var cust1 = new Customer { Id = 1, FullName = "Khách Hàng A", Phone = "0911111111", MembershipTierId = 1, TotalPoints = 100, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        var cust2 = new Customer { Id = 2, FullName = "Khách Hàng B", Phone = "0922222222", MembershipTierId = 1, TotalPoints = 200, IsActive = true, CreatedAt = DateTime.Today.AddDays(-30) };
        db.Customers.AddRange(cust1, cust2);

        var admin = new AppUser { Id = 1, Username = "admin", Role = UserRole.Admin, FullName = "Quản trị viên", IsActive = true };
        var manager1 = new AppUser { Id = 2, Username = "manager1", Role = UserRole.Manager, FullName = "Quản lý 1", BranchId = 1, IsActive = true };
        var staff1 = new AppUser { Id = 3, Username = "staff1", Role = UserRole.Staff, FullName = "Nhân viên 1", BranchId = 1, IsActive = true };
        var custUser = new AppUser { Id = 4, Username = "cust1", Role = UserRole.Customer, FullName = "Khách 1", IsActive = true };
        db.Users.AddRange(admin, manager1, staff1, custUser);

        // PriceRule
        var r1 = new PriceRule { Id = 1, DayType = DayType.Weekday, StartMinute = 360, EndMinute = 1440, PricePerHour = 60000m, IsActive = true };
        var r2 = new PriceRule { Id = 2, DayType = DayType.Weekend, StartMinute = 360, EndMinute = 1440, PricePerHour = 80000m, IsActive = true };
        db.PriceRules.AddRange(r1, r2);

        // Booking
        var today = DateTime.Today.AddDays(1); // Tomorrow
        var bk1 = new Booking
        {
            Id = 1,
            Code = "BK-B1-01",
            CourtId = 1,
            CustomerId = 1,
            StartTime = today.AddHours(8),
            EndTime = today.AddHours(10),
            Status = BookingStatus.Pending,
            SubTotal = 120000m,
            FinalAmount = 120000m,
            DepositAmount = 36000m,
            CreatedByUserId = 4,
            CreatedAt = DateTime.Now
        };

        var bk2 = new Booking
        {
            Id = 2,
            Code = "BK-B2-01",
            CourtId = 3,
            CustomerId = 2,
            StartTime = today.AddHours(14),
            EndTime = today.AddHours(16),
            Status = BookingStatus.Confirmed,
            SubTotal = 120000m,
            FinalAmount = 120000m,
            DepositAmount = 36000m,
            CreatedByUserId = 3,
            CreatedAt = DateTime.Now
        };

        db.Bookings.AddRange(bk1, bk2);
        db.SaveChanges();
    }

    [Fact]
    public async Task Matrix_01_BranchManagement_AdminAllowed_OthersForbidden()
    {
        // Admin: Allowed
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var branch = await _branchCourtService.CreateBranchAsync(new SaveBranchRequest("Cơ sở Mới", "789 Hoàng Mai", "0987654399"));
        branch.Should().NotBeNull();

        // Manager: Forbidden
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var actManager = async () => await _branchCourtService.CreateBranchAsync(new SaveBranchRequest("Cơ sở Lậu", "---", "0999999999"));
        await actManager.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaff = async () => await _branchCourtService.ToggleBranchStatusAsync(1);
        await actStaff.Should().ThrowAsync<ForbiddenException>();

        // Customer: Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null);
        var actCustomer = async () => await _branchCourtService.CreateBranchAsync(new SaveBranchRequest("Cơ sở Khách", "---", "0999999999"));
        await actCustomer.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_02_CourtManagement_AdminAll_ManagerOwnOnly_OthersForbidden()
    {
        // Admin: Allowed for any branch
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var courtAdmin = await _branchCourtService.CreateCourtAsync(new SaveCourtRequest(2, "Sân 2 - Đống Đa"));
        courtAdmin.Should().NotBeNull();

        // Manager (Branch 1): Allowed for Branch 1
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var courtManager = await _branchCourtService.CreateCourtAsync(new SaveCourtRequest(1, "Sân 3 - Cầu Giấy"));
        courtManager.Should().NotBeNull();

        // Manager (Branch 1): Forbidden for Branch 2
        var actMgrBranch2 = async () => await _branchCourtService.CreateCourtAsync(new SaveCourtRequest(2, "Sân Gian"));
        await actMgrBranch2.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaff = async () => await _branchCourtService.CreateCourtAsync(new SaveCourtRequest(1, "Sân Staff"));
        await actStaff.Should().ThrowAsync<ForbiddenException>();

        // Customer: Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null);
        var actCustomer = async () => await _branchCourtService.CreateCourtAsync(new SaveCourtRequest(1, "Sân Khách"));
        await actCustomer.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_03_ManageManagerAccounts_AdminOnly()
    {
        // Admin: Allowed
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var newMgr = await _userService.CreateUserAsync(new CreateUserRequest("manager2", "123456", "Quản lý 2", UserRole.Manager, 2));
        newMgr.Should().NotBeNull();

        // Manager: Forbidden to create another Manager
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var actMgr = async () => await _userService.CreateUserAsync(new CreateUserRequest("manager3", "123456", "Quản lý 3", UserRole.Manager, 1));
        await actMgr.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaff = async () => await _userService.CreateUserAsync(new CreateUserRequest("manager4", "123456", "Quản lý 4", UserRole.Manager, 1));
        await actStaff.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_04_ManageStaffAccounts_AdminAll_ManagerOwnBranchOnly_OthersForbidden()
    {
        // Admin: Allowed for any branch
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var staffAdmin = await _userService.CreateUserAsync(new CreateUserRequest("staff_donda", "123456", "Staff Đống Đa", UserRole.Staff, 2));
        staffAdmin.Should().NotBeNull();

        // Manager: Allowed for own branch (Branch 1)
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var staffMgr = await _userService.CreateUserAsync(new CreateUserRequest("staff_caugiay_2", "123456", "Staff CG 2", UserRole.Staff, 1));
        staffMgr.Should().NotBeNull();

        // Manager: Forbidden for other branch (Branch 2)
        var actMgrOther = async () => await _userService.CreateUserAsync(new CreateUserRequest("staff_other", "123456", "Staff Sai", UserRole.Staff, 2));
        await actMgrOther.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaff = async () => await _userService.CreateUserAsync(new CreateUserRequest("staff_staff", "123456", "Staff", UserRole.Staff, 1));
        await actStaff.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_05_PriceAndTierConfiguration_AdminOnly()
    {
        // Admin: Allowed
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var savePriceAct = async () => await _pricingService.SavePriceRuleAsync(new SavePriceRuleRequest
        {
            Id = null,
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 50000m,
            IsActive = true
        });
        // Might throw BusinessException if overlapping rule, but NOT ForbiddenException
        try { await savePriceAct(); } catch (BusinessException) { }

        // Manager: Forbidden
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var actMgrPrice = async () => await _pricingService.SavePriceRuleAsync(new SavePriceRuleRequest
        {
            Id = null,
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 50000m,
            IsActive = true
        });
        await actMgrPrice.Should().ThrowAsync<ForbiddenException>();

        var actMgrTier = async () => await _membershipService.SaveTierAsync(new SaveMembershipTierRequest
        {
            Id = null,
            Name = "Kim Cương",
            MinPoints = 5000,
            DiscountPercent = 15m,
            SortOrder = 4
        });
        await actMgrTier.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaffPrice = async () => await _pricingService.SavePriceRuleAsync(new SavePriceRuleRequest
        {
            Id = null,
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 50000m,
            IsActive = true
        });
        await actStaffPrice.Should().ThrowAsync<ForbiddenException>();

        // Customer: Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null);
        var actCustPrice = async () => await _pricingService.SavePriceRuleAsync(new SavePriceRuleRequest
        {
            Id = null,
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 50000m,
            IsActive = true
        });
        await actCustPrice.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_06_ViewBookings_ScopedProperlyAcrossAllRoles()
    {
        // Admin: sees bookings of all branches
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var adminBookings = await _bookingService.GetBookingsAsync(null, null);
        adminBookings.Should().HaveCount(2);

        // Manager (Branch 1): only sees Branch 1 bookings
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var mgrBookings = await _bookingService.GetBookingsAsync(null, null);
        mgrBookings.Should().HaveCount(1);
        mgrBookings[0].CourtId.Should().Be(1);

        // Staff (Branch 1): only sees Branch 1 bookings
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var staffBookings = await _bookingService.GetBookingsAsync(null, null);
        staffBookings.Should().HaveCount(1);
        staffBookings[0].CourtId.Should().Be(1);

        // Customer (Customer 1): only sees their own bookings
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var custBookings = await _bookingService.GetBookingsAsync(null, null);
        custBookings.Should().HaveCount(1);
        custBookings[0].CustomerId.Should().Be(1);
    }

    [Fact]
    public async Task Matrix_08_ApprovePendingBooking_StaffManagerAdminAllowed_CustomerForbidden()
    {
        // Customer: Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var actCust = async () => await _bookingService.ConfirmBookingAsync(1);
        await actCust.Should().ThrowAsync<ForbiddenException>();

        // Staff: Allowed
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        await _bookingService.ConfirmBookingAsync(1);

        var approved = await _bookingService.GetBookingByIdAsync(1);
        approved!.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Matrix_09_CheckInAndPayment_StaffManagerAdminAllowed_CustomerForbidden()
    {
        // Customer: Forbidden to process payment directly
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var actCustPay = async () => await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = 1,
            Amount = 36000m,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Deposit
        });
        await actCustPay.Should().ThrowAsync<ForbiddenException>();

        // Staff: Allowed
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var payResult = await _paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = 1,
            Amount = 36000m,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Deposit
        });
        payResult.PaymentId.Should().BeGreaterThan(0);

        // Staff check-in: Allowed
        await _bookingService.CheckInAsync(1);
        var checkedIn = await _bookingService.GetBookingByIdAsync(1);
        checkedIn!.Status.Should().Be(BookingStatus.CheckedIn);
    }

    [Fact]
    public async Task Matrix_10_CustomerManagement_StaffManagerAdminAllowed_CustomerForbiddenSearch()
    {
        // Staff/Manager/Admin: Allowed to search customers
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var staffSearch = await _customerService.SearchCustomersAsync("Khách");
        staffSearch.Should().HaveCount(2);

        // Customer: Forbidden to search customer directory
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var actCustSearch = async () => await _customerService.SearchCustomersAsync("Khách");
        await actCustSearch.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Matrix_11_RevenueReports_AdminAll_ManagerOwnBranch_StaffAndCustomerForbidden()
    {
        var today = DateTime.Today;

        // Admin: Allowed all
        _currentUser.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var adminDaily = await _reportService.GetDailyRevenueAsync(null, today, today.AddDays(2));
        adminDaily.Should().NotBeNull();

        // Manager: Allowed own branch, Forbidden other branch
        _currentUser.SetUser(2, "manager1", "Manager", UserRole.Manager, 1);
        var mgrDaily = await _reportService.GetDailyRevenueAsync(1, today, today.AddDays(2));
        mgrDaily.Should().NotBeNull();

        var actMgrOther = async () => await _reportService.GetDailyRevenueAsync(2, today, today.AddDays(2));
        await actMgrOther.Should().ThrowAsync<ForbiddenException>();

        // Staff: Forbidden
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        var actStaff = async () => await _reportService.GetDailyRevenueAsync(1, today, today.AddDays(2));
        await actStaff.Should().ThrowAsync<UnauthorizedAccessException>();

        // Customer: Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var actCust = async () => await _reportService.GetDailyRevenueAsync(null, today, today.AddDays(2));
        await actCust.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Matrix_12_CancelBooking_CustomerCannotCancelOthers_StaffAllowed()
    {
        // Customer 1 tries to cancel Booking 2 (belongs to Customer 2): Forbidden
        _currentUser.SetUser(4, "cust1", "Customer", UserRole.Customer, null, null, customerId: 1);
        var actCustCancelOther = async () => await _bookingService.CancelBookingAsync(new CancelBookingRequest { BookingId = 2, Reason = "Hủy nhầm" });
        await actCustCancelOther.Should().ThrowAsync<ForbiddenException>();

        // Staff can cancel
        _currentUser.SetUser(3, "staff1", "Staff", UserRole.Staff, 1);
        await _bookingService.CancelBookingAsync(new CancelBookingRequest { BookingId = 1, Reason = "Khách yêu cầu hủy qua điện thoại" });
        var bk = await _bookingService.GetBookingByIdAsync(1);
        bk!.Status.Should().Be(BookingStatus.Cancelled);
    }
}
