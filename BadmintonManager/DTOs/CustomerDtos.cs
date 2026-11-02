using System;
using System.Collections.Generic;

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
    public string StatusText => IsActive ? "Hoạt động" : "Tạm khóa";
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

public record CustomerPointHistoryDto(
    int Id,
    int Points,
    int BalanceAfter,
    string Reason,
    DateTime CreatedAt,
    string? BookingCode
);

public record CustomerDetailDto
{
    public CustomerDto Customer { get; init; } = null!;
    public int TotalBookings { get; init; }
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public decimal TotalSpent { get; init; }
    public List<CustomerPointHistoryDto> PointHistory { get; init; } = new();
    public List<BookingItemDto> RecentBookings { get; init; } = new();
    public List<FixedScheduleDto> ActiveFixedSchedules { get; init; } = new();
}
