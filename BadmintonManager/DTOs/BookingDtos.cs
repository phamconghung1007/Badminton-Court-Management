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
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateBookingRequest
{
    public int CourtId { get; init; }
    public int? CustomerId { get; init; }
    public string? GuestName { get; init; }
    public string? GuestPhone { get; init; }
    public BookingType BookingType { get; init; } = BookingType.Single;
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

public record CreateFixedScheduleRequest
{
    public int CustomerId { get; init; }
    public int CourtId { get; init; }
    public int DayOfWeek { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly ValidTo { get; init; }
}

public record FixedScheduleResultDto
{
    public int CreatedCount { get; init; }
    public List<DateOnly> SkippedDates { get; init; } = new();
}
