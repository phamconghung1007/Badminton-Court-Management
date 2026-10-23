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
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessException("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
        }

        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.Branch)
            .Include(u => u.Customer)
                .ThenInclude(c => c!.MembershipTier)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        // Section 8.5: Kiểm tra IsActive, BCrypt.Verify, báo lỗi chung không tiết lộ lý do
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

        string? branchName = user.Branch?.Name;
        string? tierName = user.Customer?.MembershipTier?.Name;

        _currentUserContext.SetUser(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            branchName,
            user.Customer?.Id,
            tierName
        );

        return new LoginResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            branchName,
            user.Customer?.Id,
            tierName
        );
    }

    public async Task<CustomerDto> RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new BusinessException("Tên đăng nhập không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            throw new BusinessException("Mật khẩu phải có ít nhất 6 ký tự.");
        }
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new BusinessException("Họ và tên không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            throw new BusinessException("Số điện thoại không được để trống.");
        }

        await using var db = await _factory.CreateDbContextAsync();

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new BusinessException("Tên đăng nhập đã tồn tại trong hệ thống.");
        }

        var existingCustomer = await db.Customers
            .Include(c => c.MembershipTier)
            .FirstOrDefaultAsync(c => c.Phone == request.Phone);

        if (existingCustomer != null && existingCustomer.UserId.HasValue)
        {
            throw new BusinessException("Số điện thoại này đã được liên kết với một tài khoản.");
        }

        var defaultTier = await db.MembershipTiers
            .OrderBy(t => t.SortOrder)
            .FirstOrDefaultAsync();

        if (defaultTier == null)
        {
            throw new BusinessException("Hệ thống chưa cấu hình hạng thành viên mặc định.");
        }

        var user = new AppUser
        {
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRole.Customer,
            BranchId = null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Customer customer;
        // Section 4.1: Nếu đã có hồ sơ khách do Staff tạo tại quầy (chưa có UserId) thì liên kết vào hồ sơ cũ
        if (existingCustomer != null)
        {
            existingCustomer.UserId = user.Id;
            existingCustomer.FullName = request.FullName.Trim();
            customer = existingCustomer;
        }
        else
        {
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Phone = request.Phone.Trim(),
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
            TierName = customer.MembershipTier?.Name ?? defaultTier.Name,
            DiscountPercent = customer.MembershipTier?.DiscountPercent ?? defaultTier.DiscountPercent,
            TotalPoints = customer.TotalPoints,
            MembershipCardNo = customer.MembershipCardNo,
            CreatedAt = customer.CreatedAt,
            IsActive = customer.IsActive
        };
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new BusinessException("Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

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

    public async Task<UserProfileDto> GetCurrentUserProfileAsync()
    {
        if (!_currentUserContext.Id.HasValue)
        {
            throw new ForbiddenException("Chưa đăng nhập.");
        }

        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Branch)
            .Include(u => u.Customer)
                .ThenInclude(c => c!.MembershipTier)
            .FirstOrDefaultAsync(u => u.Id == _currentUserContext.Id.Value);

        if (user == null)
        {
            throw new BusinessException("Không tìm thấy thông tin tài khoản.");
        }

        return new UserProfileDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            user.Branch?.Name,
            user.Customer?.Id,
            user.Customer?.Phone,
            user.Customer?.MembershipTier?.Name,
            user.Customer?.TotalPoints ?? 0,
            user.CreatedAt,
            user.LastLoginAt
        );
    }
}
