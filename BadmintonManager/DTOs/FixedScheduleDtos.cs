using System;
using System.Collections.Generic;

namespace BadmintonManager.DTOs;

public record FixedScheduleDto
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public int CourtId { get; init; }
    public string CourtName { get; init; } = string.Empty;
    public int BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int DayOfWeek { get; init; }
    public string DayOfWeekName => DayOfWeek switch
    {
        0 => "Chủ Nhật",
        1 => "Thứ Hai",
        2 => "Thứ Ba",
        3 => "Thứ Tư",
        4 => "Thứ Năm",
        5 => "Thứ Sáu",
        6 => "Thứ Bảy",
        _ => $"Thứ {DayOfWeek}"
    };
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public string StartTimeDisplay => $"{StartMinute / 60:D2}:{StartMinute % 60:D2}";
    public string EndTimeDisplay => $"{EndMinute / 60:D2}:{EndMinute % 60:D2}";
    public string TimeRange => $"{StartTimeDisplay} - {EndTimeDisplay}";
    public DateOnly ValidFrom { get; init; }
    public DateOnly ValidTo { get; init; }
    public string ValidRangeDisplay => $"{ValidFrom:dd/MM/yyyy} - {ValidTo:dd/MM/yyyy}";
    public bool IsActive { get; init; }
    public string StatusText => IsActive ? "Đang hiệu lực" : "Đã hủy";
    public DateTime CreatedAt { get; init; }
    public int TotalBookingsCount { get; init; }
    public int FutureBookingsCount { get; init; }
}

public record CreateFixedScheduleRequest
{
    public int CustomerId { get; init; }
    public int CourtId { get; init; }
    public int DayOfWeek { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public int? NumberOfWeeks { get; init; } = 4;
}

public record SkippedScheduleDateDto(
    DateOnly Date,
    string DayOfWeekName,
    string Reason
);

public record FixedScheduleResultDto
{
    public int ScheduleId { get; init; }
    public int TotalWeeks { get; init; }
    public int CreatedCount { get; init; }
    public List<int> CreatedBookingIds { get; init; } = new();
    public List<DateOnly> SkippedDates { get; init; } = new();
    public List<SkippedScheduleDateDto> SkippedDetails { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}
