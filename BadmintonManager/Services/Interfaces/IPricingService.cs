using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IPricingService
{
    Task<PriceCalculationResult> CalculatePriceAsync(int courtId, DateTime startTime, DateTime endTime, int? customerId);
    Task<List<PriceRule>> GetPriceRulesAsync();
    Task UpdatePriceRuleAsync(PriceRule rule);
    Task ValidatePriceRulesCoverageAsync();

    // Day 3 enhanced methods for Admin management & BR-10 validation
    Task<List<PriceRuleDto>> GetAllPriceRulesAsync(DayType? dayType = null);
    Task<PriceRuleDto?> GetPriceRuleByIdAsync(int id);
    Task<PriceRuleDto> SavePriceRuleAsync(SavePriceRuleRequest request);
    Task DeletePriceRuleAsync(int id);
    Task TogglePriceRuleStatusAsync(int id);
    Task<PriceRuleCoverageValidationResult> ValidateCoverageAsync(DayType dayType);
}
