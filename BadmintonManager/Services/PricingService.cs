using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class PricingService : IPricingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ISettingService _settingService;
    private readonly CurrentUserContext? _currentUser;

    public PricingService(
        IDbContextFactory<AppDbContext> factory,
        ISettingService settingService,
        CurrentUserContext? currentUser = null)
    {
        _factory = factory;
        _settingService = settingService;
        _currentUser = currentUser;
    }

    private void EnsureAdmin()
    {
        if (_currentUser != null && _currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên (Admin) mới có quyền cấu hình bảng giá.");
        }
    }

    public async Task<PriceCalculationResult> CalculatePriceAsync(int courtId, DateTime startTime, DateTime endTime, int? customerId)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var rules = await db.PriceRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();

        string tierName = "Vãng lai";
        decimal discountPercent = 0m;
        if (customerId.HasValue)
        {
            var customer = await db.Customers
                .AsNoTracking()
                .Include(c => c.MembershipTier)
                .FirstOrDefaultAsync(c => c.Id == customerId.Value);

            if (customer != null && customer.MembershipTier != null)
            {
                discountPercent = customer.MembershipTier.DiscountPercent;
                tierName = customer.MembershipTier.Name;
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
            DepositPercent = depositPct,
            DepositAmount = depositAmount,
            TierName = tierName,
            TotalMinutes = (int)(endTime - startTime).TotalMinutes,
            SlotDetails = slotDetails
        };
    }

    public async Task<PriceCalculationResult> CalculatePriceDirectAsync(DateTime startTime, DateTime endTime, decimal discountPercent, int? depositPercent = null)
    {
        if (endTime <= startTime)
        {
            throw new BusinessException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var rules = await db.PriceRules.AsNoTracking().Where(r => r.IsActive).ToListAsync();

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

        int depositPct = depositPercent ?? await _settingService.GetIntSettingAsync(Constants.Settings.DepositPercent, 30);
        decimal rawDeposit = finalAmount * (depositPct / 100m);
        // Round to nearest 1,000 VND (BR-11)
        decimal depositAmount = Math.Round(rawDeposit / 1000m, MidpointRounding.AwayFromZero) * 1000m;

        return new PriceCalculationResult
        {
            SubTotal = subTotal,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            DepositPercent = depositPct,
            DepositAmount = depositAmount,
            TierName = string.Empty,
            TotalMinutes = (int)(endTime - startTime).TotalMinutes,
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
        EnsureAdmin();
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

    public async Task<List<PriceRuleDto>> GetAllPriceRulesAsync(DayType? dayType = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.PriceRules.AsNoTracking();

        if (dayType.HasValue)
        {
            query = query.Where(r => r.DayType == dayType.Value);
        }

        var list = await query
            .OrderBy(r => r.DayType)
            .ThenBy(r => r.StartMinute)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<PriceRuleDto?> GetPriceRuleByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rule = await db.PriceRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return rule == null ? null : MapToDto(rule);
    }

    public async Task<PriceRuleDto> SavePriceRuleAsync(SavePriceRuleRequest request)
    {
        EnsureAdmin();

        // 1. Basic validation
        if (request.StartMinute < 360 || request.StartMinute >= 1440)
        {
            throw new BusinessException("Giờ bắt đầu phải nằm trong khoảng từ 06:00 đến trước 24:00.");
        }

        if (request.EndMinute <= request.StartMinute || request.EndMinute > 1440)
        {
            throw new BusinessException("Giờ kết thúc phải lớn hơn giờ bắt đầu và tối đa là 24:00.");
        }

        if (request.StartMinute % 30 != 0 || request.EndMinute % 30 != 0)
        {
            throw new BusinessException("Khung giờ bắt đầu và kết thúc phải là bội số của 30 phút (00 hoặc 30).");
        }

        if (request.PricePerHour <= 0)
        {
            throw new BusinessException("Đơn giá theo giờ phải lớn hơn 0 VNĐ.");
        }

        await using var db = await _factory.CreateDbContextAsync();

        // 2. Overlap validation (BR-10: Các khung giờ của cùng loại ngày không được chồng nhau)
        if (request.IsActive)
        {
            var conflictingRule = await db.PriceRules
                .Where(r => r.DayType == request.DayType && r.IsActive)
                .Where(r => !request.Id.HasValue || r.Id != request.Id.Value)
                .Where(r => r.StartMinute < request.EndMinute && request.StartMinute < r.EndMinute)
                .FirstOrDefaultAsync();

            if (conflictingRule != null)
            {
                string conflictStart = $"{conflictingRule.StartMinute / 60:D2}:{conflictingRule.StartMinute % 60:D2}";
                string conflictEnd = $"{conflictingRule.EndMinute / 60:D2}:{conflictingRule.EndMinute % 60:D2}";
                string reqStart = $"{request.StartMinute / 60:D2}:{request.StartMinute % 60:D2}";
                string reqEnd = $"{request.EndMinute / 60:D2}:{request.EndMinute % 60:D2}";

                throw new BusinessException($"Khung giờ {reqStart} - {reqEnd} bị chồng lấn với khung giờ đang áp dụng {conflictStart} - {conflictEnd} ({conflictingRule.PricePerHour:N0} đ/h). BR-10 quy định các khung giờ của cùng loại ngày không được chồng nhau.");
            }
        }

        PriceRule entity;
        if (!request.Id.HasValue || request.Id.Value == 0)
        {
            entity = new PriceRule
            {
                DayType = request.DayType,
                StartMinute = request.StartMinute,
                EndMinute = request.EndMinute,
                PricePerHour = request.PricePerHour,
                IsActive = request.IsActive
            };
            db.PriceRules.Add(entity);
        }
        else
        {
            entity = await db.PriceRules.FindAsync(request.Id.Value)
                ?? throw new BusinessException($"Không tìm thấy khung giá #{request.Id.Value}");

            entity.DayType = request.DayType;
            entity.StartMinute = request.StartMinute;
            entity.EndMinute = request.EndMinute;
            entity.PricePerHour = request.PricePerHour;
            entity.IsActive = request.IsActive;
        }

        await db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeletePriceRuleAsync(int id)
    {
        EnsureAdmin();

        await using var db = await _factory.CreateDbContextAsync();
        var rule = await db.PriceRules.FindAsync(id)
            ?? throw new BusinessException($"Không tìm thấy khung giá #{id}");

        db.PriceRules.Remove(rule);
        await db.SaveChangesAsync();
    }

    public async Task TogglePriceRuleStatusAsync(int id)
    {
        EnsureAdmin();

        await using var db = await _factory.CreateDbContextAsync();
        var rule = await db.PriceRules.FindAsync(id)
            ?? throw new BusinessException($"Không tìm thấy khung giá #{id}");

        if (!rule.IsActive)
        {
            // If activating, verify it won't conflict with any active rules
            var conflictingRule = await db.PriceRules
                .Where(r => r.DayType == rule.DayType && r.IsActive && r.Id != rule.Id)
                .Where(r => r.StartMinute < rule.EndMinute && rule.StartMinute < r.EndMinute)
                .FirstOrDefaultAsync();

            if (conflictingRule != null)
            {
                string conflictStart = $"{conflictingRule.StartMinute / 60:D2}:{conflictingRule.StartMinute % 60:D2}";
                string conflictEnd = $"{conflictingRule.EndMinute / 60:D2}:{conflictingRule.EndMinute % 60:D2}";
                throw new BusinessException($"Không thể kích hoạt vì trùng lấn với khung giờ đang áp dụng: {conflictStart} - {conflictEnd}.");
            }
        }

        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task<PriceRuleCoverageValidationResult> ValidateCoverageAsync(DayType dayType)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rules = await db.PriceRules
            .AsNoTracking()
            .Where(r => r.DayType == dayType && r.IsActive)
            .OrderBy(r => r.StartMinute)
            .ToListAsync();

        var errors = new List<string>();
        string dayTypeName = dayType == DayType.Weekday ? "Ngày thường" : "Cuối tuần";

        if (rules.Count == 0)
        {
            errors.Add($"Chưa có bất kỳ khung giờ giá nào đang kích hoạt cho {dayTypeName}.");
            return new PriceRuleCoverageValidationResult
            {
                IsValid = false,
                DayType = dayType,
                Summary = $"Chưa có khung giá nào cho {dayTypeName}.",
                Errors = errors
            };
        }

        if (rules.First().StartMinute > 360)
        {
            int gapEnd = rules.First().StartMinute;
            errors.Add($"Thiếu khung giờ mở cửa từ 06:00 đến {gapEnd / 60:D2}:{gapEnd % 60:D2}.");
        }
        else if (rules.First().StartMinute < 360)
        {
            errors.Add($"Khung giờ bắt đầu trước giờ mở cửa 06:00 ({rules.First().StartMinute / 60:D2}:{rules.First().StartMinute % 60:D2}).");
        }

        for (int i = 0; i < rules.Count - 1; i++)
        {
            var cur = rules[i];
            var nxt = rules[i + 1];
            if (cur.EndMinute < nxt.StartMinute)
            {
                errors.Add($"Khoảng trống giữa {cur.EndMinute / 60:D2}:{cur.EndMinute % 60:D2} và {nxt.StartMinute / 60:D2}:{nxt.StartMinute % 60:D2}.");
            }
            else if (cur.EndMinute > nxt.StartMinute)
            {
                errors.Add($"Chồng lấn khung giờ giữa [{cur.StartMinute / 60:D2}:{cur.StartMinute % 60:D2} - {cur.EndMinute / 60:D2}:{cur.EndMinute % 60:D2}] và [{nxt.StartMinute / 60:D2}:{nxt.StartMinute % 60:D2} - {nxt.EndMinute / 60:D2}:{nxt.EndMinute % 60:D2}].");
            }
        }

        if (rules.Last().EndMinute < 1440)
        {
            int gapStart = rules.Last().EndMinute;
            errors.Add($"Thiếu khung giờ đóng cửa từ {gapStart / 60:D2}:{gapStart % 60:D2} đến 24:00.");
        }
        else if (rules.Last().EndMinute > 1440)
        {
            errors.Add($"Khung giờ kết thúc vượt quá 24:00 ({rules.Last().EndMinute / 60:D2}:{rules.Last().EndMinute % 60:D2}).");
        }

        bool isValid = errors.Count == 0;
        string summary = isValid
            ? "Đã phủ kín toàn bộ 06:00 - 24:00 không có khoảng trống hay chồng lấn (Đạt chuẩn BR-10)."
            : $"Bảng giá {dayTypeName} chưa đạt chuẩn BR-10 ({errors.Count} vấn đề cần xử lý).";

        return new PriceRuleCoverageValidationResult
        {
            IsValid = isValid,
            DayType = dayType,
            Summary = summary,
            Errors = errors
        };
    }

    private static PriceRuleDto MapToDto(PriceRule r) => new()
    {
        Id = r.Id,
        DayType = r.DayType,
        StartMinute = r.StartMinute,
        EndMinute = r.EndMinute,
        PricePerHour = r.PricePerHour,
        IsActive = r.IsActive
    };
}
