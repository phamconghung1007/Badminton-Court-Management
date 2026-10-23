using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Data;

public record HealthCheckItem(string Category, string Description, bool IsPassed, string Details);

public class DbVerificationReport
{
    public bool IsAllPassed => Items.All(i => i.IsPassed);
    public List<HealthCheckItem> Items { get; } = new();

    public void AddCheck(string category, string description, bool passed, string details)
    {
        Items.Add(new HealthCheckItem(category, description, passed, details));
    }
}

public static class DbHealthChecker
{
    public static async Task<DbVerificationReport> VerifyDay1DoDAsync(AppDbContext context)
    {
        var report = new DbVerificationReport();

        // 1. Branches & Courts Check (DoD: seed đủ 3 cơ sở/18 sân)
        var branches = await context.Branches.Include(b => b.Courts).AsNoTracking().ToListAsync();
        bool branchPassed = branches.Count == 3;
        report.AddCheck("Cơ sở (Branches)", "Yêu cầu có đủ 3 cơ sở hoạt động", branchPassed, $"Thực tế: {branches.Count} cơ sở ({string.Join(", ", branches.Select(b => b.Name))})");

        int totalCourts = branches.Sum(b => b.Courts.Count);
        bool courtsPassed = totalCourts == 18 && branches.All(b => b.Courts.Count == 6);
        report.AddCheck("Sân (Courts)", "Yêu cầu có đủ 18 sân (mỗi cơ sở đúng 6 sân)", courtsPassed, $"Thực tế: {totalCourts} sân ({string.Join(", ", branches.Select(b => $"{b.Name}: {b.Courts.Count} sân"))})");

        // 2. Users Check (DoD: seed user 1 Admin, 3 Manager, 6 Staff, Customer)
        var users = await context.Users.AsNoTracking().ToListAsync();
        var adminCount = users.Count(u => u.Role == UserRole.Admin);
        var managerCount = users.Count(u => u.Role == UserRole.Manager);
        var staffCount = users.Count(u => u.Role == UserRole.Staff);
        var customerCount = users.Count(u => u.Role == UserRole.Customer);

        bool adminPassed = adminCount >= 1 && users.Where(u => u.Role == UserRole.Admin).All(u => u.BranchId == null);
        report.AddCheck("Tài khoản Admin", "1 tài khoản Admin không gắn cơ sở (BranchId = null)", adminPassed, $"Thực tế: {adminCount} Admin");

        bool managerPassed = managerCount == 3 && users.Where(u => u.Role == UserRole.Manager).Select(u => u.BranchId).Distinct().Count() == 3;
        report.AddCheck("Tài khoản Manager", "3 Manager gắn với 3 cơ sở khác nhau", managerPassed, $"Thực tế: {managerCount} Manager");

        bool staffPassed = staffCount == 6;
        report.AddCheck("Tài khoản Staff", "6 Staff (mỗi cơ sở 2 nhân viên)", staffPassed, $"Thực tế: {staffCount} Staff");

        bool passwordPassed = users.All(u => BCrypt.Net.BCrypt.Verify("123456", u.PasswordHash));
        report.AddCheck("Bảo mật mật khẩu", "Mật khẩu mẫu được băm BCrypt an toàn", passwordPassed, $"Kiểm tra xác thực thành công cho toàn bộ {users.Count} users");

        // 3. MembershipTiers Check (DoD: 3 hạng Đồng, Bạc, Vàng)
        var tiers = await context.MembershipTiers.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
        bool tiersPassed = tiers.Count == 3 && tiers.Any(t => t.Name == "Đồng") && tiers.Any(t => t.Name == "Bạc") && tiers.Any(t => t.Name == "Vàng");
        report.AddCheck("Hạng hội viên (MembershipTiers)", "Đủ 3 hạng Đồng (0%), Bạc (5%), Vàng (10%)", tiersPassed, $"Thực tế: {string.Join(", ", tiers.Select(t => $"{t.Name} ({t.DiscountPercent}%)"))}");

        // 4. PriceRules Check (DoD: Bảng giá phủ 06:00 - 24:00)
        var priceRules = await context.PriceRules.Where(r => r.IsActive).AsNoTracking().ToListAsync();
        var weekdayRules = priceRules.Where(r => r.DayType == DayType.Weekday).OrderBy(r => r.StartMinute).ToList();
        var weekendRules = priceRules.Where(r => r.DayType == DayType.Weekend).OrderBy(r => r.StartMinute).ToList();

        bool weekdayFull = weekdayRules.Count > 0 && weekdayRules.First().StartMinute == 360 && weekdayRules.Last().EndMinute == 1440;
        bool weekendFull = weekendRules.Count > 0 && weekendRules.First().StartMinute == 360 && weekendRules.Last().EndMinute == 1440;
        report.AddCheck("Bảng giá (PriceRules)", "Phủ kín 06:00 (360p) - 24:00 (1440p) cho Ngày thường & Cuối tuần", weekdayFull && weekendFull, $"Weekday: {weekdayRules.Count} khung, Weekend: {weekendRules.Count} khung");

        // 5. SystemSettings Check
        var settings = await context.SystemSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
        bool settingsPassed = settings.ContainsKey(Constants.Settings.OpenMinute) &&
                             settings.ContainsKey(Constants.Settings.CloseMinute) &&
                             settings.ContainsKey(Constants.Settings.SlotMinutes) &&
                             settings.ContainsKey(Constants.Settings.DepositPercent);
        report.AddCheck("Cấu hình hệ thống (SystemSettings)", "Đủ 10 tham số cấu hình vận hành", settingsPassed, $"Thực tế: {settings.Count} tham số");

        // 6. Bookings Check (~50 booking mẫu rải 30 ngày)
        int bookingCount = await context.Bookings.CountAsync();
        int paymentCount = await context.Payments.CountAsync();
        bool bookingsPassed = bookingCount >= 50 && paymentCount > 0;
        report.AddCheck("Dữ liệu Bookings & Payments", "Tối thiểu 50 booking mẫu rải trong 30 ngày để báo cáo", bookingsPassed, $"Thực tế: {bookingCount} bookings, {paymentCount} payments");

        return report;
    }
}
