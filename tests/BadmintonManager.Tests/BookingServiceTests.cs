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

public class BookingServiceTests
{
    private async Task<(TestDbContextFactory factory, CurrentUserContext currentUser, BookingService bookingService, PricingService pricingService)> CreateTestSetupAsync(
        UserRole role = UserRole.Customer,
        int? branchId = 1,
        int? customerId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        currentUser.SetUser(1, "testuser", "Test User", role, branchId, "Cơ sở Cầu Giấy", customerId, "Vàng");

        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, currentUser);
        var bookingService = new BookingService(factory, currentUser, pricingService, settingService);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        return (factory, currentUser, bookingService, pricingService);
    }

    private static DateTime GetFutureDate(int daysInFuture = 7)
    {
        // Chọn ngày trong tương lai để tránh vi phạm BR-05 (không đặt trong quá khứ)
        return DateTime.Today.AddDays(daysInFuture);
    }

    #region BR-01: Khung giờ & Bước nhảy thời gian (06:00 - 24:00, phút 00/30)

    [Fact]
    public async Task CreateBooking_BR01_MinuteNot00Or30_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req1 = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8).AddMinutes(15), // 08:15 (sai mốc)
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        };

        var req2 = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(9).AddMinutes(45), // 09:45 (sai mốc)
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act1 = async () => await bookingService.CreateBookingAsync(req1);
        await act1.Should().ThrowAsync<BusinessException>()
            .WithMessage("*phải là mốc đúng giờ (00 phút) hoặc rưỡi (30 phút)*");

        var act2 = async () => await bookingService.CreateBookingAsync(req2);
        await act2.Should().ThrowAsync<BusinessException>()
            .WithMessage("*phải là mốc đúng giờ (00 phút) hoặc rưỡi (30 phút)*");
    }

    [Fact]
    public async Task CreateBooking_BR01_EndTimeBeforeOrEqualStartTime_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(10),
            EndTime = date.AddHours(9), // End < Start
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Thời gian kết thúc phải lớn hơn thời gian bắt đầu*");
    }

    [Fact]
    public async Task CreateBooking_BR01_BeforeOpeningHour_0600_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(5).AddMinutes(30), // 05:30 (trước 06:00)
            EndTime = date.AddHours(7),
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*khung giờ hoạt động (06:00 - 24:00)*");
    }

    [Fact]
    public async Task CreateBooking_BR01_AfterClosingHour_2400_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(22),
            EndTime = date.AddDays(1).AddMinutes(30), // 00:30 hôm sau (vượt quá 24:00)
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*khung giờ hoạt động (06:00 - 24:00)*");
    }

    [Fact]
    public async Task CreateBooking_BR01_ValidBookingAtClosing_2400_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(22),
            EndTime = date.AddDays(1), // 24:00 (chuẩn đóng cửa)
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(date.AddHours(22));
        result.EndTime.Should().Be(date.AddDays(1));
    }

    #endregion

    #region BR-02: Thời lượng đặt sân (60 - 240 phút)

    [Fact]
    public async Task CreateBooking_BR02_DurationUnder60Minutes_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(8).AddMinutes(30), // Chỉ 30 phút (< 60 phút)
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Thời lượng đặt sân phải từ 60 phút*");
    }

    [Fact]
    public async Task CreateBooking_BR02_DurationOver240Minutes_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(13), // 5 giờ = 300 phút (> 240 phút)
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*tối đa 4 giờ (240 phút)*");
    }

    [Fact]
    public async Task CreateBooking_BR02_MinDuration60Minutes_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(9), // Đúng 60 phút
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        (result.EndTime - result.StartTime).TotalMinutes.Should().Be(60);
    }

    [Fact]
    public async Task CreateBooking_BR02_MaxDuration240Minutes_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(12), // Đúng 240 phút (4 tiếng)
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        (result.EndTime - result.StartTime).TotalMinutes.Should().Be(240);
    }

    #endregion

    #region BR-03: Chống trùng lịch đặt sân (Overlap detection)

    [Fact]
    public async Task CreateBooking_BR03_ExactOverlap_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Booking 1: 08:00 - 10:00
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đặt cùng khung giờ 08:00 - 10:00 trên Sân 1
        var act = async () => await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Khung giờ này đã có người đặt trên sân đã chọn*");
    }

    [Fact]
    public async Task CreateBooking_BR03_PartialOverlapStart_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Booking 1: 08:00 - 10:00
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đặt đè phần đầu: 07:00 - 09:00
        var act = async () => await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(7),
            EndTime = date.AddHours(9),
            BookingType = BookingType.Single
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Khung giờ này đã có người đặt trên sân đã chọn*");
    }

    [Fact]
    public async Task CreateBooking_BR03_PartialOverlapEnd_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Booking 1: 08:00 - 10:00
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đặt đè phần đuôi: 09:00 - 11:00
        var act = async () => await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(9),
            EndTime = date.AddHours(11),
            BookingType = BookingType.Single
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Khung giờ này đã có người đặt trên sân đã chọn*");
    }

    [Fact]
    public async Task CreateBooking_BR03_EnclosingOverlap_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Booking 1: 09:00 - 10:00
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(9),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đặt bao trùm cả đơn cũ: 08:00 - 11:00
        var act = async () => await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(11),
            BookingType = BookingType.Single
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Khung giờ này đã có người đặt trên sân đã chọn*");
    }

    [Fact]
    public async Task CreateBooking_BR03_SameTimeDifferentCourt_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Đặt Sân 1: 08:00 - 10:00
        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đặt Sân 2 cùng khung giờ 08:00 - 10:00
        var result = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 2,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Assert
        result.Should().NotBeNull();
        result.CourtId.Should().Be(2);
    }

    [Fact]
    public async Task CreateBooking_BR03_CancelledBookingIgnored_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        // Tạo đơn 1: 08:00 - 10:00
        var b1 = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Hủy đơn 1
        await bookingService.CancelBookingAsync(new CancelBookingRequest
        {
            BookingId = b1.Id,
            Reason = "Khách bận việc đột xuất"
        });

        // Act: Đặt lại đúng khung giờ 08:00 - 10:00 sau khi đơn cũ đã hủy
        var b2 = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Assert
        b2.Should().NotBeNull();
        b2.Id.Should().NotBe(b1.Id);
    }

    #endregion

    #region BR-04: Quy tắc khoảng trống 30 phút (Gap Rule A4)

    [Fact]
    public async Task CreateBooking_BR04_30MinGapAtDayStart_0600To0630_ThrowsBusinessException()
    {
        // Arrange: Đặt lúc 06:30 - 08:00, để lại khoảng trống 06:00 - 06:30 (30 phút từ lúc mở cửa)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(6).AddMinutes(30), // 06:30
            EndTime = date.AddHours(8),
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút*");
    }

    [Fact]
    public async Task CreateBooking_BR04_0MinGapAtDayStart_0600To0800_Succeeds()
    {
        // Arrange: Đặt sát giờ mở cửa 06:00 (khoảng trống 0 phút)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(6), // 06:00
            EndTime = date.AddHours(8),
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(date.AddHours(6));
    }

    [Fact]
    public async Task CreateBooking_BR04_60MinGapAtDayStart_0700To0900_Succeeds()
    {
        // Arrange: Đặt từ 07:00 (để lại 06:00 - 07:00 = 60 phút >= 60 phút)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(7), // 07:00
            EndTime = date.AddHours(9),
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBooking_BR04_30MinGapBetweenBookings_ThrowsBusinessException()
    {
        // Arrange: Đơn 1: 08:00 - 09:30.
        // Đơn 2 cố đặt: 10:00 - 11:30 (để hở 09:30 - 10:00 = 30 phút giữa 2 đơn)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(9).AddMinutes(30),
            BookingType = BookingType.Single
        });

        // Act
        var act = async () => await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(10), // 10:00 (cách 09:30 đúng 30 phút)
            EndTime = date.AddHours(11).AddMinutes(30),
            BookingType = BookingType.Single
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút*");
    }

    [Fact]
    public async Task CreateBooking_BR04_0MinGapBetweenBookings_Consecutive_Succeeds()
    {
        // Arrange: Đơn 1: 08:00 - 09:30.
        // Đơn 2: 09:30 - 11:00 (nối tiếp ngay sau, khoảng trống 0 phút)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(9).AddMinutes(30),
            BookingType = BookingType.Single
        });

        // Act
        var result = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(9).AddMinutes(30), // Nối liền sát giờ
            EndTime = date.AddHours(11),
            BookingType = BookingType.Single
        });

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(date.AddHours(9).AddMinutes(30));
    }

    [Fact]
    public async Task CreateBooking_BR04_60MinGapBetweenBookings_Succeeds()
    {
        // Arrange: Đơn 1: 08:00 - 09:30.
        // Đơn 2: 10:30 - 12:00 (chừa 09:30 - 10:30 = 60 phút >= 1 giờ)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(9).AddMinutes(30),
            BookingType = BookingType.Single
        });

        // Act
        var result = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(10).AddMinutes(30), // 10:30 (chừa 1h)
            EndTime = date.AddHours(12),
            BookingType = BookingType.Single
        });

        // Assert
        result.Should().NotBeNull();
        result.StartTime.Should().Be(date.AddHours(10).AddMinutes(30));
    }

    [Fact]
    public async Task CreateBooking_BR04_30MinGapAtDayEnd_2330To2400_ThrowsBusinessException()
    {
        // Arrange: Đặt 22:00 - 23:30, để hở 23:30 - 24:00 (30 phút trước giờ đóng cửa)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(22),
            EndTime = date.AddHours(23).AddMinutes(30), // Kết thúc 23:30
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút*");
    }

    [Fact]
    public async Task CreateBooking_BR04_0MinGapAtDayEnd_2400_Succeeds()
    {
        // Arrange: Đặt 22:30 - 24:00 (kết thúc ngay mốc đóng cửa 24:00)
        var (_, _, bookingService, _) = await CreateTestSetupAsync();
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(22).AddMinutes(30),
            EndTime = date.AddDays(1), // 24:00
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.EndTime.Should().Be(date.AddDays(1));
    }

    #endregion

    #region BR-05: Chống đặt trong quá khứ & Khách vãng lai (Walk-in)

    [Fact]
    public async Task CreateBooking_BR05_PastTimeForCustomer_ThrowsBusinessException()
    {
        // Arrange: Thời gian 1 ngày trong quá khứ
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Customer);
        var pastDate = DateTime.Today.AddDays(-1);

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = pastDate.AddHours(8),
            EndTime = pastDate.AddHours(10),
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không thể đặt sân trong quá khứ*");
    }

    [Fact]
    public async Task CreateBooking_BR05_WalkInPastSlot_ThrowsBusinessException()
    {
        // Arrange: Nhân viên đặt WalkIn nhưng chọn ngày hôm qua
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var yesterday = DateTime.Today.AddDays(-1);

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = yesterday.AddHours(8),
            EndTime = yesterday.AddHours(10),
            BookingType = BookingType.WalkIn,
            GuestName = "Khách vãng lai"
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không thể đặt sân cho khách vãng lai trước thời điểm hiện tại*");
    }

    [Fact]
    public async Task CreateBooking_BR05_WalkInFutureSlot_Succeeds()
    {
        // Arrange: Nhân viên đặt WalkIn cho khách trực tiếp
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.WalkIn,
            GuestName = "Nguyễn Văn Khách"
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.BookingType.Should().Be(BookingType.WalkIn);
        result.Status.Should().Be(BookingStatus.CheckedIn); // A7: Walk-in tự động chuyển CheckedIn
    }

    #endregion

    #region A7 & Phân quyền chi nhánh (Role and Initial Status)

    [Fact]
    public async Task CreateBooking_A7_Customer_CreatesPendingStatus()
    {
        // Arrange: Khách hàng tự đặt sân qua app
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Customer, customerId: 1);
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Pending); // A7: Khách tự đặt -> Pending
        result.CustomerId.Should().Be(1);
    }

    [Fact]
    public async Task CreateBooking_A7_Staff_CreatesConfirmedStatus()
    {
        // Arrange: Nhân viên tạo đơn cho khách qua điện thoại
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        };

        // Act
        var result = await bookingService.CreateBookingAsync(req);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(BookingStatus.Confirmed); // A7: Nhân viên tạo -> Confirmed
    }

    [Fact]
    public async Task CreateBooking_Staff_OtherBranchCourt_ThrowsForbiddenException()
    {
        // Arrange: Nhân viên thuộc Branch 1 (Cơ sở Cầu Giấy), nhưng cố đặt sân thuộc Branch 2 (Đống Đa: Sân 7-12)
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 8, // Sân 8 thuộc Branch 2 (Đống Đa)
            CustomerId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Bạn chỉ có thể đặt sân thuộc cơ sở của mình*");
    }

    [Fact]
    public async Task CreateBooking_Customer_WithoutCustomerId_ThrowsForbiddenException()
    {
        // Arrange: User có role Customer nhưng không liên kết CustomerId
        var (factory, currentUser, bookingService, _) = await CreateTestSetupAsync(UserRole.Customer, customerId: null);
        currentUser.SetUser(99, "badcustomer", "Bad Customer", UserRole.Customer, null, null, customerId: null);
        var date = GetFutureDate();

        var req = new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        };

        // Act & Assert
        var act = async () => await bookingService.CreateBookingAsync(req);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Không tìm thấy thông tin hồ sơ khách hàng*");
    }

    #endregion

    #region Update & Cancel Booking (A9, BR-14)

    [Fact]
    public async Task UpdateBooking_ValidTimeChange_Succeeds()
    {
        // Arrange
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate();

        var created = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Act: Đổi sang 10:00 - 12:00
        var updated = await bookingService.UpdateBookingAsync(new UpdateBookingRequest
        {
            BookingId = created.Id,
            CourtId = 1,
            StartTime = date.AddHours(10),
            EndTime = date.AddHours(12)
        });

        // Assert
        updated.Should().NotBeNull();
        updated.StartTime.Should().Be(date.AddHours(10));
        updated.EndTime.Should().Be(date.AddHours(12));
    }

    [Fact]
    public async Task UpdateBooking_Causing30MinGap_ThrowsBusinessException()
    {
        // Arrange: Sân 1 có đơn khác lúc 12:00 - 14:00
        var (_, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate();

        var b1 = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 2,
            StartTime = date.AddHours(12),
            EndTime = date.AddHours(14),
            BookingType = BookingType.Single
        });

        // Act: Đổi đơn 1 thành 09:30 - 11:30 (để hở 11:30 - 12:00 = 30 phút so với đơn 2)
        var act = async () => await bookingService.UpdateBookingAsync(new UpdateBookingRequest
        {
            BookingId = b1.Id,
            CourtId = 1,
            StartTime = date.AddHours(9).AddMinutes(30),
            EndTime = date.AddHours(11).AddMinutes(30)
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút*");
    }

    [Fact]
    public async Task CancelBooking_BR14_Over24Hours_CreatesRefundPayment()
    {
        // Arrange
        var (factory, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        var date = GetFutureDate(5); // 5 ngày tới (> 24 giờ)

        var created = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1,
            StartTime = date.AddHours(8),
            EndTime = date.AddHours(10),
            BookingType = BookingType.Single
        });

        // Thêm bản ghi thanh toán tiền cọc (50,000 VND)
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Payments.Add(new Payment
            {
                BookingId = created.Id,
                Amount = 50000m,
                Method = PaymentMethod.BankTransfer,
                Type = PaymentType.Deposit,
                ReferenceNo = "DEP-TEST-001",
                PaidAt = DateTime.Now,
                ReceivedByUserId = 1
            });
            await db.SaveChangesAsync();
        }

        // Act: Hủy đơn trước 24 giờ
        await bookingService.CancelBookingAsync(new CancelBookingRequest
        {
            BookingId = created.Id,
            Reason = "Khách hủy sớm"
        });

        // Assert: Trạng thái Cancelled và có bản ghi hoàn tiền (Refund)
        await using (var db = await factory.CreateDbContextAsync())
        {
            var cancelledBooking = await db.Bookings.Include(b => b.Payments).FirstAsync(b => b.Id == created.Id);
            cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);
            cancelledBooking.CancelReason.Should().Be("Khách hủy sớm");

            var refundPayment = cancelledBooking.Payments.FirstOrDefault(p => p.Type == PaymentType.Refund);
            refundPayment.Should().NotBeNull();
            refundPayment!.Amount.Should().Be(50000m);
        }
    }

    [Fact]
    public async Task CancelBooking_BR14_Under24Hours_NoRefundPayment()
    {
        // Arrange: Đơn đặt sát giờ (< 24 giờ tính từ hiện tại)
        // Lưu ý: Để tạo đơn có giờ bắt đầu cách DateTime.Now < 24h mà không phạm BR-05 (start in past),
        // ta tạo trực tiếp trong DB với StartTime = DateTime.Now.AddHours(4)
        var (factory, _, bookingService, _) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);

        int bookingId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            var booking = new Booking
            {
                Code = "BK-SOON-01",
                CourtId = 1,
                CustomerId = 1,
                BookingType = BookingType.Single,
                StartTime = DateTime.Now.AddHours(4), // Chỉ còn 4 tiếng (< 24h)
                EndTime = DateTime.Now.AddHours(6),
                Status = BookingStatus.Confirmed,
                SubTotal = 160000m,
                FinalAmount = 160000m,
                DepositAmount = 48000m,
                CreatedByUserId = 1,
                CreatedAt = DateTime.Now.AddHours(-1)
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            bookingId = booking.Id;

            // Đã nộp cọc 48,000 VND
            db.Payments.Add(new Payment
            {
                BookingId = bookingId,
                Amount = 48000m,
                Method = PaymentMethod.BankTransfer,
                Type = PaymentType.Deposit,
                ReferenceNo = "DEP-SOON-001",
                PaidAt = DateTime.Now.AddHours(-1),
                ReceivedByUserId = 1
            });
            await db.SaveChangesAsync();
        }

        // Act: Hủy đơn khi còn < 24 giờ
        await bookingService.CancelBookingAsync(new CancelBookingRequest
        {
            BookingId = bookingId,
            Reason = "Hủy muộn sát giờ"
        });

        // Assert: Trạng thái Cancelled và KHÔNG có bản ghi Refund (mất cọc theo BR-14)
        await using (var db = await factory.CreateDbContextAsync())
        {
            var booking = await db.Bookings.Include(b => b.Payments).FirstAsync(b => b.Id == bookingId);
            booking.Status.Should().Be(BookingStatus.Cancelled);

            var refundPayment = booking.Payments.FirstOrDefault(p => p.Type == PaymentType.Refund);
            refundPayment.Should().BeNull();
        }
    }

    #endregion
}
