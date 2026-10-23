using BadmintonManager.Models.Enums;

namespace BadmintonManager.Common;

public class CurrentUserContext
{
    public int? Id { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public UserRole? Role { get; private set; }
    public int? BranchId { get; private set; }
    public int? CustomerId { get; private set; }

    public bool IsAuthenticated => Id.HasValue;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsManager => Role == UserRole.Manager;
    public bool IsStaff => Role == UserRole.Staff;
    public bool IsCustomer => Role == UserRole.Customer;

    public void SetUser(int id, string username, string fullName, UserRole role, int? branchId, int? customerId = null)
    {
        Id = id;
        Username = username;
        FullName = fullName;
        Role = role;
        BranchId = branchId;
        CustomerId = customerId;
    }

    public void Clear()
    {
        Id = null;
        Username = null;
        FullName = null;
        Role = null;
        BranchId = null;
        CustomerId = null;
    }
}
