using System;
using System.Collections.Generic;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public record PriceCalculationResult
{
    public decimal SubTotal { get; init; }
    public decimal DiscountPercent { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal DepositAmount { get; init; }
    public List<SlotPriceDetail> SlotDetails { get; init; } = new();
}

public record SlotPriceDetail(
    DateTime Start,
    DateTime End,
    DayType DayType,
    decimal HourlyRate,
    decimal SlotPrice
);

public record PriceRuleDto
{
    public int Id { get; init; }
    public DayType DayType { get; init; }
    public string DayTypeName => DayType == DayType.Weekday ? "Ngày thường (T2 - T6)" : "Cuối tuần (T7 - CN)";
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public string StartTimeDisplay => $"{StartMinute / 60:D2}:{StartMinute % 60:D2}";
    public string EndTimeDisplay => $"{EndMinute / 60:D2}:{EndMinute % 60:D2}";
    public string TimeRange => $"{StartTimeDisplay} - {EndTimeDisplay}";
    public int DurationMinutes => EndMinute - StartMinute;
    public decimal PricePerHour { get; init; }
    public bool IsActive { get; init; }
    public string StatusText => IsActive ? "Đang áp dụng" : "Tạm dừng";
}

public record SavePriceRuleRequest
{
    public int? Id { get; init; }
    public DayType DayType { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public decimal PricePerHour { get; init; }
    public bool IsActive { get; init; } = true;
}

public record PriceRuleCoverageValidationResult
{
    public bool IsValid { get; init; }
    public DayType DayType { get; init; }
    public string DayTypeName => DayType == DayType.Weekday ? "Ngày thường" : "Cuối tuần";
    public string Summary { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();
}

public record MembershipTierDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
    public int SortOrder { get; init; }
    public int CustomerCount { get; init; }
    public string DiscountText => $"{DiscountPercent:0.#}%";
}

public record SaveMembershipTierRequest
{
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MinPoints { get; init; }
    public decimal DiscountPercent { get; init; }
    public int SortOrder { get; init; }
}
