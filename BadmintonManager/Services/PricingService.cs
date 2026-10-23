using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class PricingService : IPricingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ISettingService _settingService;

    public PricingService(IDbContextFactory<AppDbContext> factory, ISettingService settingService)
    {
        _factory = factory;
        _settingService = settingService;
    }

    public async Task<PriceCalculationResult> CalculatePriceAsync(int courtId, DateTime startTime, DateTime endTime, int? customerId)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var rules = await db.PriceRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();

        decimal discountPercent = 0m;
        if (customerId.HasValue)
        {
            var customer = await db.Customers
                .AsNoTracking()
                .Include(c => c.MembershipTier)
                .FirstOrDefaultAsync(c => c.Id == customerId.Value);

            if (customer != null)
            {
                discountPercent = customer.MembershipTier.DiscountPercent;
            }
        }

        decimal subTotal = 0m;
        var slotDetails = new List<SlotPriceDetail>();

        DateTime current = startTime;
        while (current < endTime)
        {
            DateTime next = current.AddMinutes(30);
            DayType dayType = (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
                ? DayType.Weekend
                : DayType.Weekday;

            int minuteOfDay = current.Hour * 60 + current.Minute;

            var matchingRule = rules.FirstOrDefault(r =>
                r.DayType == dayType &&
                minuteOfDay >= r.StartMinute &&
                minuteOfDay < r.EndMinute);

            if (matchingRule == null)
            {
                throw new BusinessException($"Chưa cấu hình bảng giá cho khung giờ {current:HH:mm} ({dayType}). Vui lòng liên hệ Admin.");
            }

            decimal slotPrice = matchingRule.PricePerHour / 2m;
            subTotal += slotPrice;

            slotDetails.Add(new SlotPriceDetail(
                current,
                next,
                dayType,
                matchingRule.PricePerHour,
                slotPrice
            ));

            current = next;
        }

        decimal discountAmount = Math.Round(subTotal * (discountPercent / 100m), 0);
        decimal finalAmount = subTotal - discountAmount;

        int depositPct = await _settingService.GetIntSettingAsync(Constants.Settings.DepositPercent, 30);
        decimal rawDeposit = finalAmount * (depositPct / 100m);
        // Round to nearest 1,000 VND (BR-11)
        decimal depositAmount = Math.Round(rawDeposit / 1000m, MidpointRounding.AwayFromZero) * 1000m;

        return new PriceCalculationResult
        {
            SubTotal = subTotal,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            DepositAmount = depositAmount,
            SlotDetails = slotDetails
        };
    }

    public async Task<List<PriceRule>> GetPriceRulesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.PriceRules
            .AsNoTracking()
            .OrderBy(r => r.DayType)
            .ThenBy(r => r.StartMinute)
            .ToListAsync();
    }

    public async Task UpdatePriceRuleAsync(PriceRule rule)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.PriceRules.FindAsync(rule.Id);
        if (existing == null)
        {
            throw new BusinessException("Không tìm thấy quy tắc giá.");
        }

        existing.PricePerHour = rule.PricePerHour;
        existing.IsActive = rule.IsActive;

        await db.SaveChangesAsync();
    }

    public async Task ValidatePriceRulesCoverageAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rules = await db.PriceRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();

        foreach (DayType dayType in new[] { DayType.Weekday, DayType.Weekend })
        {
            var dayRules = rules
                .Where(r => r.DayType == dayType)
                .OrderBy(r => r.StartMinute)
                .ToList();

            if (!dayRules.Any())
            {
                throw new BusinessException($"Chưa có cấu hình giá cho {dayType}.");
            }

            if (dayRules.First().StartMinute > 360) // 06:00
            {
                throw new BusinessException($"Bảng giá {dayType} chưa bao phủ từ 06:00.");
            }

            if (dayRules.Last().EndMinute < 1440) // 24:00
            {
                throw new BusinessException($"Bảng giá {dayType} chưa bao phủ đến 24:00.");
            }

            for (int i = 0; i < dayRules.Count - 1; i++)
            {
                if (dayRules[i].EndMinute < dayRules[i + 1].StartMinute)
                {
                    throw new BusinessException($"Bảng giá {dayType} bị gián đoạn giữa {dayRules[i].EndMinute} và {dayRules[i + 1].StartMinute}.");
                }
                if (dayRules[i].EndMinute > dayRules[i + 1].StartMinute)
                {
                    throw new BusinessException($"Bảng giá {dayType} bị chồng lấn khung giờ.");
                }
            }
        }
    }
}
