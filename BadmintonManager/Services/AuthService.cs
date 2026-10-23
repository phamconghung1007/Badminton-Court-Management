using System;
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

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUserContext;

    public AuthService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUserContext)
    {
        _factory = factory;
        _currentUserContext = currentUserContext;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || !user.IsActive)
        {
            throw new BusinessException("Sai tên đăng nhập hoặc mật khẩu.");
        }

        bool verified = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!verified)
        {
            throw new BusinessException("Sai tên đăng nhập hoặc mật khẩu.");
        }

        user.LastLoginAt = DateTime.Now;
        await db.SaveChangesAsync();

        _currentUserContext.SetUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            user.Customer?.Id
        );

        return new LoginResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            user.Customer?.Id
        );
    }

    public async Task<CustomerDto> RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new BusinessException("Tên đăng nhập đã tồn tại trong hệ thống.");
        }

        var existingCustomer = await db.Customers.FirstOrDefaultAsync(c => c.Phone == request.Phone);
        if (existingCustomer != null && existingCustomer.UserId.HasValue)
        {
            throw new BusinessException("Số điện thoại này đã được đăng ký tài khoản.");
        }

        var defaultTier = await db.MembershipTiers
            .OrderBy(t => t.SortOrder)
            .FirstOrDefaultAsync();

        if (defaultTier == null)
        {
            throw new BusinessException("Hệ thống chưa cấu hình hạng thành viên.");
        }

        var user = new AppUser
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = UserRole.Customer,
            BranchId = null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Customer customer;
        if (existingCustomer != null)
        {
            existingCustomer.UserId = user.Id;
            existingCustomer.FullName = request.FullName;
            customer = existingCustomer;
        }
        else
        {
            customer = new Customer
            {
                FullName = request.FullName,
                Phone = request.Phone,
                UserId = user.Id,
                MembershipTierId = defaultTier.Id,
                TotalPoints = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            db.Customers.Add(customer);
        }

        await db.SaveChangesAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Phone = customer.Phone,
            UserId = user.Id,
            Username = user.Username,
            MembershipTierId = customer.MembershipTierId,
            TierName = defaultTier.Name,
            DiscountPercent = defaultTier.DiscountPercent,
            TotalPoints = customer.TotalPoints,
            MembershipCardNo = customer.MembershipCardNo,
            CreatedAt = customer.CreatedAt,
            IsActive = customer.IsActive
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BusinessException("Không tìm thấy người dùng.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessException("Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();
    }
}
