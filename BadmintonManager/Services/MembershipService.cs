using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Data;
using BadmintonManager.Models.Entities;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class MembershipService : IMembershipService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MembershipService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<MembershipTier>> GetTiersAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.MembershipTiers
            .AsNoTracking()
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task AddPointsAsync(int customerId, int? bookingId, decimal finalAmount)
    {
        if (finalAmount <= 0) return;

        int earnedPoints = (int)Math.Floor(finalAmount / 10000m);
        if (earnedPoints <= 0) return;

        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.Include(c => c.MembershipTier).FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return;

        customer.TotalPoints += earnedPoints;

        // Check tier upgrade (BR-16: Khi tổng điểm đạt ngưỡng cao hơn thì tự nâng hạng, không tự hạ)
        var tiers = await db.MembershipTiers.OrderByDescending(t => t.MinPoints).ToListAsync();
        var eligibleTier = tiers.FirstOrDefault(t => customer.TotalPoints >= t.MinPoints);
        if (eligibleTier != null && eligibleTier.MinPoints > customer.MembershipTier.MinPoints)
        {
            customer.MembershipTierId = eligibleTier.Id;
        }

        var transaction = new PointTransaction
        {
            CustomerId = customerId,
            BookingId = bookingId,
            Points = earnedPoints,
            Reason = $"Tích điểm đơn đặt sân #{bookingId} (thanh toán {finalAmount:N0}đ)",
            CreatedAt = DateTime.Now
        };

        db.PointTransactions.Add(transaction);
        await db.SaveChangesAsync();
    }

    public async Task DeductPointsAsync(int customerId, int? bookingId, int points, string reason)
    {
        if (points <= 0) return;

        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return;

        customer.TotalPoints = Math.Max(0, customer.TotalPoints - points);

        var transaction = new PointTransaction
        {
            CustomerId = customerId,
            BookingId = bookingId,
            Points = -points,
            Reason = reason,
            CreatedAt = DateTime.Now
        };

        db.PointTransactions.Add(transaction);
        await db.SaveChangesAsync();
    }
}
