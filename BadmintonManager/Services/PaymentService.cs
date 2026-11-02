using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BadmintonManager.Services;

public class PaymentService : IPaymentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IMembershipService _membershipService;

    public PaymentService(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IMembershipService membershipService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _membershipService = membershipService;
    }

    private static async Task<IDbContextTransaction> BeginTransactionSafelyAsync(AppDbContext db)
    {
        if (!db.Database.IsRelational())
        {
            return new NoOpTransaction();
        }
        return await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    }

    private class NoOpTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new BusinessException("Số tiền thanh toán phải lớn hơn 0 VNĐ.");
        }

        if (_currentUser.IsCustomer)
        {
            throw new ForbiddenException("Khách hàng không có quyền thực hiện giao dịch thu tiền.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await BeginTransactionSafelyAsync(db);

        var booking = await db.Bookings
            .Include(b => b.Payments)
            .Include(b => b.Court)
            .Include(b => b.Customer)
                .ThenInclude(c => c!.MembershipTier)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null)
        {
            throw new BusinessException("Không tìm thấy đơn đặt sân.");
        }

        // Phân quyền chi nhánh
        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue && booking.Court.BranchId != _currentUser.BranchId.Value)
            {
                throw new ForbiddenException("Bạn chỉ có quyền thu tiền đơn đặt sân thuộc cơ sở của mình.");
            }
        }

        if (booking.Status == BookingStatus.Cancelled && request.Type != PaymentType.Refund)
        {
            throw new BusinessException("Đơn đặt sân đã bị hủy, không thể thu thêm tiền.");
        }

        int userId = _currentUser.Id ?? 1;

        var payment = new Payment
        {
            BookingId = request.BookingId,
            Amount = request.Amount,
            Method = request.Method,
            Type = request.Type,
            ReferenceNo = request.ReferenceNo,
            PaidAt = DateTime.Now,
            ReceivedByUserId = userId,
            Note = request.Note
        };

        db.Payments.Add(payment);

        // Tính toán tổng số tiền đã thanh toán (EF Core tự động đồng bộ payment mới vào booking.Payments)
        decimal totalPaid = booking.Payments.Where(p => p.Type != PaymentType.Refund).Sum(p => p.Amount);
        decimal totalRefund = booking.Payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);
        decimal netPaid = totalPaid - totalRefund;
        decimal remaining = Math.Max(0, booking.FinalAmount - netPaid);

        bool justCompleted = false;
        int pointsEarned = 0;
        bool isTierUpgraded = false;
        string? newTierName = null;

        // Nếu là thanh toán tiền cọc cho đơn Pending: tự động xác nhận thành Confirmed (Section 11 Quyết định 2)
        if (request.Type == PaymentType.Deposit && booking.Status == BookingStatus.Pending)
        {
            booking.Status = BookingStatus.Confirmed;
            booking.ConfirmedByUserId = userId;
        }

        // BR-13: Booking chuyển Completed khi tổng đã thu (trừ hoàn) >= FinalAmount
        if (netPaid >= booking.FinalAmount && booking.Status != BookingStatus.Completed && booking.Status != BookingStatus.Cancelled)
        {
            booking.Status = BookingStatus.Completed;
            justCompleted = true;

            // BR-15, BR-16: Hoàn tất và tích điểm trong cùng transaction
            if (booking.CustomerId.HasValue && booking.Customer != null)
            {
                pointsEarned = (int)Math.Floor(booking.FinalAmount / 10000m);
                if (pointsEarned > 0)
                {
                    booking.Customer.TotalPoints += pointsEarned;

                    var pointTx = new PointTransaction
                    {
                        CustomerId = booking.CustomerId.Value,
                        BookingId = booking.Id,
                        Points = pointsEarned,
                        Reason = $"Tích điểm đơn đặt sân #{booking.Code} (Thanh toán {booking.FinalAmount:N0}đ)",
                        CreatedAt = DateTime.Now
                    };
                    db.PointTransactions.Add(pointTx);

                    // Kiểm tra nâng hạng (BR-16: Đạt ngưỡng điểm thì nâng hạng, không tự hạ)
                    var allTiers = await db.MembershipTiers.OrderByDescending(t => t.MinPoints).ToListAsync();
                    var eligibleTier = allTiers.FirstOrDefault(t => booking.Customer.TotalPoints >= t.MinPoints);
                    if (eligibleTier != null && eligibleTier.MinPoints > (booking.Customer.MembershipTier?.MinPoints ?? 0))
                    {
                        booking.Customer.MembershipTierId = eligibleTier.Id;
                        isTierUpgraded = true;
                        newTierName = eligibleTier.Name;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        string msg = justCompleted
            ? $"Thanh toán thành công! Đơn đặt sân #{booking.Code} đã hoàn tất."
            : $"Đã ghi nhận thanh toán {request.Amount:N0}đ thành công cho đơn #{booking.Code}.";

        if (isTierUpgraded)
        {
            msg += $" Chúc mừng khách hàng đã được nâng lên hạng {newTierName}!";
        }

        return new PaymentResultDto
        {
            PaymentId = payment.Id,
            BookingId = booking.Id,
            AmountPaid = request.Amount,
            NetPaid = netPaid,
            RemainingAmount = remaining,
            IsCompleted = booking.Status == BookingStatus.Completed,
            NewStatus = booking.Status,
            PointsEarned = pointsEarned,
            IsTierUpgraded = isTierUpgraded,
            NewTierName = newTierName,
            Message = msg
        };
    }

    public async Task<Payment> ProcessPaymentAsync(
        int bookingId,
        decimal amount,
        PaymentMethod method,
        PaymentType type,
        string? referenceNo,
        string? note)
    {
        var result = await ProcessPaymentAsync(new ProcessPaymentRequest
        {
            BookingId = bookingId,
            Amount = amount,
            Method = method,
            Type = type,
            ReferenceNo = referenceNo,
            Note = note
        });

        await using var db = await _factory.CreateDbContextAsync();
        return await db.Payments.FirstAsync(p => p.Id == result.PaymentId);
    }

    public async Task<PaymentSummaryDto> GetPaymentSummaryAsync(int bookingId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Payments)
                .ThenInclude(p => p.ReceivedByUser)
            .Include(b => b.Court)
                .ThenInclude(c => c.Branch)
            .Include(b => b.Customer)
                .ThenInclude(c => c!.MembershipTier)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new BusinessException("Không tìm thấy đơn đặt sân.");
        }

        decimal totalPaid = booking.Payments.Where(p => p.Type != PaymentType.Refund).Sum(p => p.Amount);
        decimal totalRefunded = booking.Payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount);

        var paymentDtos = booking.Payments.Select(p => new PaymentItemDto
        {
            Id = p.Id,
            BookingId = p.BookingId,
            Amount = p.Amount,
            Method = p.Method,
            Type = p.Type,
            ReferenceNo = p.ReferenceNo,
            PaidAt = p.PaidAt,
            ReceivedByUserName = p.ReceivedByUser?.FullName ?? p.ReceivedByUser?.Username ?? "Hệ thống",
            Note = p.Note
        }).OrderBy(p => p.PaidAt).ToList();

        return new PaymentSummaryDto
        {
            BookingId = booking.Id,
            BookingCode = booking.Code,
            BranchName = booking.Court.Branch.Name,
            CourtName = booking.Court.Name,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            CustomerName = booking.Customer?.FullName ?? booking.GuestName ?? "Khách vãng lai",
            CustomerPhone = booking.Customer?.Phone ?? booking.GuestPhone,
            CustomerTierName = booking.Customer?.MembershipTier?.Name,
            Status = booking.Status,
            SubTotal = booking.SubTotal,
            DiscountPercent = booking.DiscountPercent,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,
            DepositAmount = booking.DepositAmount,
            TotalPaid = totalPaid,
            TotalRefunded = totalRefunded,
            Payments = paymentDtos
        };
    }

    public async Task<List<PaymentItemDto>> GetPaymentItemDtosByBookingIdAsync(int bookingId)
    {
        var summary = await GetPaymentSummaryAsync(bookingId);
        return summary.Payments;
    }

    public async Task<List<Payment>> GetPaymentsByBookingIdAsync(int bookingId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Payments
            .AsNoTracking()
            .Include(p => p.ReceivedByUser)
            .Where(p => p.BookingId == bookingId)
            .OrderBy(p => p.PaidAt)
            .ToListAsync();
    }
}
