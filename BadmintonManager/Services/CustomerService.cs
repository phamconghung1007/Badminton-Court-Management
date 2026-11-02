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
            query = query.Where(c => c.FullName.Contains(keyword) 
                || c.Phone.Contains(keyword) 
                || (c.MembershipCardNo != null && c.MembershipCardNo.Contains(keyword))
                || (c.User != null && c.User.Username.Contains(keyword)));
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
        }).OrderByDescending(c => c.TotalPoints).ToListAsync();
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

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var c = await db.Customers
            .AsNoTracking()
            .Include(c => c.MembershipTier)
            .Include(c => c.User)
            .Include(c => c.Bookings)
                .ThenInclude(b => b.Court)
                    .ThenInclude(crt => crt.Branch)
            .Include(c => c.PointTransactions)
            .Include(c => c.FixedSchedules)
                .ThenInclude(fs => fs.Court)
                    .ThenInclude(crt => crt.Branch)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return null;

        var customerDto = new CustomerDto
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

        var completedBookings = c.Bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
        var cancelledBookings = c.Bookings.Where(b => b.Status == BookingStatus.Cancelled).ToList();
        decimal totalSpent = completedBookings.Sum(b => b.FinalAmount);

        var pointHistory = c.PointTransactions
            .OrderByDescending(pt => pt.CreatedAt)
            .Take(15)
            .Select(pt => new CustomerPointHistoryDto(
                pt.Id,
                pt.Points,
                0, // Running balance if needed
                pt.Reason,
                pt.CreatedAt,
                pt.Booking != null ? pt.Booking.Code : null
            )).ToList();

        var recentBookings = c.Bookings
            .OrderByDescending(b => b.StartTime)
            .Take(10)
            .Select(b => new BookingItemDto
            {
                Id = b.Id,
                Code = b.Code,
                CourtId = b.CourtId,
                CourtName = b.Court.Name,
                BranchId = b.Court.BranchId,
                BranchName = b.Court.Branch.Name,
                CustomerId = b.CustomerId,
                CustomerName = c.FullName,
                CustomerPhone = c.Phone,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Status = b.Status,
                SubTotal = b.SubTotal,
                DiscountPercent = b.DiscountPercent,
                DiscountAmount = b.DiscountAmount,
                FinalAmount = b.FinalAmount,
                DepositAmount = b.DepositAmount,
                PaidAmount = b.Payments.Sum(p => p.Amount),
                BookingType = b.BookingType,
                CreatedAt = b.CreatedAt
            }).ToList();

        var activeSchedules = c.FixedSchedules
            .Where(fs => fs.IsActive)
            .Select(fs => new FixedScheduleDto
            {
                Id = fs.Id,
                CustomerId = fs.CustomerId,
                CustomerName = c.FullName,
                CustomerPhone = c.Phone,
                CourtId = fs.CourtId,
                CourtName = fs.Court.Name,
                BranchId = fs.Court.BranchId,
                BranchName = fs.Court.Branch.Name,
                DayOfWeek = fs.DayOfWeek,
                StartMinute = fs.StartMinute,
                EndMinute = fs.EndMinute,
                ValidFrom = fs.ValidFrom,
                ValidTo = fs.ValidTo,
                IsActive = fs.IsActive,
                CreatedAt = fs.CreatedAt,
                TotalBookingsCount = fs.Bookings.Count,
                FutureBookingsCount = fs.Bookings.Count(b => b.StartTime > DateTime.Now && b.Status != BookingStatus.Cancelled)
            }).ToList();

        return new CustomerDetailDto
        {
            Customer = customerDto,
            TotalBookings = c.Bookings.Count,
            CompletedBookings = completedBookings.Count,
            CancelledBookings = cancelledBookings.Count,
            TotalSpent = totalSpent,
            PointHistory = pointHistory,
            RecentBookings = recentBookings,
            ActiveFixedSchedules = activeSchedules
        };
    }

    public async Task<CustomerDto> CreateCustomerAsync(CustomerCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BusinessException("Họ và tên khách hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new BusinessException("Số điện thoại khách hàng không được để trống.");

        string phone = request.Phone.Trim();

        await using var db = await _factory.CreateDbContextAsync();

        // BR-20: Phone của khách duy nhất
        if (await db.Customers.AnyAsync(c => c.Phone == phone))
        {
            throw new BusinessException($"Số điện thoại '{phone}' đã tồn tại trong danh sách khách hàng.");
        }

        var defaultTier = await db.MembershipTiers.OrderBy(t => t.SortOrder).FirstOrDefaultAsync();
        if (defaultTier == null) throw new BusinessException("Hệ thống chưa có cấu hình hạng thành viên.");

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Phone = phone,
            MembershipCardNo = string.IsNullOrWhiteSpace(request.MembershipCardNo) ? null : request.MembershipCardNo.Trim(),
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
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BusinessException("Họ và tên khách hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new BusinessException("Số điện thoại khách hàng không được để trống.");

        string phone = request.Phone.Trim();

        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.Include(c => c.MembershipTier).FirstOrDefaultAsync(c => c.Id == request.Id);
        if (customer == null) throw new BusinessException("Không tìm thấy khách hàng.");

        // BR-20: Phone duy nhất
        bool phoneTaken = await db.Customers.AnyAsync(c => c.Phone == phone && c.Id != request.Id);
        if (phoneTaken) throw new BusinessException($"Số điện thoại '{phone}' đã được sử dụng bởi khách hàng khác.");

        customer.FullName = request.FullName.Trim();
        customer.Phone = phone;
        customer.MembershipCardNo = string.IsNullOrWhiteSpace(request.MembershipCardNo) ? null : request.MembershipCardNo.Trim();
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

    public async Task<bool> ToggleCustomerStatusAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.FindAsync(id);
        if (customer == null) throw new BusinessException("Không tìm thấy khách hàng.");

        customer.IsActive = !customer.IsActive;
        await db.SaveChangesAsync();
        return customer.IsActive;
    }

    public async Task DeleteCustomerAsync(int id)
    {
        // BR-21: Không xóa cứng dữ liệu đã có booking (cơ sở, sân, khách): dùng IsActive / xóa mềm
        await using var db = await _factory.CreateDbContextAsync();
        var customer = await db.Customers.FindAsync(id);
        if (customer == null) throw new BusinessException("Không tìm thấy khách hàng.");

        customer.IsActive = false;
        await db.SaveChangesAsync();
    }
}
