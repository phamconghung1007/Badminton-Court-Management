using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.Models.Entities;

namespace BadmintonManager.Services.Interfaces;

public interface IMembershipService
{
    Task<List<MembershipTier>> GetTiersAsync();
    Task AddPointsAsync(int customerId, int? bookingId, decimal finalAmount);
    Task DeductPointsAsync(int customerId, int? bookingId, int points, string reason);
}
