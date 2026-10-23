using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IUserService
{
    Task<List<AppUser>> GetUsersAsync(int? branchId = null, UserRole? role = null);
    Task<AppUser?> GetUserByIdAsync(int id);
    Task<AppUser> CreateUserAsync(AppUser user, string password);
    Task<AppUser> UpdateUserAsync(AppUser user);
    Task ToggleUserStatusAsync(int userId);
}
