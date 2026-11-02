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

public class BranchCourtService : IBranchCourtService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public BranchCourtService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    public async Task<List<BranchDto>> GetAllBranchDtosAsync(bool onlyActive = false)
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

        return await query
            .OrderBy(b => b.Id)
            .Select(b => new BranchDto(
                b.Id,
                b.Name,
                b.Address ?? "",
                b.Phone ?? "",
                b.IsActive,
                b.Courts.Count,
                b.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<Branch?> GetBranchByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Branches.AsNoTracking().Where(b => b.Id == id);

        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue && id != _currentUser.BranchId.Value)
            {
                throw new ForbiddenException("Bạn không có quyền truy cập thông tin cơ sở khác.");
            }
        }

        return await query.Include(b => b.Courts).FirstOrDefaultAsync();
    }

    public async Task<BranchDto> CreateBranchAsync(SaveBranchRequest request)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền tạo cơ sở mới.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Tên cơ sở không được để trống.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        bool exists = await db.Branches.AnyAsync(b => b.Name.Trim().ToLower() == request.Name.Trim().ToLower());
        if (exists)
        {
            throw new BusinessException($"Tên cơ sở '{request.Name}' đã tồn tại trong hệ thống.");
        }

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            Address = request.Address?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            IsActive = request.IsActive,
            CreatedAt = DateTime.Now
        };

        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        return new BranchDto(branch.Id, branch.Name, branch.Address, branch.Phone, branch.IsActive, 0, branch.CreatedAt);
    }

    public async Task<BranchDto> UpdateBranchAsync(int branchId, SaveBranchRequest request)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền cập nhật thông tin cơ sở.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Tên cơ sở không được để trống.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var branch = await db.Branches.Include(b => b.Courts).FirstOrDefaultAsync(b => b.Id == branchId);
        if (branch == null)
        {
            throw new BusinessException("Không tìm thấy cơ sở cần cập nhật.");
        }

        bool exists = await db.Branches.AnyAsync(b => b.Id != branchId && b.Name.Trim().ToLower() == request.Name.Trim().ToLower());
        if (exists)
        {
            throw new BusinessException($"Tên cơ sở '{request.Name}' đã tồn tại trong hệ thống.");
        }

        branch.Name = request.Name.Trim();
        branch.Address = request.Address?.Trim() ?? string.Empty;
        branch.Phone = request.Phone?.Trim() ?? string.Empty;
        branch.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        return new BranchDto(branch.Id, branch.Name, branch.Address, branch.Phone, branch.IsActive, branch.Courts.Count, branch.CreatedAt);
    }

    public async Task ToggleBranchStatusAsync(int branchId)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền thay đổi trạng thái cơ sở.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var branch = await db.Branches.FindAsync(branchId);
        if (branch == null)
        {
            throw new BusinessException("Không tìm thấy cơ sở.");
        }

        branch.IsActive = !branch.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task<List<CourtDto>> GetAllCourtDtosAsync(int? branchId = null, bool onlyActive = false)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Courts.AsNoTracking().Include(c => c.Branch).AsQueryable();

        // Enforce Manager/Staff restriction to their own branch
        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            int assignedBranchId = _currentUser.BranchId ?? -1;
            query = query.Where(c => c.BranchId == assignedBranchId);
        }
        else if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(c => c.BranchId == branchId.Value);
        }

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.BranchId)
            .ThenBy(c => c.Name)
            .Select(c => new CourtDto(
                c.Id,
                c.BranchId,
                c.Branch.Name,
                c.Name,
                c.IsActive,
                c.Note
            ))
            .ToListAsync();
    }

    public async Task<List<CourtDto>> GetCourtDtosByBranchIdAsync(int branchId, bool onlyActive = false)
    {
        return await GetAllCourtDtosAsync(branchId, onlyActive);
    }

    public async Task<Court?> GetCourtByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var court = await db.Courts.Include(c => c.Branch).FirstOrDefaultAsync(c => c.Id == id);

        if (court != null && (_currentUser.IsManager || _currentUser.IsStaff))
        {
            if (court.BranchId != _currentUser.BranchId)
            {
                throw new ForbiddenException("Bạn không có quyền truy cập thông tin sân của cơ sở khác.");
            }
        }

        return court;
    }

    public async Task<CourtDto> CreateCourtAsync(SaveCourtRequest request)
    {
        if (!_currentUser.IsAdmin && (!_currentUser.IsManager || _currentUser.BranchId != request.BranchId))
        {
            throw new ForbiddenException("Bạn không có quyền thêm sân cho cơ sở này.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Tên sân không được để trống.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var branch = await db.Branches.FindAsync(request.BranchId);
        if (branch == null)
        {
            throw new BusinessException("Cơ sở được chọn không tồn tại.");
        }

        bool exists = await db.Courts.AnyAsync(c => c.BranchId == request.BranchId && c.Name.Trim().ToLower() == request.Name.Trim().ToLower());
        if (exists)
        {
            throw new BusinessException($"Tên sân '{request.Name}' đã tồn tại trong cơ sở này.");
        }

        var court = new Court
        {
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            Note = request.Note?.Trim(),
            IsActive = request.IsActive
        };

        db.Courts.Add(court);
        await db.SaveChangesAsync();

        return new CourtDto(court.Id, court.BranchId, branch.Name, court.Name, court.IsActive, court.Note);
    }

    public async Task<CourtDto> UpdateCourtAsync(int courtId, SaveCourtRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var court = await db.Courts.Include(c => c.Branch).FirstOrDefaultAsync(c => c.Id == courtId);
        if (court == null)
        {
            throw new BusinessException("Không tìm thấy sân cần cập nhật.");
        }

        if (!_currentUser.IsAdmin && (!_currentUser.IsManager || _currentUser.BranchId != court.BranchId || _currentUser.BranchId != request.BranchId))
        {
            throw new ForbiddenException("Bạn không có quyền sửa thông tin sân của cơ sở này.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BusinessException("Tên sân không được để trống.");
        }

        bool exists = await db.Courts.AnyAsync(c => c.Id != courtId && c.BranchId == request.BranchId && c.Name.Trim().ToLower() == request.Name.Trim().ToLower());
        if (exists)
        {
            throw new BusinessException($"Tên sân '{request.Name}' đã tồn tại trong cơ sở này.");
        }

        court.BranchId = request.BranchId;
        court.Name = request.Name.Trim();
        court.Note = request.Note?.Trim();
        court.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        var branch = await db.Branches.FindAsync(court.BranchId);
        return new CourtDto(court.Id, court.BranchId, branch?.Name ?? string.Empty, court.Name, court.IsActive, court.Note);
    }

    public async Task ToggleCourtStatusAsync(int courtId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var court = await db.Courts.FindAsync(courtId);
        if (court == null)
        {
            throw new BusinessException("Không tìm thấy sân.");
        }

        if (!_currentUser.IsAdmin && (!_currentUser.IsManager || _currentUser.BranchId != court.BranchId))
        {
            throw new ForbiddenException("Bạn không có quyền thay đổi trạng thái sân này.");
        }

        court.IsActive = !court.IsActive;
        await db.SaveChangesAsync();
    }
}
