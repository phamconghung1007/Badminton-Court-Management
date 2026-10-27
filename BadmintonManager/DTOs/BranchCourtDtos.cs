using System;

namespace BadmintonManager.DTOs;

public record BranchDto(
    int Id,
    string Name,
    string Address,
    string Phone,
    bool IsActive,
    int CourtCount,
    DateTime CreatedAt
);

public record SaveBranchRequest(
    string Name,
    string Address,
    string Phone,
    bool IsActive = true
);

public record CourtDto(
    int Id,
    int BranchId,
    string BranchName,
    string Name,
    bool IsActive,
    string? Note
);

public record SaveCourtRequest(
    int BranchId,
    string Name,
    bool IsActive = true,
    string? Note = null
);
