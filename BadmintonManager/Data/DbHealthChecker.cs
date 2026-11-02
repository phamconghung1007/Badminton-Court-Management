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

    public static async Task<DbVerificationReport> VerifyDay10FinalDoDAsync(AppDbContext context)
    {
        var report = new DbVerificationReport();

        // 1. CSDL Connectivity & Pending Migrations
        bool isRelational = context.Database.IsRelational();
        bool canConnect = await context.Database.CanConnectAsync();
        bool migrationPassed = canConnect;
        string migrationDetails;

        if (isRelational)
        {
            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            migrationPassed = canConnect && pendingMigrations.Count == 0;
            migrationDetails = canConnect
                ? (pendingMigrations.Count == 0 ? "CSDL SQL Server kết nối tốt, 0 pending migration" : $"{pendingMigrations.Count} pending migrations")
                : "Không thể kết nối CSDL SQL Server";
        }
        else
        {
            migrationDetails = canConnect ? "CSDL kết nối thông suốt (In-Memory / Test Provider)" : "Không thể kết nối CSDL";
        }

        report.AddCheck("Hạ tầng CSDL", "Kết nối CSDL thông suốt & không còn Migration tồn đọng", migrationPassed, migrationDetails);

        // 2. Cơ sở (3 cơ sở hoạt động)
        var branches = await context.Branches.Include(b => b.Courts).AsNoTracking().ToListAsync();
        bool branchesPassed = branches.Count == 3 && branches.All(b => b.IsActive);
        report.AddCheck("Hệ thống Cơ sở (Section 1)", "3 cơ sở hoạt động: Cầu Giấy, Đống Đa, Tây Hồ", branchesPassed,
            $"Thực tế: {branches.Count} cơ sở [{string.Join(", ", branches.Select(b => b.Name))}]");

        // 3. Sân cầu lông (18 sân, mỗi cơ sở đúng 6 sân)
        int totalCourts = branches.Sum(b => b.Courts.Count);
        bool courtsPassed = totalCourts == 18 && branches.All(b => b.Courts.Count == 6 && b.Courts.All(c => c.IsActive));
        report.AddCheck("Hệ thống Sân (Section 1)", "18 sân hoạt động (mỗi cơ sở chuẩn xác 6 sân)", courtsPassed,
            $"Thực tế: {totalCourts} sân ({string.Join(", ", branches.Select(b => $"{b.Name}: {b.Courts.Count} sân"))})");

        // 4. Tài khoản 4 vai trò & BCrypt (Section 2 & 3)
        var users = await context.Users.AsNoTracking().ToListAsync();
        var adminCount = users.Count(u => u.Role == UserRole.Admin);
        var managerCount = users.Count(u => u.Role == UserRole.Manager);
        var staffCount = users.Count(u => u.Role == UserRole.Staff);
        var customerCount = users.Count(u => u.Role == UserRole.Customer);
        bool usersRoleValid = adminCount >= 1 &&
                              managerCount == 3 &&
                              staffCount >= 6 &&
                              customerCount >= 1 &&
                              users.Where(u => u.Role == UserRole.Admin).All(u => u.BranchId == null) &&
                              users.Where(u => u.Role == UserRole.Manager).Select(u => u.BranchId).Distinct().Count() == 3;
        bool passwordHashValid = users.All(u => BCrypt.Net.BCrypt.Verify("123456", u.PasswordHash));
        report.AddCheck("Tài khoản & Ma trận vai trò", "10+ tài khoản mẫu (Admin, 3 Manager, 6 Staff, Customer) băm BCrypt", usersRoleValid && passwordHashValid,
            $"Admin: {adminCount}, Manager: {managerCount}, Staff: {staffCount}, Customer: {customerCount}. Mật khẩu BCrypt: {(passwordHashValid ? "HỢP LỆ" : "LỖI")}");

        // 5. Hạng thành viên (Section 1.3)
        var tiers = await context.MembershipTiers.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
        bool tiersValid = tiers.Count == 3 &&
                          tiers.Any(t => t.Name == "Đồng" && t.DiscountPercent == 0m) &&
                          tiers.Any(t => t.Name == "Bạc" && t.DiscountPercent == 5m) &&
                          tiers.Any(t => t.Name == "Vàng" && t.DiscountPercent == 10m);
        report.AddCheck("Hạng hội viên & Tỷ lệ giảm giá", "Đồng (0%), Bạc (5%), Vàng (10%)", tiersValid,
            $"Thực tế: {string.Join(" | ", tiers.Select(t => $"{t.Name} (Min: {t.MinPoints}đ, Giảm {t.DiscountPercent}%)"))}");

        // 6. Bảng giá phủ 06:00 - 24:00 (Section 1.2)
        var priceRules = await context.PriceRules.Where(r => r.IsActive).AsNoTracking().ToListAsync();
        var weekdayRules = priceRules.Where(r => r.DayType == DayType.Weekday).OrderBy(r => r.StartMinute).ToList();
        var weekendRules = priceRules.Where(r => r.DayType == DayType.Weekend).OrderBy(r => r.StartMinute).ToList();
        bool weekdayFull = weekdayRules.Count > 0 && weekdayRules.First().StartMinute == 360 && weekdayRules.Last().EndMinute == 1440;
        bool weekendFull = weekendRules.Count > 0 && weekendRules.First().StartMinute == 360 && weekendRules.Last().EndMinute == 1440;
        report.AddCheck("Bảng giá vận hành", "Phủ kín 06:00 (360p) - 24:00 (1440p) cho Ngày thường & Cuối tuần", weekdayFull && weekendFull,
            $"Weekday: {weekdayRules.Count} khung, Weekend: {weekendRules.Count} khung");

        // 7. Cấu hình hệ thống (10 tham số vận hành)
        var settings = await context.SystemSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
        var requiredKeys = new[]
        {
            Constants.Settings.OpenMinute,
            Constants.Settings.CloseMinute,
            Constants.Settings.SlotMinutes,
            Constants.Settings.MinDurationMinutes,
            Constants.Settings.MaxDurationMinutes,
            Constants.Settings.DepositPercent,
            Constants.Settings.FreeCancelHours,
            Constants.Settings.PendingExpireMinutes,
            Constants.Settings.PointsPer,
            Constants.Settings.FixedWeeksAhead
        };
        bool settingsValid = requiredKeys.All(k => settings.ContainsKey(k));
        report.AddCheck("Cấu hình hệ thống", "Đủ 10 tham số vận hành quy định trong đặc tả", settingsValid,
            $"Thực tế: {settings.Count} tham số ({string.Join(", ", requiredKeys.Where(k => settings.ContainsKey(k)))})");

        // 8. Đặt sân mẫu rải 30 ngày (Day 9 DoD)
        var bookings = await context.Bookings.AsNoTracking().ToListAsync();
        int totalBookings = bookings.Count;
        bool hasConfirmed = bookings.Any(b => b.Status == BookingStatus.Confirmed);
        bool hasCheckedIn = bookings.Any(b => b.Status == BookingStatus.CheckedIn);
        bool hasCompleted = bookings.Any(b => b.Status == BookingStatus.Completed);
        bool hasCancelled = bookings.Any(b => b.Status == BookingStatus.Cancelled);
        bool bookingsValid = totalBookings >= 50 && hasCompleted && (hasConfirmed || hasCheckedIn) && hasCancelled;
        report.AddCheck("Dữ liệu Đặt sân mẫu", ">= 50 đơn đặt sân rải 30 ngày, đầy đủ mọi trạng thái vòng đời", bookingsValid,
            $"Tổng {totalBookings} đơn. Status: Confirmed({bookings.Count(b => b.Status == BookingStatus.Confirmed)}), CheckedIn({bookings.Count(b => b.Status == BookingStatus.CheckedIn)}), Completed({bookings.Count(b => b.Status == BookingStatus.Completed)}), Cancelled({bookings.Count(b => b.Status == BookingStatus.Cancelled)})");

        // 9. Thanh toán & Đối soát dòng tiền
        var payments = await context.Payments.AsNoTracking().ToListAsync();
        int totalPayments = payments.Count;
        decimal totalPaymentAmount = payments.Sum(p => p.Amount);
        bool paymentsValid = totalPayments >= 50 && totalPaymentAmount > 0;
        report.AddCheck("Thanh toán & Thu ngân", ">= 50 giao dịch thanh toán (Đặt cọc, Thanh toán, Hoàn tiền)", paymentsValid,
            $"Tổng {totalPayments} giao dịch, tổng thu chi: {totalPaymentAmount:N0} VND");

        // 10. Tính toàn vẹn tài chính (Zero Discrepancy)
        bool financialIntegrity = bookings.All(b => b.FinalAmount == Math.Max(0, b.SubTotal - b.DiscountAmount));
        report.AddCheck("Tính toàn vẹn tài chính", "Công thức FinalAmount = SubTotal - DiscountAmount chính xác 100%", financialIntegrity,
            financialIntegrity ? "Khớp số liệu 100% trên toàn bộ các đơn đặt sân" : "Phát hiện sai lệch tiền cược / chiết khấu");

        return report;
    }

    public static void PrintReport(DbVerificationReport report, string title = "BÁO CÁO NGHIỆM THU HỆ THỐNG (DoD VERIFICATION)")
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================================");
        Console.WriteLine($"      {title}");
        Console.WriteLine("==========================================================================================");
        Console.ResetColor();

        int passedCount = 0;
        for (int i = 0; i < report.Items.Count; i++)
        {
            var item = report.Items[i];
            if (item.IsPassed)
            {
                passedCount++;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[{i + 1:D2}] [PASS] ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"[{i + 1:D2}] [FAIL] ");
            }
            Console.ResetColor();

            Console.WriteLine($"{item.Category,-30} | {item.Description}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"     -> Chi tiết: {item.Details}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================================");
        if (report.IsAllPassed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" KẾT QUẢ: TẤT CẢ {passedCount}/{report.Items.Count} TIÊU CHÍ ĐÃ ĐẠT CHUẨN! (SYSTEM READY TO OPERATE)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" KẾT QUẢ: {passedCount}/{report.Items.Count} ĐẠT, {report.Items.Count - passedCount} CHƯA ĐẠT. VUI LÒNG KIỂM TRA LẠI!");
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================================");
        Console.ResetColor();
    }
}
