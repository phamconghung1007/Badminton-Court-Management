using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class PricingAndTierServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser, PricingService pricingService, MembershipService membershipService) CreateTestSetup(
        UserRole role = UserRole.Admin)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        currentUser.SetUser(1, "admin", "Admin User", role, null);

        var settingService = new SettingService(factory);
        var pricingService = new PricingService(factory, settingService, currentUser);
        var membershipService = new MembershipService(factory, currentUser);

        return (factory, currentUser, pricingService, membershipService);
    }

    [Fact]
    public async Task Admin_CanSaveValidPriceRule_Successfully()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 360, // 06:00
            EndMinute = 720,   // 12:00
            PricePerHour = 75000m,
            IsActive = true
        };

        // Act
        var result = await pricingService.SavePriceRuleAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.PricePerHour.Should().Be(75000m);
        result.TimeRange.Should().Be("06:00 - 12:00");

        await using var db = await factory.CreateDbContextAsync();
        var saved = await db.PriceRules.FirstOrDefaultAsync(r => r.PricePerHour == 75000m);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task NonAdmin_CannotSavePriceRule_ThrowsForbiddenException()
    {
        // Arrange
        var (_, _, pricingService, _) = CreateTestSetup(UserRole.Manager);
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 75000m
        };

        // Act & Assert
        var act = async () => await pricingService.SavePriceRuleAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Chỉ Quản trị viên (Admin) mới có quyền cấu hình bảng giá*");
    }

    [Fact]
    public async Task PriceRule_StartOrEndNotMultipleOf30_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 365, // Not multiple of 30
            EndMinute = 720,
            PricePerHour = 80000m
        };

        // Act & Assert
        var act = async () => await pricingService.SavePriceRuleAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*phải là bội số của 30 phút*");
    }

    [Fact]
    public async Task PriceRule_EndMinuteBeforeStartMinute_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 720,
            EndMinute = 360, // End before Start
            PricePerHour = 80000m
        };

        // Act & Assert
        var act = async () => await pricingService.SavePriceRuleAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Giờ kết thúc phải lớn hơn giờ bắt đầu*");
    }

    [Fact]
    public async Task PriceRule_PriceZeroOrNegative_ThrowsBusinessException()
    {
        // Arrange
        var (_, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 360,
            EndMinute = 720,
            PricePerHour = 0m
        };

        // Act & Assert
        var act = async () => await pricingService.SavePriceRuleAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Đơn giá theo giờ phải lớn hơn 0 VNĐ*");
    }

    [Fact]
    public async Task PriceRule_BR10_OverlapWithExistingRule_ThrowsBusinessException()
    {
        // Arrange: Existing rule is Weekday 06:00 - 16:00 (360 - 960)
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PriceRules.Add(new PriceRule
            {
                DayType = DayType.Weekday,
                StartMinute = 360,
                EndMinute = 960,
                PricePerHour = 60000m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // New rule: Weekday 10:00 - 18:00 (600 - 1080) -> overlaps!
        var request = new SavePriceRuleRequest
        {
            DayType = DayType.Weekday,
            StartMinute = 600,
            EndMinute = 1080,
            PricePerHour = 100000m,
            IsActive = true
        };

        // Act & Assert
        var act = async () => await pricingService.SavePriceRuleAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bị chồng lấn với khung giờ đang áp dụng*BR-10 quy định các khung giờ của cùng loại ngày không được chồng nhau*");
    }

    [Fact]
    public async Task ValidateCoverage_CompleteRules_ReturnsValid()
    {
        // Arrange: Seed complete coverage 06:00 to 24:00
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PriceRules.AddRange(
                new PriceRule { DayType = DayType.Weekday, StartMinute = 360, EndMinute = 960, PricePerHour = 60000m, IsActive = true },
                new PriceRule { DayType = DayType.Weekday, StartMinute = 960, EndMinute = 1320, PricePerHour = 100000m, IsActive = true },
                new PriceRule { DayType = DayType.Weekday, StartMinute = 1320, EndMinute = 1440, PricePerHour = 70000m, IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var result = await pricingService.ValidateCoverageAsync(DayType.Weekday);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Summary.Should().Contain("Đạt chuẩn BR-10");
    }

    [Fact]
    public async Task ValidateCoverage_MissingStart_ReturnsInvalid()
    {
        // Arrange: starts at 07:00 (420) instead of 06:00 (360)
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PriceRules.Add(new PriceRule
            {
                DayType = DayType.Weekday,
                StartMinute = 420,
                EndMinute = 1440,
                PricePerHour = 80000m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Act
        var result = await pricingService.ValidateCoverageAsync(DayType.Weekday);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Thiếu khung giờ mở cửa từ 06:00"));
    }

    [Fact]
    public async Task ValidateCoverage_MissingEnd_ReturnsInvalid()
    {
        // Arrange: ends at 22:00 (1320) instead of 24:00 (1440)
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PriceRules.Add(new PriceRule
            {
                DayType = DayType.Weekday,
                StartMinute = 360,
                EndMinute = 1320,
                PricePerHour = 80000m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Act
        var result = await pricingService.ValidateCoverageAsync(DayType.Weekday);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Thiếu khung giờ đóng cửa"));
    }

    [Fact]
    public async Task ValidateCoverage_HasGap_ReturnsInvalid()
    {
        // Arrange: Gap between 12:00 (720) and 13:00 (780)
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.PriceRules.AddRange(
                new PriceRule { DayType = DayType.Weekday, StartMinute = 360, EndMinute = 720, PricePerHour = 60000m, IsActive = true },
                new PriceRule { DayType = DayType.Weekday, StartMinute = 780, EndMinute = 1440, PricePerHour = 90000m, IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var result = await pricingService.ValidateCoverageAsync(DayType.Weekday);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Khoảng trống giữa 12:00 và 13:00"));
    }

    [Fact]
    public async Task Admin_CanCreateAndEditMembershipTier()
    {
        // Arrange
        var (factory, _, _, membershipService) = CreateTestSetup(UserRole.Admin);

        var createReq = new SaveMembershipTierRequest
        {
            Name = "Bạch Kim",
            MinPoints = 3000,
            DiscountPercent = 15.0m,
            SortOrder = 4
        };

        // Act - Create
        var created = await membershipService.SaveTierAsync(createReq);
        created.Should().NotBeNull();
        created.Name.Should().Be("Bạch Kim");
        created.DiscountPercent.Should().Be(15.0m);

        // Act - Edit
        var editReq = new SaveMembershipTierRequest
        {
            Id = created.Id,
            Name = "Bạch Kim VIP",
            MinPoints = 3500,
            DiscountPercent = 20.0m,
            SortOrder = 4
        };
        var updated = await membershipService.SaveTierAsync(editReq);

        // Assert
        updated.Name.Should().Be("Bạch Kim VIP");
        updated.DiscountPercent.Should().Be(20.0m);
        updated.MinPoints.Should().Be(3500);
    }

    [Fact]
    public async Task NonAdmin_CannotManageTier_ThrowsForbiddenException()
    {
        // Arrange
        var (_, _, _, membershipService) = CreateTestSetup(UserRole.Staff);
        var req = new SaveMembershipTierRequest { Name = "Kim Cương", MinPoints = 5000, DiscountPercent = 25m, SortOrder = 5 };

        // Act & Assert
        var act = async () => await membershipService.SaveTierAsync(req);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Chỉ Quản trị viên (Admin) mới có quyền quản lý hạng hội viên*");
    }

    [Fact]
    public async Task DeleteTier_WithAssociatedCustomers_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, _, membershipService) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            var tier = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0m, SortOrder = 1 };
            db.MembershipTiers.Add(tier);

            var cust = new Customer { Id = 1, FullName = "Khách hàng", Phone = "0987654321", MembershipTierId = 1 };
            db.Customers.Add(cust);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        var act = async () => await membershipService.DeleteTierAsync(1);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Không thể xoá hạng 'Đồng' vì hiện đang có 1 khách hàng đang thuộc hạng này*");
    }

    [Fact]
    public async Task DeleteTier_WithoutCustomers_Succeeds()
    {
        // Arrange
        var (factory, _, _, membershipService) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            var tier = new MembershipTier { Id = 2, Name = "Hạng thử nghiệm", MinPoints = 9999, DiscountPercent = 0m, SortOrder = 99 };
            db.MembershipTiers.Add(tier);
            await db.SaveChangesAsync();
        }

        // Act
        await membershipService.DeleteTierAsync(2);

        // Assert
        await using (var db = await factory.CreateDbContextAsync())
        {
            var deleted = await db.MembershipTiers.FindAsync(2);
            deleted.Should().BeNull();
        }
    }

    [Fact]
    public async Task CalculatePrice_SingleSlot_Weekday_CalculatesCorrectly()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        // Monday 08:00 - 10:00 (Weekday rule: 360-960 is 60,000 VND/h)
        DateTime monday = new DateTime(2026, 11, 2, 8, 0, 0); // Monday
        DateTime endMonday = new DateTime(2026, 11, 2, 10, 0, 0);

        // Act
        var result = await pricingService.CalculatePriceAsync(1, monday, endMonday, customerId: null);

        // Assert
        result.SubTotal.Should().Be(120000m);
        result.DiscountAmount.Should().Be(0m);
        result.FinalAmount.Should().Be(120000m);
        result.DepositAmount.Should().Be(36000m); // 30% of 120,000 = 36,000
        result.SlotDetails.Should().HaveCount(4); // 4 half-hour slots
    }

    [Fact]
    public async Task CalculatePrice_SpanningTwoPriceRules_BR07_CalculatesCorrectly()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        // Monday 15:00 - 17:00 (Crosses 16:00 = 960 min boundary: 60k/h before, 100k/h after)
        // 15:00 - 16:00: 2 slots * 30k = 60,000
        // 16:00 - 17:00: 2 slots * 50k = 100,000
        // Expected SubTotal = 160,000
        DateTime mondayStart = new DateTime(2026, 11, 2, 15, 0, 0);
        DateTime mondayEnd = new DateTime(2026, 11, 2, 17, 0, 0);

        // Act
        var result = await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: null);

        // Assert
        result.SubTotal.Should().Be(160000m);
        result.FinalAmount.Should().Be(160000m);
        result.DepositAmount.Should().Be(48000m); // 30% of 160,000
        result.SlotDetails.Should().HaveCount(4);
        result.SlotDetails[0].HourlyRate.Should().Be(60000m);
        result.SlotDetails[1].HourlyRate.Should().Be(60000m);
        result.SlotDetails[2].HourlyRate.Should().Be(100000m);
        result.SlotDetails[3].HourlyRate.Should().Be(100000m);
    }

    [Fact]
    public async Task CalculatePrice_Weekend_BR08_UsesWeekendRates()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        // Saturday 08:00 - 10:00 (Weekend rule 360-960 is 80,000 VND/h)
        DateTime saturdayStart = new DateTime(2026, 11, 7, 8, 0, 0); // Saturday
        DateTime saturdayEnd = new DateTime(2026, 11, 7, 10, 0, 0);

        // Act
        var result = await pricingService.CalculatePriceAsync(1, saturdayStart, saturdayEnd, customerId: null);

        // Assert
        result.SubTotal.Should().Be(160000m); // 2 hours * 80,000
        result.DepositAmount.Should().Be(48000m);
        result.SlotDetails.All(s => s.DayType == DayType.Weekend).Should().BeTrue();
    }

    [Fact]
    public async Task CalculatePrice_MembershipTiers_BR09_AppliesCorrectDiscount()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        // customer1 is Gold (10%), customer2 is Silver (5%), customer3 is Bronze (0%)
        DateTime mondayStart = new DateTime(2026, 11, 2, 8, 0, 0);
        DateTime mondayEnd = new DateTime(2026, 11, 2, 10, 0, 0); // SubTotal = 120,000

        // Act - Gold
        var goldResult = await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: 1);
        // Act - Silver
        var silverResult = await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: 2);
        // Act - Bronze
        var bronzeResult = await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: 3);

        // Assert
        goldResult.DiscountPercent.Should().Be(10.0m);
        goldResult.DiscountAmount.Should().Be(12000m);
        goldResult.FinalAmount.Should().Be(108000m);
        goldResult.DepositAmount.Should().Be(32000m); // 108,000 * 30% = 32,400 -> rounded to nearest 1,000 = 32,000

        silverResult.DiscountPercent.Should().Be(5.0m);
        silverResult.DiscountAmount.Should().Be(6000m);
        silverResult.FinalAmount.Should().Be(114000m);
        silverResult.DepositAmount.Should().Be(34000m); // 114,000 * 30% = 34,200 -> rounded to nearest 1,000 = 34,000

        bronzeResult.DiscountPercent.Should().Be(0.0m);
        bronzeResult.DiscountAmount.Should().Be(0m);
        bronzeResult.FinalAmount.Should().Be(120000m);
        bronzeResult.DepositAmount.Should().Be(36000m);
    }

    [Fact]
    public async Task CalculatePrice_DepositRounding_BR11_RoundsToNearest1000VND()
    {
        // Arrange
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);
        await using (var db = await factory.CreateDbContextAsync())
        {
            // Seed special custom rule with hourly rate 103,333 VND
            db.PriceRules.Add(new PriceRule
            {
                DayType = DayType.Weekday,
                StartMinute = 360,
                EndMinute = 1440,
                PricePerHour = 103333m,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        DateTime mondayStart = new DateTime(2026, 11, 2, 8, 0, 0);
        DateTime mondayEnd = new DateTime(2026, 11, 2, 9, 0, 0); // 1 hour -> SubTotal = 103,333

        // Act
        var result = await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: null);

        // Assert
        // raw deposit = 103,333 * 30% = 30,999.9 -> rounded to nearest 1,000 = 31,000
        result.DepositAmount.Should().Be(31000m);
        (result.DepositAmount % 1000m).Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePrice_MissingPriceRule_ThrowsBusinessException()
    {
        // Arrange: no rules seeded
        var (factory, _, pricingService, _) = CreateTestSetup(UserRole.Admin);

        DateTime mondayStart = new DateTime(2026, 11, 2, 8, 0, 0);
        DateTime mondayEnd = new DateTime(2026, 11, 2, 10, 0, 0);

        // Act & Assert
        var act = async () => await pricingService.CalculatePriceAsync(1, mondayStart, mondayEnd, customerId: null);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*Chưa cấu hình bảng giá cho khung giờ*");
    }
}
