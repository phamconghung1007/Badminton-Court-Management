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

public class UserService : IUserService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public UserService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<List<AppUser>> GetUsersAsync(int? branchId = null, UserRole? role = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Users.AsNoTracking().Include(u => u.Branch).AsQueryable();

        if (_currentUser.IsManager)
        {
            query = query.Where(u => u.BranchId == _currentUser.BranchId && u.Role == UserRole.Staff);
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
            throw new ForbiddenException();
        }

        return user;
    }

    public async Task<AppUser> CreateUserAsync(AppUser user, string password)
    {
        if (_currentUser.IsManager)
        {
            if (user.Role != UserRole.Staff || user.BranchId != _currentUser.BranchId)
            {
                throw new ForbiddenException("Manager chỉ được tạo tài khoản Staff cho cơ sở của mình.");
            }
        }
        else if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        await using var db = await _factory.CreateDbContextAsync();
        if (await db.Users.AnyAsync(u => u.Username == user.Username))
        {
            throw new BusinessException("Tên đăng nhập đã tồn tại.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.Now;
        user.IsActive = true;

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<AppUser> UpdateUserAsync(AppUser user)
    {
        if (_currentUser.IsManager)
        {
            if (user.Role != UserRole.Staff || user.BranchId != _currentUser.BranchId)
            {
                throw new ForbiddenException();
            }
        }
        else if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        await using var db = await _factory.CreateDbContextAsync();
        db.Users.Update(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task ToggleUserStatusAsync(int userId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        if (user == null) throw new BusinessException("Không tìm thấy tài khoản.");

        if (_currentUser.IsManager && (user.BranchId != _currentUser.BranchId || user.Role != UserRole.Staff))
        {
            throw new ForbiddenException();
        }

        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
    }
}
