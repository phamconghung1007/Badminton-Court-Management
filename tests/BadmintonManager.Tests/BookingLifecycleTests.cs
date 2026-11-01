using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using BadmintonManager.Common;
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

public class BookingLifecycleTests
{
    private async Task<(IDbContextFactory<AppDbContext> factory, CurrentUserContext userContext, BookingService bookingService, PaymentService paymentService, InvoiceService invoiceService, InvoicePrinter invoicePrinter)> CreateTestSetupAsync()
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
        userContext.SetUser(3, "staff1", "Lê Thu Staff", UserRole.Staff, 1, "Cơ sở Cầu Giấy");

        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, userContext);
        var bookingService = new BookingService(factory, userContext, pricingService, settingService);
        var membershipService = new MembershipService(factory, userContext);
        var paymentService = new PaymentService(factory, userContext, membershipService);
        var invoiceService = new InvoiceService(factory, userContext);
        var invoicePrinter = new InvoicePrinter();

        return (factory, userContext, bookingService, paymentService, invoiceService, invoicePrinter);
    }

    [Fact]
    public async Task Day6_DoD_FullBookingLifecycle_FromPendingToCompleted_InvoiceRendersAccurateData()
    {
        // =========================================================================
        // KIỂM CHỨNG TIÊU CHÍ NGHIỆM THU NGÀY 6 (DoD DAY 6):
        // "Chạy trọn vòng đời 1 booking từ Pending đến Completed; hóa đơn in/preview ra đúng số"
        // =========================================================================

        var (factory, userContext, bookingService, paymentService, invoiceService, invoicePrinter) = await CreateTestSetupAsync();
        DateTime playDate = DateTime.Today.AddDays(1);

        // BƯỚC 1: Khách hàng (Hội viên Vàng customer 1 - giảm 10%) đặt sân online -> Trạng thái ban đầu: Pending
        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1,
            StartTime = playDate.AddHours(8),
            EndTime = playDate.AddHours(10), // 2 tiếng (08:00 - 10:00)
            BookingType = BookingType.Single
        });

        // Thiết lập trạng thái ban đầu là Pending (mô phỏng đơn do khách đặt online trên web/app)
        await using (var db = await factory.CreateDbContextAsync())
        {
            var b = await db.Bookings.FindAsync(createResult.Id);
            b!.Status = BookingStatus.Pending;
            await db.SaveChangesAsync();
        }

        var step1Booking = await bookingService.GetBookingByIdAsync(createResult.Id);
        step1Booking.Status.Should().Be(BookingStatus.Pending);

        // BƯỚC 2: Khách đóng tiền cọc 30% (hoặc Lễ tân xác nhận cọc) -> Trạng thái chuyển thành Confirmed
        var depositPaymentResult = await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = step1Booking.DepositAmount,
            Method = PaymentMethod.BankTransfer,
            Type = PaymentType.Deposit,
            ReferenceNo = "MOMO-20261102-01",
            Note = "Chuyển khoản cọc 30%"
        });

        depositPaymentResult.NewStatus.Should().Be(BookingStatus.Confirmed);
        depositPaymentResult.AmountPaid.Should().Be(step1Booking.DepositAmount);

        var step2Booking = await bookingService.GetBookingByIdAsync(createResult.Id);
        step2Booking.Status.Should().Be(BookingStatus.Confirmed);

        // BƯỚC 3: Khách xin đổi sang Sân 2 và khung giờ 09:00 - 11:00 trước giờ bắt đầu
        var updateResult = await bookingService.UpdateBookingAsync(new UpdateBookingRequest
        {
            BookingId = createResult.Id,
            CourtId = 2, // Đổi sang Sân 2
            StartTime = playDate.AddHours(9),
            EndTime = playDate.AddHours(11) // 2 tiếng (09:00 - 11:00)
        });

        updateResult.CourtId.Should().Be(2);
        updateResult.CourtName.Should().Be("Sân 2");
        updateResult.StartTime.Should().Be(playDate.AddHours(9));
        updateResult.EndTime.Should().Be(playDate.AddHours(11));

        // BƯỚC 4: Đến giờ chơi, khách đến nhận sân -> Nhân viên Check-in vào sân
        await bookingService.CheckInAsync(createResult.Id);

        var step4Booking = await bookingService.GetBookingByIdAsync(createResult.Id);
        step4Booking.Status.Should().Be(BookingStatus.CheckedIn);

        // BƯỚC 5: Khách chơi xong, nhân viên thu nốt số tiền còn lại (Final Payment)
        decimal remainingDue = step4Booking.FinalAmount - step4Booking.PaidAmount;
        var finalPaymentResult = await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = remainingDue,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final,
            Note = "Thu đủ tiền mặt khi trả sân"
        });

        // BR-13: Thu đủ tiền, booking tự động chuyển sang Completed
        finalPaymentResult.IsCompleted.Should().BeTrue();
        finalPaymentResult.NewStatus.Should().Be(BookingStatus.Completed);
        finalPaymentResult.RemainingAmount.Should().Be(0);
        finalPaymentResult.PointsEarned.Should().BeGreaterThan(0);

        var step5Booking = await bookingService.GetBookingByIdAsync(createResult.Id);
        step5Booking.Status.Should().Be(BookingStatus.Completed);

        // BƯỚC 6: In và xem trước hóa đơn -> Kiểm tra toàn bộ dữ liệu & các con số chính xác 100%
        var invoice = await invoiceService.GetInvoiceDataAsync(createResult.Id, "Lê Thu Staff");

        invoice.Should().NotBeNull();
        invoice.BookingCode.Should().Be(createResult.Code);
        invoice.CourtName.Should().Be("Sân 2");
        invoice.CustomerName.Should().Be("Nguyễn Văn An");
        invoice.CustomerTierName.Should().Be("Vàng");
        invoice.StatusText.Should().Be("Hoàn thành");
        invoice.PaidAmount.Should().Be(step4Booking.FinalAmount);
        invoice.RemainingAmount.Should().Be(0);
        invoice.Payments.Should().HaveCount(2); // 1 cọc + 1 thanh toán còn lại

        // Sinh FlowDocument in ấn và đối chiếu text hiển thị
        var doc = invoicePrinter.BuildDocument(invoice);
        doc.Should().NotBeNull();

        var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
        string invoiceText = textRange.Text;

        invoiceText.Should().Contain("PHIẾU THANH TOÁN TIỀN SÂN");
        invoiceText.Should().Contain(createResult.Code);
        invoiceText.Should().Contain("Nguyễn Văn An");
        invoiceText.Should().Contain("Sân 2");
        invoiceText.Should().Contain($"{invoice.SubTotal:N0} đ");
        invoiceText.Should().Contain($"{invoice.FinalAmount:N0} đ");
        invoiceText.Should().Contain($"{invoice.PaidAmount:N0} đ");
        invoiceText.Should().Contain("Lê Thu Staff");
        invoiceText.Should().Contain("Cảm ơn Quý khách");
    }
}
