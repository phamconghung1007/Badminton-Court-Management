using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.Models.Entities;

namespace BadmintonManager.Services.Interfaces;

public interface IBranchCourtService
{
    Task<List<Branch>> GetAllBranchesAsync(bool onlyActive = true);
    Task<Branch?> GetBranchByIdAsync(int id);
    Task<Branch> CreateBranchAsync(Branch branch);
    Task<Branch> UpdateBranchAsync(Branch branch);
    Task<List<Court>> GetCourtsByBranchIdAsync(int branchId, bool onlyActive = true);
    Task<Court> CreateCourtAsync(Court court);
    Task<Court> UpdateCourtAsync(Court court);
}
