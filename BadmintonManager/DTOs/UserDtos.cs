using System;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public record UserItemDto(
    int Id,
    string Username,
    string FullName,
    UserRole Role,
    int? BranchId,
    string? BranchName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record CreateUserRequest(
    string Username,
    string Password,
    string FullName,
    UserRole Role,
    int? BranchId
);

public record UpdateUserRequest(
    string FullName,
    UserRole Role,
    int? BranchId,
    bool IsActive
);
