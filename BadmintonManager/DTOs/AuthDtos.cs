using System;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    int UserId,
    string Username,
    string FullName,
    UserRole Role,
    int? BranchId,
    int? CustomerId
);

public record RegisterCustomerRequest(
    string Username,
    string Password,
    string FullName,
    string Phone
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
