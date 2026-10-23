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
