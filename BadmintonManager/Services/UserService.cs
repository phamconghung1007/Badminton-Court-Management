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

public class UserService : IUserService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public UserService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<List<UserItemDto>> GetUserDtosAsync(int? branchId = null, UserRole? role = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking().Include(u => u.Branch).AsQueryable();

        if (_currentUser.IsManager)
        {
            int managerBranchId = _currentUser.BranchId ?? -1;
            query = query.Where(u => u.BranchId == managerBranchId && u.Role == UserRole.Staff);
        }
        else if (_currentUser.IsAdmin)
        {
            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(u => u.BranchId == branchId.Value);
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }
        }
        else
        {
            throw new ForbiddenException("Bạn không có quyền truy cập danh sách tài khoản người dùng.");
        }

        return await query
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .Select(u => new UserItemDto(
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                u.BranchId,
                u.Branch != null ? u.Branch.Name : null,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt
            ))
            .ToListAsync();
    }

    public async Task<List<AppUser>> GetUsersAsync(int? branchId = null, UserRole? role = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking().Include(u => u.Branch).AsQueryable();

        if (_currentUser.IsManager)
        {
            int managerBranchId = _currentUser.BranchId ?? -1;
            query = query.Where(u => u.BranchId == managerBranchId && u.Role == UserRole.Staff);
        }
        else if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        if (branchId.HasValue && _currentUser.IsAdmin)
        {
            query = query.Where(u => u.BranchId == branchId.Value);
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<AppUser?> GetUserByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking().Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return null;

        if (_currentUser.IsManager && (user.BranchId != _currentUser.BranchId || user.Role != UserRole.Staff))
        {
            throw new ForbiddenException("Bạn không có quyền truy cập tài khoản này.");
        }

        return user;
    }

    public async Task<UserItemDto> CreateUserAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new BusinessException("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new BusinessException("Mật khẩu không được để trống.");
        }

        if (_currentUser.IsManager)
        {
            if (request.Role != UserRole.Staff)
            {
                throw new ForbiddenException("Quản lý cơ sở (Manager) chỉ có quyền tạo tài khoản Nhân viên (Staff).");
            }

            if (!_currentUser.BranchId.HasValue || request.BranchId != _currentUser.BranchId.Value)
            {
                throw new ForbiddenException("Bạn chỉ được tạo nhân viên thuộc chi nhánh cơ sở của mình.");
            }
        }
        else if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền tạo tài khoản nhân sự.");
        }

        // Validate role-branch constraint (Section 4.1)
        if (request.Role == UserRole.Manager || request.Role == UserRole.Staff)
        {
            if (!request.BranchId.HasValue)
            {
                throw new BusinessException("Tài khoản Quản lý (Manager) và Nhân viên (Staff) bắt buộc phải gắn với một chi nhánh cơ sở.");
            }
        }
        else if (request.Role == UserRole.Admin)
        {
            if (request.BranchId.HasValue)
            {
                throw new BusinessException("Tài khoản Quản trị viên (Admin) không được gắn với cơ sở cụ thể.");
            }
        }

        await using var db = await _factory.CreateDbContextAsync();

        bool exists = await db.Users.AnyAsync(u => u.Username.Trim().ToLower() == request.Username.Trim().ToLower());
        if (exists)
        {
            throw new BusinessException($"Tên đăng nhập '{request.Username}' đã tồn tại trong hệ thống.");
        }

        string? branchName = null;
        if (request.BranchId.HasValue)
        {
            var branch = await db.Branches.FindAsync(request.BranchId.Value);
            if (branch == null)
            {
                throw new BusinessException("Cơ sở được chỉ định không tồn tại.");
            }
            branchName = branch.Name;
        }

        var user = new AppUser
        {
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? request.Username.Trim() : request.FullName.Trim(),
            Role = request.Role,
            BranchId = request.BranchId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new UserItemDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            branchName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        );
    }

    public async Task<UserItemDto> UpdateUserAsync(int userId, UpdateUserRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var user = await db.Users.Include(u => u.Branch).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new BusinessException("Không tìm thấy tài khoản cần cập nhật.");
        }

        if (_currentUser.IsManager)
        {
            if (user.Role != UserRole.Staff || user.BranchId != _currentUser.BranchId || request.Role != UserRole.Staff || request.BranchId != _currentUser.BranchId)
            {
                throw new ForbiddenException("Bạn chỉ có quyền sửa thông tin nhân viên trực thuộc cơ sở của mình.");
            }
        }
        else if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền cập nhật tài khoản.");
        }

        if (request.Role == UserRole.Manager || request.Role == UserRole.Staff)
        {
            if (!request.BranchId.HasValue)
            {
                throw new BusinessException("Tài khoản Quản lý và Nhân viên bắt buộc phải gắn với cơ sở.");
            }
        }

        user.FullName = string.IsNullOrWhiteSpace(request.FullName) ? user.Username : request.FullName.Trim();
        user.Role = request.Role;
        user.BranchId = request.BranchId;
        user.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        string? branchName = null;
        if (user.BranchId.HasValue)
        {
            var b = await db.Branches.FindAsync(user.BranchId.Value);
            branchName = b?.Name;
        }

        return new UserItemDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.BranchId,
            branchName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt
        );
    }

    public async Task ToggleUserStatusAsync(int userId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BusinessException("Không tìm thấy tài khoản.");
        }

        if (_currentUser.IsManager && (user.BranchId != _currentUser.BranchId || user.Role != UserRole.Staff))
        {
            throw new ForbiddenException("Bạn chỉ có quyền thao tác với nhân viên thuộc cơ sở của mình.");
        }
        else if (!_currentUser.IsAdmin && !_currentUser.IsManager)
        {
            throw new ForbiddenException("Bạn không có quyền thay đổi trạng thái tài khoản.");
        }

        if (_currentUser.Id == userId)
        {
            throw new BusinessException("Bạn không thể tự khóa tài khoản của chính mình.");
        }

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new BusinessException("Mật khẩu mới không được để trống.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            throw new BusinessException("Không tìm thấy tài khoản.");
        }

        if (_currentUser.IsManager && (user.BranchId != _currentUser.BranchId || user.Role != UserRole.Staff))
        {
            throw new ForbiddenException("Bạn chỉ có quyền reset mật khẩu cho nhân viên thuộc cơ sở của mình.");
        }
        else if (!_currentUser.IsAdmin && !_currentUser.IsManager)
        {
            throw new ForbiddenException("Bạn không có quyền reset mật khẩu tài khoản này.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync();
    }
}
