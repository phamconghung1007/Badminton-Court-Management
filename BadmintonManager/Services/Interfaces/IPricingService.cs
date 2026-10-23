using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;

namespace BadmintonManager.Services.Interfaces;

public interface IPricingService
{
    Task<PriceCalculationResult> CalculatePriceAsync(int courtId, DateTime startTime, DateTime endTime, int? customerId);
    Task<List<PriceRule>> GetPriceRulesAsync();
    Task UpdatePriceRuleAsync(PriceRule rule);
    Task ValidatePriceRulesCoverageAsync();
}
