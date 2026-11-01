using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public InvoiceService(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> GetInvoiceDataAsync(int bookingId, string? cashierName = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
                .ThenInclude(c => c.Branch)
            .Include(b => b.Customer)
                .ThenInclude(c => c!.MembershipTier)
            .Include(b => b.Payments)
                .ThenInclude(p => p.ReceivedByUser)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new BusinessException("Không tìm thấy đơn đặt sân để lập hóa đơn.");
        }

        // Tính toán các khoản thanh toán
        decimal totalPaid = booking.Payments.Where(p => p.Type != PaymentType.Refund).Sum(p => p.Amount);
        decimal totalRefund = booking.Payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);
        decimal netPaid = totalPaid - totalRefund;

        // Xác định phương thức thanh toán chính từ giao dịch gần nhất
        var latestPayment = booking.Payments.OrderByDescending(p => p.PaidAt).FirstOrDefault();
        string paymentMethod = latestPayment != null
            ? (latestPayment.Method == PaymentMethod.BankTransfer ? "Chuyển khoản ngân hàng" : "Tiền mặt")
            : "Chưa thanh toán";

        string effectiveCashier = !string.IsNullOrWhiteSpace(cashierName)
            ? cashierName
            : (latestPayment?.ReceivedByUser?.FullName
               ?? _currentUser.FullName
               ?? "Lễ tân ca trực");

        var paymentList = booking.Payments.Select(p => new PaymentItemDto
        {
            Id = p.Id,
            BookingId = p.BookingId,
            Amount = p.Amount,
            Method = p.Method,
            Type = p.Type,
            ReferenceNo = p.ReferenceNo,
            PaidAt = p.PaidAt,
            ReceivedByUserName = p.ReceivedByUser?.FullName ?? "Hệ thống",
            Note = p.Note
        }).OrderBy(p => p.PaidAt).ToList();

        string statusName = booking.Status switch
        {
            BookingStatus.Pending => "Chờ duyệt",
            BookingStatus.Confirmed => "Đã duyệt",
            BookingStatus.CheckedIn => "Đang chơi",
            BookingStatus.Completed => "Hoàn thành",
            BookingStatus.Cancelled => "Đã hủy",
            BookingStatus.NoShow => "Vắng mặt",
            _ => booking.Status.ToString()
        };

        return new InvoiceDto
        {
            BookingId = booking.Id,
            BookingCode = booking.Code,
            BranchName = booking.Court.Branch.Name,
            BranchAddress = booking.Court.Branch.Address,
            BranchPhone = booking.Court.Branch.Phone,
            CourtName = booking.Court.Name,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            DurationMinutes = (int)(booking.EndTime - booking.StartTime).TotalMinutes,
            CustomerName = booking.Customer?.FullName ?? booking.GuestName ?? "Khách vãng lai",
            CustomerPhone = booking.Customer?.Phone ?? booking.GuestPhone ?? "---",
            CustomerTierName = booking.Customer?.MembershipTier?.Name,
            SubTotal = booking.SubTotal,
            DiscountPercent = booking.DiscountPercent,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,
            DepositAmount = booking.DepositAmount,
            PaidAmount = netPaid,
            PaymentMethod = paymentMethod,
            ReferenceNo = latestPayment?.ReferenceNo,
            CashierName = effectiveCashier,
            PrintedAt = DateTime.Now,
            StatusText = statusName,
            Payments = paymentList
        };
    }
}
