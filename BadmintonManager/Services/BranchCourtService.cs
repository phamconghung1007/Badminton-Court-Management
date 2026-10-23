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

public class BranchCourtService : IBranchCourtService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public BranchCourtService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<List<Branch>> GetAllBranchesAsync(bool onlyActive = true)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Branches.AsNoTracking();

        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue)
            {
                query = query.Where(b => b.Id == _currentUser.BranchId.Value);
            }
        }

        if (onlyActive)
        {
            query = query.Where(b => b.IsActive);
        }

        return await query.Include(b => b.Courts).ToListAsync();
    }

    public async Task<Branch?> GetBranchByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Branches
            .AsNoTracking()
            .Include(b => b.Courts)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Branch> CreateBranchAsync(Branch branch)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền tạo cơ sở mới.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        return branch;
    }

    public async Task<Branch> UpdateBranchAsync(Branch branch)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền cập nhật thông tin cơ sở.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        db.Branches.Update(branch);
        await db.SaveChangesAsync();
        return branch;
    }

    public async Task<List<Court>> GetCourtsByBranchIdAsync(int branchId, bool onlyActive = true)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Courts.AsNoTracking().Where(c => c.BranchId == branchId);

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.ToListAsync();
    }

    public async Task<Court> CreateCourtAsync(Court court)
    {
        if (!_currentUser.IsAdmin && (!_currentUser.IsManager || _currentUser.BranchId != court.BranchId))
        {
            throw new ForbiddenException("Bạn không có quyền thêm sân cho cơ sở này.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        bool exists = await db.Courts.AnyAsync(c => c.BranchId == court.BranchId && c.Name == court.Name);
        if (exists)
        {
            throw new BusinessException($"Tên sân '{court.Name}' đã tồn tại trong cơ sở này.");
        }

        db.Courts.Add(court);
        await db.SaveChangesAsync();
        return court;
    }

    public async Task<Court> UpdateCourtAsync(Court court)
    {
        if (!_currentUser.IsAdmin && (!_currentUser.IsManager || _currentUser.BranchId != court.BranchId))
        {
            throw new ForbiddenException("Bạn không có quyền sửa thông tin sân này.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        bool exists = await db.Courts.AnyAsync(c => c.BranchId == court.BranchId && c.Name == court.Name && c.Id != court.Id);
        if (exists)
        {
            throw new BusinessException($"Tên sân '{court.Name}' đã tồn tại trong cơ sở này.");
        }

        db.Courts.Update(court);
        await db.SaveChangesAsync();
        return court;
    }
}
