using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;

namespace BadmintonManager.Services.Interfaces;

public interface IMembershipService
{
    Task<List<MembershipTier>> GetTiersAsync();
    Task AddPointsAsync(int customerId, int? bookingId, decimal finalAmount);
    Task DeductPointsAsync(int customerId, int? bookingId, int points, string reason);

    // Day 3 enhanced methods for Admin tier management
    Task<List<MembershipTierDto>> GetAllTiersWithStatsAsync();
    Task<MembershipTierDto?> GetTierByIdAsync(int id);
    Task<MembershipTierDto> SaveTierAsync(SaveMembershipTierRequest request);
    Task DeleteTierAsync(int id);
}
