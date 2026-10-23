using BadmintonManager.Models.Enums;

namespace BadmintonManager.Common;

public class CurrentUserContext
{
    public int? Id { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public UserRole? Role { get; private set; }
    public int? BranchId { get; private set; }
    public string? BranchName { get; private set; }
    public int? CustomerId { get; private set; }
    public string? TierName { get; private set; }

    public bool IsAuthenticated => Id.HasValue;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsManager => Role == UserRole.Manager;
    public bool IsStaff => Role == UserRole.Staff;
    public bool IsCustomer => Role == UserRole.Customer;

    public void SetUser(
        int id,
        string username,
        string fullName,
        UserRole role,
        int? branchId,
        string? branchName = null,
        int? customerId = null,
        string? tierName = null)
    {
        Id = id;
        Username = username;
        FullName = fullName;
        Role = role;
        BranchId = branchId;
        BranchName = branchName;
        CustomerId = customerId;
        TierName = tierName;
    }

    public void Clear()
    {
        Id = null;
        Username = null;
        FullName = null;
        Role = null;
        BranchId = null;
        BranchName = null;
        CustomerId = null;
        TierName = null;
    }
}
