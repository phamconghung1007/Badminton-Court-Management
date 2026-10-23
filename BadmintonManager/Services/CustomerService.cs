using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class CustomerService : ICustomerService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public CustomerService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<List<CustomerDto>> SearchCustomersAsync(string? keyword)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Customers
            .AsNoTracking()
            .Include(c => c.MembershipTier)
            .Include(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(c => c.FullName.Contains(keyword) || c.Phone.Contains(keyword) || (c.MembershipCardNo != null && c.MembershipCardNo.Contains(keyword)));
        }

        return await query.Select(c => new CustomerDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Phone = c.Phone,
            UserId = c.UserId,
            Username = c.User != null ? c.User.Username : null,
            MembershipTierId = c.MembershipTierId,
            TierName = c.MembershipTier.Name,
            DiscountPercent = c.MembershipTier.DiscountPercent,
            TotalPoints = c.TotalPoints,
            MembershipCardNo = c.MembershipCardNo,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        }).ToListAsync();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var c = await db.Customers
            .AsNoTracking()
            .Include(c => c.MembershipTier)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return null;

        return new CustomerDto
        {
            Id = c.Id,
            FullName = c.FullName,
            Phone = c.Phone,
            UserId = c.UserId,
            Username = c.User?.Username,
            MembershipTierId = c.MembershipTierId,
            TierName = c.MembershipTier.Name,
            DiscountPercent = c.MembershipTier.DiscountPercent,
            TotalPoints = c.TotalPoints,
            MembershipCardNo = c.MembershipCardNo,
            CreatedAt = c.CreatedAt,
            IsActive = c.IsActive
        };
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerCreateDto request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Customers.AnyAsync(c => c.Phone == request.Phone))
        {
            throw new BusinessException($"Số điện thoại '{request.Phone}' đã tồn tại trong danh sách khách hàng.");
        }

        var defaultTier = await db.MembershipTiers.OrderBy(t => t.SortOrder).FirstOrDefaultAsync();
        if (defaultTier == null) throw new BusinessException("Hệ thống chưa có cấu hình hạng thành viên.");

        var customer = new Customer
        {
            FullName = request.FullName,
            Phone = request.Phone,
            MembershipCardNo = request.MembershipCardNo,
            MembershipTierId = defaultTier.Id,
            TotalPoints = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone,
            MembershipTierId = defaultTier.Id,
            TierName = defaultTier.Name,
            DiscountPercent = defaultTier.DiscountPercent,
            TotalPoints = 0,
            MembershipCardNo = customer.MembershipCardNo,
            CreatedAt = customer.CreatedAt,
            IsActive = true
        };
    }

    public async Task<CustomerDto> UpdateCustomerAsync(CustomerUpdateDto request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.Include(c => c.MembershipTier).FirstOrDefaultAsync(c => c.Id == request.Id);
        if (customer == null) throw new BusinessException("Không tìm thấy khách hàng.");

        bool phoneTaken = await db.Customers.AnyAsync(c => c.Phone == request.Phone && c.Id != request.Id);
        if (phoneTaken) throw new BusinessException($"Số điện thoại '{request.Phone}' đã được sử dụng bởi khách hàng khác.");

        customer.FullName = request.FullName;
        customer.Phone = request.Phone;
        customer.MembershipCardNo = request.MembershipCardNo;
        customer.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone,
            UserId = customer.UserId,
            MembershipTierId = customer.MembershipTierId,
            TierName = customer.MembershipTier.Name,
            DiscountPercent = customer.MembershipTier.DiscountPercent,
            TotalPoints = customer.TotalPoints,
            MembershipCardNo = customer.MembershipCardNo,
            CreatedAt = customer.CreatedAt,
            IsActive = customer.IsActive
        };
    }
}
