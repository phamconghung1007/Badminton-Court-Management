using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed SystemSettings
        if (!await context.SystemSettings.AnyAsync())
        {
            var settings = new List<SystemSetting>
            {
                new() { Key = "OpenMinute", Value = "360" },
                new() { Key = "CloseMinute", Value = "1440" },
                new() { Key = "SlotMinutes", Value = "30" },
                new() { Key = "MinDurationMinutes", Value = "60" },
                new() { Key = "MaxDurationMinutes", Value = "240" },
                new() { Key = "DepositPercent", Value = "30" },
                new() { Key = "FreeCancelHours", Value = "24" },
                new() { Key = "PendingExpireMinutes", Value = "120" },
                new() { Key = "PointsPer", Value = "10000" },
                new() { Key = "FixedWeeksAhead", Value = "4" }
            };
            await context.SystemSettings.AddRangeAsync(settings);
            await context.SaveChangesAsync();
        }

        // 2. Seed MembershipTiers
        if (!await context.MembershipTiers.AnyAsync())
        {
            var tiers = new List<MembershipTier>
            {
                new() { Name = "Đồng", MinPoints = 0, DiscountPercent = 0.0m, SortOrder = 1 },
                new() { Name = "Bạc", MinPoints = 500, DiscountPercent = 5.0m, SortOrder = 2 },
                new() { Name = "Vàng", MinPoints = 1500, DiscountPercent = 10.0m, SortOrder = 3 }
            };
            await context.MembershipTiers.AddRangeAsync(tiers);
            await context.SaveChangesAsync();
        }

        // 3. Seed PriceRules (covering 06:00 to 24:00 for Weekday and Weekend)
        if (!await context.PriceRules.AnyAsync())
        {
            var priceRules = new List<PriceRule>
            {
                // Weekday
                new() { DayType = DayType.Weekday, StartMinute = 360, EndMinute = 960, PricePerHour = 60000m, IsActive = true },
                new() { DayType = DayType.Weekday, StartMinute = 960, EndMinute = 1320, PricePerHour = 100000m, IsActive = true },
                new() { DayType = DayType.Weekday, StartMinute = 1320, EndMinute = 1440, PricePerHour = 70000m, IsActive = true },

                // Weekend
                new() { DayType = DayType.Weekend, StartMinute = 360, EndMinute = 960, PricePerHour = 80000m, IsActive = true },
                new() { DayType = DayType.Weekend, StartMinute = 960, EndMinute = 1320, PricePerHour = 120000m, IsActive = true },
                new() { DayType = DayType.Weekend, StartMinute = 1320, EndMinute = 1440, PricePerHour = 90000m, IsActive = true }
            };
            await context.PriceRules.AddRangeAsync(priceRules);
            await context.SaveChangesAsync();
        }

        // 4. Seed Branches and Courts (3 branches, 6 courts each = 18 courts)
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                new()
                {
                    Name = "Cơ sở Cầu Giấy",
                    Address = "123 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội",
                    Phone = "02431234567",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60),
                    Courts = Enumerable.Range(1, 6).Select(i => new Court
                    {
                        Name = $"Sân {i}",
                        IsActive = true,
                        Note = $"Sân thảm tiêu chuẩn thi đấu số {i}"
                    }).ToList()
                },
                new()
                {
                    Name = "Cơ sở Đống Đa",
                    Address = "456 Xã Đàn, Nam Đồng, Đống Đa, Hà Nội",
                    Phone = "02432345678",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60),
                    Courts = Enumerable.Range(1, 6).Select(i => new Court
                    {
                        Name = $"Sân {i}",
                        IsActive = true,
                        Note = $"Sân sàn gỗ chống trượt cao cấp số {i}"
                    }).ToList()
                },
                new()
                {
                    Name = "Cơ sở Thanh Xuân",
                    Address = "789 Nguyễn Trãi, Thanh Xuân Bắc, Thanh Xuân, Hà Nội",
                    Phone = "02433456789",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-60),
                    Courts = Enumerable.Range(1, 6).Select(i => new Court
                    {
                        Name = $"Sân {i}",
                        IsActive = true,
                        Note = $"Sân thảm PVC giảm chấn số {i}"
                    }).ToList()
                }
            };

            await context.Branches.AddRangeAsync(branches);
            await context.SaveChangesAsync();
        }

        // 5. Seed Users & Customers
        if (!await context.Users.AnyAsync())
        {
            var branches = await context.Branches.OrderBy(b => b.Id).ToListAsync();
            var b1 = branches[0];
            var b2 = branches[1];
            var b3 = branches[2];

            var tiers = await context.MembershipTiers.OrderBy(t => t.SortOrder).ToListAsync();
            var tierDong = tiers[0];
            var tierBac = tiers[1];
            var tierVang = tiers[2];

            string defaultHash = BCrypt.Net.BCrypt.HashPassword("123456");

            // Admin
            var adminUser = new AppUser
            {
                Username = "admin",
                PasswordHash = defaultHash,
                Role = UserRole.Admin,
                FullName = "Hệ Thống Admin",
                BranchId = null,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-60)
            };
            await context.Users.AddAsync(adminUser);

            // Managers (1 per branch)
            var mgr1 = new AppUser { Username = "manager1", PasswordHash = defaultHash, Role = UserRole.Manager, FullName = "Nguyễn Văn Quản Lý (Cầu Giấy)", BranchId = b1.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            var mgr2 = new AppUser { Username = "manager2", PasswordHash = defaultHash, Role = UserRole.Manager, FullName = "Trần Thị Quản Lý (Đống Đa)", BranchId = b2.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            var mgr3 = new AppUser { Username = "manager3", PasswordHash = defaultHash, Role = UserRole.Manager, FullName = "Lê Văn Quản Lý (Thanh Xuân)", BranchId = b3.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            await context.Users.AddRangeAsync(mgr1, mgr2, mgr3);

            // Staff (2 per branch)
            var staff1_1 = new AppUser { Username = "staff1_1", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Lê Thu Staff (CG 1)", BranchId = b1.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            var staff1_2 = new AppUser { Username = "staff1_2", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Hoàng Nam Staff (CG 2)", BranchId = b1.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };

            var staff2_1 = new AppUser { Username = "staff2_1", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Vũ Mai Staff (ĐĐ 1)", BranchId = b2.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            var staff2_2 = new AppUser { Username = "staff2_2", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Đặng Hùng Staff (ĐĐ 2)", BranchId = b2.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };

            var staff3_1 = new AppUser { Username = "staff3_1", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Phan Trang Staff (TX 1)", BranchId = b3.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            var staff3_2 = new AppUser { Username = "staff3_2", PasswordHash = defaultHash, Role = UserRole.Staff, FullName = "Bùi Tiến Staff (TX 2)", BranchId = b3.Id, IsActive = true, CreatedAt = DateTime.Now.AddDays(-60) };
            await context.Users.AddRangeAsync(staff1_1, staff1_2, staff2_1, staff2_2, staff3_1, staff3_2);

            // Customer Users & Customers
            var custUser1 = new AppUser { Username = "customer1", PasswordHash = defaultHash, Role = UserRole.Customer, FullName = "Nguyễn Văn An", BranchId = null, IsActive = true, CreatedAt = DateTime.Now.AddDays(-50) };
            var custUser2 = new AppUser { Username = "customer2", PasswordHash = defaultHash, Role = UserRole.Customer, FullName = "Trần Thị Bình", BranchId = null, IsActive = true, CreatedAt = DateTime.Now.AddDays(-40) };
            var custUser3 = new AppUser { Username = "customer3", PasswordHash = defaultHash, Role = UserRole.Customer, FullName = "Lê Hoàng Cường", BranchId = null, IsActive = true, CreatedAt = DateTime.Now.AddDays(-30) };
            await context.Users.AddRangeAsync(custUser1, custUser2, custUser3);

            await context.SaveChangesAsync();

            // Link Customers
            var customer1 = new Customer
            {
                FullName = "Nguyễn Văn An",
                Phone = "0911000001",
                UserId = custUser1.Id,
                MembershipTierId = tierVang.Id,
                TotalPoints = 1600,
                MembershipCardNo = "MB-0001",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-50)
            };

            var customer2 = new Customer
            {
                FullName = "Trần Thị Bình",
                Phone = "0911000002",
                UserId = custUser2.Id,
                MembershipTierId = tierBac.Id,
                TotalPoints = 750,
                MembershipCardNo = "MB-0002",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-40)
            };

            var customer3 = new Customer
            {
                FullName = "Lê Hoàng Cường",
                Phone = "0911000003",
                UserId = custUser3.Id,
                MembershipTierId = tierDong.Id,
                TotalPoints = 120,
                MembershipCardNo = "MB-0003",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-30)
            };

            // Customer created at counter (no user account yet)
            var customer4 = new Customer
            {
                FullName = "Phạm Minh Dũng",
                Phone = "0911000004",
                UserId = null,
                MembershipTierId = tierDong.Id,
                TotalPoints = 40,
                MembershipCardNo = null,
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-15)
            };

            await context.Customers.AddRangeAsync(customer1, customer2, customer3, customer4);
            await context.SaveChangesAsync();

            // Initial point transactions for customers
            await context.PointTransactions.AddRangeAsync(
                new PointTransaction { CustomerId = customer1.Id, Points = 1600, Reason = "Tích điểm lịch sử thành viên Vàng", CreatedAt = DateTime.Now.AddDays(-5) },
                new PointTransaction { CustomerId = customer2.Id, Points = 750, Reason = "Tích điểm lịch sử thành viên Bạc", CreatedAt = DateTime.Now.AddDays(-10) },
                new PointTransaction { CustomerId = customer3.Id, Points = 120, Reason = "Tích điểm lịch sử thành viên Đồng", CreatedAt = DateTime.Now.AddDays(-15) },
                new PointTransaction { CustomerId = customer4.Id, Points = 40, Reason = "Tích điểm khách lẻ tại quầy", CreatedAt = DateTime.Now.AddDays(-15) }
            );
            await context.SaveChangesAsync();
        }

        // 6. Seed ~50 sample Bookings across past 30 days
        if (!await context.Bookings.AnyAsync())
        {
            var courts = await context.Courts.Include(c => c.Branch).ToListAsync();
            var customers = await context.Customers.ToListAsync();
            var staff = await context.Users.Where(u => u.Role == UserRole.Staff).ToListAsync();
            var adminUser = await context.Users.FirstAsync(u => u.Role == UserRole.Admin);

            var random = new Random(42);
            var bookings = new List<Booking>();
            var payments = new List<Payment>();
            int codeCounter = 1;

            DateTime baseDate = DateTime.Today.AddDays(-30);

            for (int dayOffset = 0; dayOffset <= 30; dayOffset++)
            {
                DateTime bookingDate = baseDate.AddDays(dayOffset);
                bool isWeekend = bookingDate.DayOfWeek == DayOfWeek.Saturday || bookingDate.DayOfWeek == DayOfWeek.Sunday;

                // Pick 1 to 3 bookings per day
                int bookingsToday = random.Next(1, 4);

                for (int b = 0; b < bookingsToday; b++)
                {
                    var court = courts[random.Next(courts.Count)];
                    var staffUser = staff.FirstOrDefault(s => s.BranchId == court.BranchId) ?? staff[0];

                    // Slots: start hour between 8 and 20
                    int startHour = random.Next(8, 20);
                    int startMinute = random.Next(0, 2) * 30;
                    int durationMinutes = random.Next(2, 5) * 30; // 60, 90, 120 minutes

                    DateTime startTime = bookingDate.Date.AddHours(startHour).AddMinutes(startMinute);
                    DateTime endTime = startTime.AddMinutes(durationMinutes);

                    // Check for overlap in current batch
                    bool overlaps = bookings.Any(x => x.CourtId == court.Id && x.StartTime < endTime && startTime < x.EndTime);
                    if (overlaps) continue;

                    bool isGuest = random.Next(0, 4) == 0;
                    Customer? customer = isGuest ? null : customers[random.Next(customers.Count)];

                    decimal rate = isWeekend ? (startHour >= 16 ? 120000m : 80000m) : (startHour >= 16 ? 100000m : 60000m);
                    decimal subTotal = rate * (durationMinutes / 60m);
                    decimal discountPercent = customer?.MembershipTierId switch
                    {
                        3 => 10.0m, // Vàng
                        2 => 5.0m,  // Bạc
                        _ => 0.0m
                    };
                    decimal discountAmount = Math.Round(subTotal * (discountPercent / 100m));
                    decimal finalAmount = subTotal - discountAmount;
                    decimal depositAmount = Math.Round(finalAmount * 0.3m, 0);

                    // Status based on whether bookingDate is past or future
                    BookingStatus status;
                    if (bookingDate < DateTime.Today)
                    {
                        int r = random.Next(10);
                        if (r < 7) status = BookingStatus.Completed;
                        else if (r < 9) status = BookingStatus.CheckedIn;
                        else status = BookingStatus.Cancelled;
                    }
                    else if (bookingDate == DateTime.Today)
                    {
                        status = BookingStatus.Confirmed;
                    }
                    else
                    {
                        status = random.Next(2) == 0 ? BookingStatus.Confirmed : BookingStatus.Pending;
                    }

                    var booking = new Booking
                    {
                        Code = $"BK{bookingDate:yyyyMMdd}-{codeCounter:D3}",
                        CourtId = court.Id,
                        CustomerId = customer?.Id,
                        GuestName = isGuest ? $"Khách Vãng Lai {codeCounter}" : null,
                        GuestPhone = isGuest ? $"0987{codeCounter:D6}" : null,
                        BookingType = isGuest ? BookingType.WalkIn : BookingType.Single,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = status,
                        SubTotal = subTotal,
                        DiscountPercent = discountPercent,
                        DiscountAmount = discountAmount,
                        FinalAmount = finalAmount,
                        DepositAmount = depositAmount,
                        CreatedByUserId = staffUser.Id,
                        CreatedAt = startTime.AddDays(-2),
                        ConfirmedByUserId = status != BookingStatus.Pending ? staffUser.Id : null,
                        CheckedInAt = (status == BookingStatus.CheckedIn || status == BookingStatus.Completed) ? startTime.AddMinutes(random.Next(-10, 5)) : null,
                        CancelledAt = status == BookingStatus.Cancelled ? startTime.AddHours(-12) : null,
                        CancelledByUserId = status == BookingStatus.Cancelled ? staffUser.Id : null,
                        CancelReason = status == BookingStatus.Cancelled ? "Khách báo bận đột xuất" : null
                    };

                    bookings.Add(booking);

                    // If Confirmed/CheckedIn/Completed, add deposit payment
                    if (status != BookingStatus.Pending && status != BookingStatus.Cancelled)
                    {
                        payments.Add(new Payment
                        {
                            Booking = booking,
                            Amount = depositAmount,
                            Method = random.Next(2) == 0 ? PaymentMethod.Cash : PaymentMethod.BankTransfer,
                            Type = PaymentType.Deposit,
                            ReferenceNo = $"DEP-{booking.Code}",
                            PaidAt = booking.CreatedAt.AddHours(1),
                            ReceivedByUserId = staffUser.Id,
                            Note = "Đặt cọc sân 30%"
                        });
                    }

                    // If Completed, add final payment
                    if (status == BookingStatus.Completed)
                    {
                        decimal remaining = finalAmount - depositAmount;
                        payments.Add(new Payment
                        {
                            Booking = booking,
                            Amount = remaining,
                            Method = PaymentMethod.Cash,
                            Type = PaymentType.Final,
                            ReferenceNo = $"FIN-{booking.Code}",
                            PaidAt = endTime,
                            ReceivedByUserId = staffUser.Id,
                            Note = "Thanh toán phần còn lại sau khi chơi"
                        });
                    }

                    // If Cancelled with refund
                    if (status == BookingStatus.Cancelled && random.Next(2) == 0)
                    {
                        payments.Add(new Payment
                        {
                            Booking = booking,
                            Amount = depositAmount,
                            Method = PaymentMethod.BankTransfer,
                            Type = PaymentType.Refund,
                            ReferenceNo = $"REF-{booking.Code}",
                            PaidAt = booking.CancelledAt ?? startTime,
                            ReceivedByUserId = staffUser.Id,
                            Note = "Hoàn cọc hủy trước 24h"
                        });
                    }

                    codeCounter++;
                    if (bookings.Count >= 55) break;
                }

                if (bookings.Count >= 55) break;
            }

            await context.Bookings.AddRangeAsync(bookings);
            await context.Payments.AddRangeAsync(payments);
            await context.SaveChangesAsync();
        }
    }
}
