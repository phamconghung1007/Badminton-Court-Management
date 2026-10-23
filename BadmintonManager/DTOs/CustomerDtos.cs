using System;

namespace BadmintonManager.DTOs;

public record CustomerDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public int MembershipTierId { get; init; }
    public string TierName { get; init; } = string.Empty;
    public decimal DiscountPercent { get; init; }
    public int TotalPoints { get; init; }
    public string? MembershipCardNo { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsActive { get; init; }
}

public record CustomerCreateDto(
    string FullName,
    string Phone,
    string? MembershipCardNo
);

public record CustomerUpdateDto(
    int Id,
    string FullName,
    string Phone,
    string? MembershipCardNo,
    bool IsActive
);
