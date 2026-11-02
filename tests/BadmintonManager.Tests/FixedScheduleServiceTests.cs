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

public class FixedScheduleServiceTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly PricingService _pricingService;
    private readonly SettingService _settingService;

    public FixedScheduleServiceTests()
    {
        var dbName = $"Badminton_FixedSched_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(1, "staff1", "Staff One", UserRole.Staff, 1);

        _settingService = new SettingService(_factory);
        _pricingService = new PricingService(_factory, _settingService, _currentUser);

        SeedInitialData();
    }

    private void SeedInitialData()
    {
        using var db = _factory.CreateDbContext();

        var branch1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0987654321", IsActive = true, CreatedAt = DateTime.Now };
        var branch2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Đống Đa", Phone = "0987654322", IsActive = true, CreatedAt = DateTime.Now };
        db.Branches.AddRange(branch1, branch2);

        var court1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true };
        var court2 = new Court { Id = 2, BranchId = 2, Name = "Sân 2", IsActive = true };
        db.Courts.AddRange(court1, court2);

        var bronzeTier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        db.MembershipTiers.Add(bronzeTier);

        var cust1 = new Customer
        {
            Id = 1,
            FullName = "Nguyễn Văn Khách",
            Phone = "0912345678",
            MembershipTierId = 1,
            TotalPoints = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        db.Customers.Add(cust1);

        var user = new AppUser
        {
            Id = 1,
            Username = "staff1",
            PasswordHash = "hash",
            Role = UserRole.Staff,
            FullName = "Staff 1",
            BranchId = 1,
            IsActive = true
        };
        db.Users.Add(user);

        // Bảng giá phủ 06:00 - 24:00 (360 - 1440)
        db.PriceRules.AddRange(
            new PriceRule { Id = 1, DayType = DayType.Weekday, StartMinute = 360, EndMinute = 1440, PricePerHour = 100000, IsActive = true },
            new PriceRule { Id = 2, DayType = DayType.Weekend, StartMinute = 360, EndMinute = 1440, PricePerHour = 120000, IsActive = true }
        );

        // Cài đặt hệ thống
        db.SystemSettings.AddRange(
            new SystemSetting { Key = Constants.Settings.OpenMinute, Value = "360" },
            new SystemSetting { Key = Constants.Settings.CloseMinute, Value = "1440" },
            new SystemSetting { Key = Constants.Settings.MinDurationMinutes, Value = "60" },
            new SystemSetting { Key = Constants.Settings.MaxDurationMinutes, Value = "240" },
            new SystemSetting { Key = Constants.Settings.FixedWeeksAhead, Value = "4" }
        );

        db.SaveChanges();
    }

    [Fact]
    public async Task CreateFixedSchedule_ValidRequest_Generates4Bookings_AllConfirmed()
    {
        var service = new FixedScheduleService(_factory, _currentUser, _pricingService, _settingService);

        // Chọn ngày bắt đầu từ ngày mai để không dính quá khứ
        DateOnly startDay = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        int targetDayOfWeek = (int)startDay.DayOfWeek;

        var request = new CreateFixedScheduleRequest
        {
            CustomerId = 1,
            CourtId = 1,
            DayOfWeek = targetDayOfWeek,
            StartMinute = 480, // 08:00
            EndMinute = 600,   // 10:00 (120 phút)
            ValidFrom = startDay,
            NumberOfWeeks = 4
        };

        var result = await service.CreateFixedScheduleAsync(request);

        result.Should().NotBeNull();
        result.CreatedCount.Should().Be(4);
        result.SkippedDates.Should().BeEmpty();
        result.CreatedBookingIds.Should().HaveCount(4);

        using var db = _factory.CreateDbContext();
        var bookings = await db.Bookings.Where(b => b.FixedScheduleId == result.ScheduleId).ToListAsync();
        bookings.Should().HaveCount(4);
        bookings.All(b => b.Status == BookingStatus.Confirmed).Should().BeTrue();
        bookings.All(b => b.BookingType == BookingType.Fixed).Should().BeTrue();
        bookings.All(b => b.CourtId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task CreateFixedSchedule_WhenWeekHasConflict_SkipsOverlappingWeek_AndReportsSkippedDate()
    {
        DateOnly startDay = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        int targetDayOfWeek = (int)startDay.DayOfWeek;

        // Tạo sẵn 1 booking đơn lẻ ở tuần thứ 2 (startDay + 7 ngày) trên sân 1 từ 08:00 đến 10:00
        DateTime conflictStart = startDay.AddDays(7).ToDateTime(TimeOnly.MinValue).AddMinutes(480);
        DateTime conflictEnd = startDay.AddDays(7).ToDateTime(TimeOnly.MinValue).AddMinutes(600);

        using (var db = _factory.CreateDbContext())
        {
            var conflictBooking = new Booking
            {
                Code = "BK-CONFLICT-001",
                CourtId = 1,
                CustomerId = 1,
                StartTime = conflictStart,
                EndTime = conflictEnd,
                Status = BookingStatus.Confirmed,
                SubTotal = 200000,
                FinalAmount = 200000,
                CreatedByUserId = 1,
                CreatedAt = DateTime.Now
            };
            db.Bookings.Add(conflictBooking);
            db.SaveChanges();
        }

        var service = new FixedScheduleService(_factory, _currentUser, _pricingService, _settingService);
        var request = new CreateFixedScheduleRequest
        {
            CustomerId = 1,
            CourtId = 1,
            DayOfWeek = targetDayOfWeek,
            StartMinute = 480, // 08:00
            EndMinute = 600,   // 10:00
            ValidFrom = startDay,
            NumberOfWeeks = 4
        };

        var result = await service.CreateFixedScheduleAsync(request);

        // Kết quả: Sinh được 3 booking, 1 tuần bị bỏ qua (BR-18)
        result.CreatedCount.Should().Be(3);
        result.SkippedDates.Should().HaveCount(1);
        result.SkippedDates.First().Should().Be(startDay.AddDays(7));
        result.SkippedDetails.Should().HaveCount(1);
        result.SkippedDetails.First().Reason.Should().Contain("BK-CONFLICT-001");
    }

    [Fact]
    public async Task CreateFixedSchedule_CrossBranch_ThrowsForbiddenException()
    {
        var service = new FixedScheduleService(_factory, _currentUser, _pricingService, _settingService);

        // Staff cơ sở 1 cố tình tạo lịch cố định cho Sân 2 (thuộc Cơ sở 2)
        var request = new CreateFixedScheduleRequest
        {
            CustomerId = 1,
            CourtId = 2,
            DayOfWeek = 1,
            StartMinute = 480,
            EndMinute = 600,
            ValidFrom = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };

        var act = async () => await service.CreateFixedScheduleAsync(request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*chi nhánh của mình*");
    }

    [Fact]
    public async Task CancelFixedSchedule_CancelsFutureBookings_MarksScheduleInactive()
    {
        var service = new FixedScheduleService(_factory, _currentUser, _pricingService, _settingService);

        DateOnly startDay = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var request = new CreateFixedScheduleRequest
        {
            CustomerId = 1,
            CourtId = 1,
            DayOfWeek = (int)startDay.DayOfWeek,
            StartMinute = 540,
            EndMinute = 660,
            ValidFrom = startDay,
            NumberOfWeeks = 4
        };

        var result = await service.CreateFixedScheduleAsync(request);
        result.CreatedCount.Should().Be(4);

        // Thực hiện hủy lịch cố định (BR-19)
        await service.CancelFixedScheduleAsync(result.ScheduleId);

        using var db = _factory.CreateDbContext();
        var schedule = await db.FixedSchedules.FindAsync(result.ScheduleId);
        schedule!.IsActive.Should().BeFalse();

        var bookings = await db.Bookings.Where(b => b.FixedScheduleId == result.ScheduleId).ToListAsync();
        bookings.All(b => b.Status == BookingStatus.Cancelled).Should().BeTrue();
        bookings.All(b => b.CancelReason == "Hủy theo toàn bộ lịch cố định").Should().BeTrue();
    }

    [Fact]
    public async Task CreateBooking_WalkIn_CreatesCheckedInStatusImmediately()
    {
        var bookingService = new BookingService(_factory, _currentUser, _pricingService, _settingService);

        // Khách vãng lai check-in ngay (A12)
        // Thời gian bắt đầu từ giờ hiện tại làm tròn mốc 30 phút
        DateTime now = DateTime.Now;
        int roundMinute = now.Minute < 30 ? 0 : 30;
        DateTime start = new DateTime(now.Year, now.Month, now.Day, now.Hour, roundMinute, 0);
        DateTime end = start.AddMinutes(60);

        var request = new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = null, // Vãng lai không bắt buộc hồ sơ
            GuestName = "Khách Vãng Lai A12",
            GuestPhone = "0999888777",
            BookingType = BookingType.WalkIn,
            StartTime = start,
            EndTime = end
        };

        var booking = await bookingService.CreateBookingAsync(request);

        booking.Should().NotBeNull();
        booking.BookingType.Should().Be(BookingType.WalkIn);
        booking.Status.Should().Be(BookingStatus.CheckedIn); // A7 & A12: WalkIn check-in ngay lập tức
        booking.GuestName.Should().Be("Khách Vãng Lai A12");
        booking.PaidAmount.Should().Be(0); // Chưa thu tiền, có thể thu sau
    }
}
