using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;

namespace BadmintonManager.Services.Interfaces;

public interface IBranchCourtService
{
    // Branches
    Task<List<BranchDto>> GetAllBranchDtosAsync(bool onlyActive = false);
    Task<Branch?> GetBranchByIdAsync(int id);
    Task<BranchDto> CreateBranchAsync(SaveBranchRequest request);
    Task<BranchDto> UpdateBranchAsync(int branchId, SaveBranchRequest request);
    Task ToggleBranchStatusAsync(int branchId);

    // Courts
    Task<List<CourtDto>> GetAllCourtDtosAsync(int? branchId = null, bool onlyActive = false);
    Task<List<CourtDto>> GetCourtDtosByBranchIdAsync(int branchId, bool onlyActive = false);
    Task<Court?> GetCourtByIdAsync(int id);
    Task<CourtDto> CreateCourtAsync(SaveCourtRequest request);
    Task<CourtDto> UpdateCourtAsync(int courtId, SaveCourtRequest request);
    Task ToggleCourtStatusAsync(int courtId);
}
