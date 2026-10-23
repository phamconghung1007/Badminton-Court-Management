using System.Threading.Tasks;
using BadmintonManager.DTOs;

namespace BadmintonManager.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<CustomerDto> RegisterCustomerAsync(RegisterCustomerRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<UserProfileDto> GetCurrentUserProfileAsync();
}
