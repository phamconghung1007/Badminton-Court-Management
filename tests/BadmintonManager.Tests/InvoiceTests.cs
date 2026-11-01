using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class InvoiceTests
{
    private async Task<(IDbContextFactory<AppDbContext> factory, CurrentUserContext userContext, InvoiceService invoiceService, BookingService bookingService, PaymentService paymentService)> CreateTestSetupAsync()
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
        userContext.SetUser(3, "staff1", "Nhân viên Cầu Giấy", UserRole.Staff, 1, "Cơ sở Cầu Giấy");

        var membershipService = new MembershipService(factory, userContext);
        var paymentService = new PaymentService(factory, userContext, membershipService);
        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, userContext);
        var bookingService = new BookingService(factory, userContext, pricingService, settingService);
        var invoiceService = new InvoiceService(factory, userContext);

        return (factory, userContext, invoiceService, bookingService, paymentService);
    }

    [Fact]
    public async Task InvoiceService_GetInvoiceDataAsync_ExtractsAllFieldsAccurately()
    {
        // Arrange: Tạo đơn đặt sân cho khách quen customer 1 (Nguyễn Văn An, Vàng - giảm 10%)
        var (factory, userContext, invoiceService, bookingService, paymentService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 1,
            CustomerId = 1, // Khách hàng Vàng
            StartTime = targetDay.AddHours(8),
            EndTime = targetDay.AddHours(10) // 2 tiếng (08:00 - 10:00)
        });

        // Thu trước tiền cọc (30%)
        await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = createResult.DepositAmount,
            Method = PaymentMethod.BankTransfer,
            Type = PaymentType.Deposit,
            ReferenceNo = "VCB-987654"
        });

        // Act: Lấy dữ liệu hóa đơn
        var invoice = await invoiceService.GetInvoiceDataAsync(createResult.Id, "Nguyễn Thu Ngân");

        // Assert
        invoice.Should().NotBeNull();
        invoice.BookingId.Should().Be(createResult.Id);
        invoice.BookingCode.Should().Be(createResult.Code);
        invoice.BranchName.Should().Contain("Cầu Giấy");
        invoice.CourtName.Should().Be("Sân 1");
        invoice.CustomerName.Should().Be("Nguyễn Văn An");
        invoice.CustomerTierName.Should().Be("Vàng");
        invoice.DurationMinutes.Should().Be(120);

        invoice.SubTotal.Should().Be(createResult.SubTotal);
        invoice.DiscountAmount.Should().Be(createResult.DiscountAmount);
        invoice.FinalAmount.Should().Be(createResult.FinalAmount);
        invoice.DepositAmount.Should().Be(createResult.DepositAmount);
        invoice.PaidAmount.Should().Be(createResult.DepositAmount);
        invoice.RemainingAmount.Should().Be(createResult.FinalAmount - createResult.DepositAmount);
        invoice.CashierName.Should().Be("Nguyễn Thu Ngân");
        invoice.PaymentMethod.Should().Be("Chuyển khoản ngân hàng");
        invoice.ReferenceNo.Should().Be("VCB-987654");
    }

    [Fact]
    public async Task InvoicePrinter_BuildDocument_RendersAllRequiredBlocksAndNumbers()
    {
        // Arrange
        var invoiceDto = new InvoiceDto
        {
            BookingId = 101,
            BookingCode = "BK20261102-001",
            BranchName = "Cơ sở Cầu Giấy",
            BranchAddress = "123 Đường Cầu Giấy, Hà Nội",
            BranchPhone = "02431234567",
            CourtName = "Sân 3",
            StartTime = new DateTime(2026, 11, 2, 8, 0, 0),
            EndTime = new DateTime(2026, 11, 2, 10, 0, 0),
            DurationMinutes = 120,
            CustomerName = "Trần Thị Khách",
            CustomerPhone = "0988776655",
            CustomerTierName = "Bạc",
            SubTotal = 120000m,
            DiscountPercent = 5m,
            DiscountAmount = 6000m,
            FinalAmount = 114000m,
            DepositAmount = 35000m,
            PaidAmount = 35000m,
            PaymentMethod = "Tiền mặt",
            CashierName = "Lê Thu Staff",
            PrintedAt = new DateTime(2026, 11, 2, 7, 55, 0),
            StatusText = "Đã duyệt"
        };

        var printer = new InvoicePrinter();

        // Act: Sinh FlowDocument
        FlowDocument document = printer.BuildDocument(invoiceDto);

        // Assert
        document.Should().NotBeNull();
        document.Blocks.Should().NotBeEmpty();

        // Trích xuất toàn bộ text trong FlowDocument để kiểm tra nội dung
        var textRange = new TextRange(document.ContentStart, document.ContentEnd);
        string plainText = textRange.Text;

        plainText.Should().Contain("PHIẾU THANH TOÁN TIỀN SÂN");
        plainText.Should().Contain("Cơ sở Cầu Giấy".ToUpper());
        plainText.Should().Contain("BK20261102-001");
        plainText.Should().Contain("Trần Thị Khách");
        plainText.Should().Contain("Sân 3");
        plainText.Should().Contain($"{invoiceDto.SubTotal:N0} đ"); // Tiền gốc
        plainText.Should().Contain($"{invoiceDto.FinalAmount:N0} đ"); // Tổng tiền
        plainText.Should().Contain($"{invoiceDto.PaidAmount:N0} đ");  // Đã thanh toán
        plainText.Should().Contain($"{invoiceDto.RemainingAmount:N0} đ");  // Còn lại
        plainText.Should().Contain("Lê Thu Staff");
        plainText.Should().Contain("Cảm ơn Quý khách");
    }

    [Fact]
    public async Task InvoiceService_ForCompletedBooking_ShowsZeroRemainingAmount()
    {
        // Arrange
        var (factory, userContext, invoiceService, bookingService, paymentService) = await CreateTestSetupAsync();
        DateTime targetDay = DateTime.Today.AddDays(1);

        var createResult = await bookingService.CreateBookingAsync(new CreateBookingRequest
        {
            CourtId = 2,
            StartTime = targetDay.AddHours(14),
            EndTime = targetDay.AddHours(15), // 1 tiếng = 60.000đ
            GuestName = "Khách Hoàn Tất",
            GuestPhone = "0944332211"
        });

        // Thanh toán 100% tiền sân
        await paymentService.ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = createResult.Id,
            Amount = createResult.FinalAmount,
            Method = PaymentMethod.Cash,
            Type = PaymentType.Final
        });

        // Act
        var invoice = await invoiceService.GetInvoiceDataAsync(createResult.Id);

        // Assert
        invoice.StatusText.Should().Be("Hoàn thành");
        invoice.PaidAmount.Should().Be(createResult.FinalAmount);
        invoice.RemainingAmount.Should().Be(0);
    }

    [Fact]
    public async Task InvoiceService_ThrowsBusinessException_WhenBookingNotFound()
    {
        // Arrange
        var (factory, userContext, invoiceService, _, _) = await CreateTestSetupAsync();

        // Act
        var act = async () => await invoiceService.GetInvoiceDataAsync(999999);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không tìm thấy đơn đặt sân*");
    }
}
