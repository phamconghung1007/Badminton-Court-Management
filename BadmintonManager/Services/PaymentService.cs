using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Payment> ProcessPaymentAsync(
        int bookingId,
        decimal amount,
        PaymentMethod method,
        PaymentType type,
        string? referenceNo,
        string? note)
    {
        if (amount <= 0)
        {
            throw new BusinessException("Số tiền thanh toán phải lớn hơn 0.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new BusinessException("Không tìm thấy đơn đặt sân.");
        }

        int userId = _currentUser.Id ?? 1; // Default fallback to system/seed user

        var payment = new Payment
        {
            BookingId = bookingId,
            Amount = amount,
            Method = method,
            Type = type,
            ReferenceNo = referenceNo,
            PaidAt = DateTime.Now,
            ReceivedByUserId = userId,
            Note = note
        };

        db.Payments.Add(payment);

        // Calculate total paid
        decimal totalPaid = booking.Payments
            .Where(p => p.Type != PaymentType.Refund)
            .Sum(p => p.Amount);

        decimal totalRefund = booking.Payments
            .Where(p => p.Type == PaymentType.Refund)
            .Sum(p => p.Amount);

        if (type == PaymentType.Refund)
        {
            totalRefund += amount;
        }
        else
        {
            totalPaid += amount;
        }

        decimal netPaid = totalPaid - totalRefund;

        // If paid >= FinalAmount, mark as Completed (BR-13)
        bool justCompleted = false;
        if (netPaid >= booking.FinalAmount && booking.Status != BookingStatus.Completed && booking.Status != BookingStatus.Cancelled)
        {
            booking.Status = BookingStatus.Completed;
            justCompleted = true;
        }

        await db.SaveChangesAsync();

        if (justCompleted && booking.CustomerId.HasValue)
        {
            await _membershipService.AddPointsAsync(booking.CustomerId.Value, booking.Id, booking.FinalAmount);
        }

        return payment;
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
