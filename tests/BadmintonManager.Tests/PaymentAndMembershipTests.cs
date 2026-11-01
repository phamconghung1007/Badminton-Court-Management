using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class PaymentAndMembershipTests
{
    private async Task<(IDbContextFactory<AppDbContext> factory, CurrentUserContext userContext, PaymentService paymentService, BookingService bookingService)> CreateTestSetupAsync(
        UserRole role = UserRole.Staff,
        int? branchId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(options);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var userContext = new CurrentUserContext();
        userContext.SetUser(3, "staff1", "Nhân viên Cầu Giấy", role, branchId, "Cơ sở Cầu Giấy");

        var membershipService = new MembershipService(factory, userContext);
        var paymentService = new PaymentService(factory, userContext, membershipService);
        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, userContext);
        var bookingService = new BookingService(factory, userContext, pricingService, settingService);

        return (factory, userContext, paymentService, bookingService);
    }

    [Fact]
    public async Task ProcessPayment_PayDeposit_PendingBooking_UpdatesStatusToConfirmed()
    {
        // Arrange
        var (factory, userContext, paymentService, bookingService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = targetDay.AddHours(8),
            EndTime = targetDay.AddHours(10),
            GuestName = "Khách Hàng Đặt Cọc",
            GuestPhone = "0988112233"
        });

        // Đưa về trạng thái Pending để mô phỏng đơn khách đặt online
        await using (var db = await factory.CreateDbContextAsync())
        {
            var b = await db.Bookings.FindAsync(createResult.Id);
            b!.Status = BookingStatus.Pending;
            await db.SaveChangesAsync();
        }

        // Act: Thu tiền cọc
        var result = await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = createResult.DepositAmount,
            Method = PaymentMethod.BankTransfer,
            Type = PaymentType.Deposit,
            ReferenceNo = "VNPAY-123456",
            Note = "Chuyển khoản cọc 30%"
        });

        // Assert
        result.NewStatus.Should().Be(BookingStatus.Confirmed);
        result.AmountPaid.Should().Be(createResult.DepositAmount);
        result.RemainingAmount.Should().Be(createResult.FinalAmount - createResult.DepositAmount);

        var summary = await paymentService.GetPaymentSummaryAsync(createResult.Id);
        summary.Status.Should().Be(BookingStatus.Confirmed);
        summary.TotalPaid.Should().Be(createResult.DepositAmount);
        summary.RemainingAmount.Should().Be(createResult.FinalAmount - createResult.DepositAmount);
        summary.Payments.Should().ContainSingle();
        summary.Payments[0].Type.Should().Be(PaymentType.Deposit);
    }

    [Fact]
    public async Task ProcessPayment_PayRemaining_CompletedStatus_AndAwardsPoints()
    {
        // Arrange: Khách hàng customer3 (Lê Hoàng Cường, Hạng Đồng, 120 điểm ban đầu)
        var (factory, userContext, paymentService, bookingService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 3, // Customer 3
            StartTime = targetDay.AddHours(9),
            EndTime = targetDay.AddHours(11), // 2 tiếng (FinalAmount = 160.000)
        });

        decimal finalAmount = createResult.FinalAmount;
        int expectedPoints = (int)Math.Floor(finalAmount / 10000m); // 16 điểm

        // Act: Thanh toán toàn bộ 100% tiền sân
        var result = await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = finalAmount,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final,
            Note = "Thanh toán đủ khi nhận sân"
        });

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.NewStatus.Should().Be(BookingStatus.Completed);
        result.RemainingAmount.Should().Be(0);
        result.PointsEarned.Should().Be(expectedPoints);

        // Kiểm tra database: Điểm của customer 3 đã được cộng chính xác
        await using (var db = await factory.CreateDbContextAsync())
        {
            var customer = await db.Customers.FindAsync(3);
            customer!.TotalPoints.Should().Be(120 + expectedPoints);

            var pointTx = await db.PointTransactions.FirstOrDefaultAsync(p => p.BookingId == createResult.Id);
            pointTx.Should().NotBeNull();
            pointTx!.Points.Should().Be(expectedPoints);
        }
    }

    [Fact]
    public async Task ProcessPayment_CompletingBooking_TriggersMembershipTierUpgrade()
    {
        // Arrange: Giả định customer 3 đang có 490 điểm (ngưỡng Bạc là 500 điểm)
        var (factory, userContext, paymentService, bookingService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        await using (var db = await factory.CreateDbContextAsync())
        {
            var customer = await db.Customers.FindAsync(3);
            customer!.TotalPoints = 490; // Chỉ thiếu 10 điểm để lên Bạc (MinPoints = 500)
            await db.SaveChangesAsync();
        }

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 3,
            StartTime = targetDay.AddHours(14),
            EndTime = targetDay.AddHours(16) // 2 tiếng (120.000đ -> 12 điểm -> 490 + 12 = 502 >= 500)
        });

        int expectedPoints = (int)Math.Floor(createResult.FinalAmount / 10000m);

        // Act: Hoàn tất thanh toán
        var result = await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = createResult.FinalAmount,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final
        });

        // Assert
        result.IsCompleted.Should().BeTrue();
        result.IsTierUpgraded.Should().BeTrue();
        result.NewTierName.Should().Be("Bạc");

        await using (var db = await factory.CreateDbContextAsync())
        {
            var customer = await db.Customers.Include(c => c.MembershipTier).FirstAsync(c => c.Id == 3);
            customer.MembershipTier.Name.Should().Be("Bạc");
            customer.TotalPoints.Should().Be(490 + expectedPoints);
        }
    }

    [Fact]
    public async Task ProcessPayment_StaffCannotPayForBookingOfAnotherBranch()
    {
        // Arrange: Staff cơ sở 1 (Cầu Giấy) nhưng booking tạo ở Cơ sở 2 (Sân 7 thuộc CS Đống Đa)
        var (factory, userContext, paymentService, bookingService) = await CreateTestSetupAsync(UserRole.Staff, branchId: 1);
        DateTime targetDay = DateTime.Today.AddDays(1);

        // Tạo booking ở Sân 7 bằng quyền Admin
        var adminContext = new CurrentUserContext();
        adminContext.SetUser(1, "admin", "Admin", UserRole.Admin, null);
        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, adminContext);
        var adminBookingService = new BookingService(factory, adminContext, pricingService, settingService);

        var createResult = await adminBookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 7, // Sân 7 thuộc Branch 2
            StartTime = targetDay.AddHours(9),
            EndTime = targetDay.AddHours(10),
            GuestName = "Khách CS 2",
            GuestPhone = "0977889900"
        });

        // Act: Staff của Branch 1 cố thanh toán cho đơn của Branch 2
        var act = async () => await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = createResult.DepositAmount,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Deposit
        });

        // Assert: Bị chặn bởi ForbiddenException
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*cơ sở của mình*");
    }

    [Fact]
    public async Task ProcessPayment_CancelledBooking_CannotAcceptPayments()
    {
        // Arrange
        var (factory, userContext, paymentService, bookingService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            StartTime = targetDay.AddHours(8),
            EndTime = targetDay.AddHours(9),
            GuestName = "Khách Hủy",
            GuestPhone = "0911223344"
        });

        await bookingService.CancelBookingAsync(new CancelBookingRequest
        {
            BookingId = createResult.Id,
            Reason = "Khách bận"
        });

        // Act: Cố thu tiền cho đơn đã hủy
        var act = async () => await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = 50000m,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final
        });

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã bị hủy*");
    }
}
