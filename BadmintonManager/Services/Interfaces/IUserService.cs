using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IUserService
{
    Task<List<UserItemDto>> GetUserDtosAsync(int? branchId = null, UserRole? role = null);
    Task<List<AppUser>> GetUsersAsync(int? branchId = null, UserRole? role = null);
    Task<AppUser?> GetUserByIdAsync(int id);
    Task<UserItemDto> CreateUserAsync(CreateUserRequest request);
    Task<UserItemDto> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task ToggleUserStatusAsync(int userId);
    Task ResetPasswordAsync(int userId, string newPassword);
}
