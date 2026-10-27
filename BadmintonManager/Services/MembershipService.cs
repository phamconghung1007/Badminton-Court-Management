using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class MembershipService : IMembershipService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext? _currentUser;

    public MembershipService(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext? currentUser = null)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    private void EnsureAdmin()
    {
        if (_currentUser != null && _currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền quản lý hạng hội viên.");
        }
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

    public async Task<List<MembershipTierDto>> GetAllTiersWithStatsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var tiers = await db.MembershipTiers
            .AsNoTracking()
            .Include(t => t.Customers)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.MinPoints)
            .ToListAsync();

        return tiers.Select(t => new MembershipTierDto
        {
            Id = t.Id,
            Name = t.Name,
            MinPoints = t.MinPoints,
            DiscountPercent = t.DiscountPercent,
            SortOrder = t.SortOrder,
            CustomerCount = t.Customers.Count
        }).ToList();
    }

    public async Task<MembershipTierDto?> GetTierByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var tier = await db.MembershipTiers
            .AsNoTracking()
            .Include(t => t.Customers)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tier == null) return null;

        return new MembershipTierDto
        {
            Id = tier.Id,
            Name = tier.Name,
            MinPoints = tier.MinPoints,
            DiscountPercent = tier.DiscountPercent,
            SortOrder = tier.SortOrder,
            CustomerCount = tier.Customers.Count
        };
    }

    public async Task<MembershipTierDto> SaveTierAsync(SaveMembershipTierRequest request)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Tên hạng hội viên không được để trống.");
        }

        if (request.MinPoints < 0)
        {
            throw new BusinessException("Điểm tối thiểu phải lớn hơn hoặc bằng 0.");
        }

        if (request.DiscountPercent < 0 || request.DiscountPercent > 100)
        {
            throw new BusinessException("Mức giảm giá phải từ 0% đến 100%.");
        }

        await using var db = await _factory.CreateDbContextAsync();

        // Check unique tier name
        var duplicate = await db.MembershipTiers
            .AnyAsync(t => t.Name.ToLower() == request.Name.Trim().ToLower() && (!request.Id.HasValue || t.Id != request.Id.Value));

        if (duplicate)
        {
            throw new BusinessException($"Đã tồn tại hạng hội viên mang tên '{request.Name.Trim()}'.");
        }

        MembershipTier entity;
        if (!request.Id.HasValue || request.Id.Value == 0)
        {
            entity = new MembershipTier
            {
                Name = request.Name.Trim(),
                MinPoints = request.MinPoints,
                DiscountPercent = request.DiscountPercent,
                SortOrder = request.SortOrder
            };
            db.MembershipTiers.Add(entity);
        }
        else
        {
            entity = await db.MembershipTiers.FindAsync(request.Id.Value)
                ?? throw new BusinessException($"Không tìm thấy hạng hội viên #{request.Id.Value}");

            entity.Name = request.Name.Trim();
            entity.MinPoints = request.MinPoints;
            entity.DiscountPercent = request.DiscountPercent;
            entity.SortOrder = request.SortOrder;
        }

        await db.SaveChangesAsync();

        return new MembershipTierDto
        {
            Id = entity.Id,
            Name = entity.Name,
            MinPoints = entity.MinPoints,
            DiscountPercent = entity.DiscountPercent,
            SortOrder = entity.SortOrder,
            CustomerCount = await db.Customers.CountAsync(c => c.MembershipTierId == entity.Id)
        };
    }

    public async Task DeleteTierAsync(int id)
    {
        EnsureAdmin();

        await using var db = await _factory.CreateDbContextAsync();
        var tier = await db.MembershipTiers
            .Include(t => t.Customers)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new BusinessException($"Không tìm thấy hạng hội viên #{id}");

        if (tier.Customers.Any())
        {
            throw new BusinessException($"Không thể xoá hạng '{tier.Name}' vì hiện đang có {tier.Customers.Count} khách hàng đang thuộc hạng này.");
        }

        db.MembershipTiers.Remove(tier);
        await db.SaveChangesAsync();
    }
}
