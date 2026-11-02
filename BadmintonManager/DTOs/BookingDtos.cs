using System;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public record BookingItemDto
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public int CourtId { get; init; }
    public string CourtName { get; init; } = string.Empty;
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? GuestName { get; init; }
    public string? GuestPhone { get; init; }
    public BookingType BookingType { get; init; }
    public int? FixedScheduleId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount => Math.Max(0, FinalAmount - PaidAmount);
    public string? CustomerPhone { get; init; }
    public bool IsFixed => FixedScheduleId.HasValue;
    public DateTime CreatedAt { get; init; }
}

public record CreateBookingRequest
{
    public int CourtId { get; init; }
    public int? CustomerId { get; init; }
    public string? GuestName { get; init; }
    public string? GuestPhone { get; init; }
    public BookingType BookingType { get; init; } = BookingType.Single;
    public int? FixedScheduleId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}

public record UpdateBookingRequest
{
    public int BookingId { get; init; }
    public int CourtId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}

public record CancelBookingRequest
{
    public int BookingId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record AvailableCourtDto
{
    public int CourtId { get; init; }
    public string CourtName { get; init; } = string.Empty;
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
}

public record SearchAvailableCourtsRequest
{
    public int BranchId { get; init; }
    public DateTime Date { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
}
