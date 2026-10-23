using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class DbSeederTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_Should_Seed_All_Required_Day1_Data()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Act
        await DbSeeder.SeedAsync(context);

        // Assert 1: Branches and Courts (3 branches, 6 courts each = 18 courts)
        var branches = await context.Branches.Include(b => b.Courts).ToListAsync();
        branches.Should().HaveCount(3);
        branches.All(b => b.Courts.Count == 6).Should().BeTrue();
        int totalCourts = branches.Sum(b => b.Courts.Count);
        totalCourts.Should().Be(18);

        // Assert 2: SystemSettings (10 settings)
        var settings = await context.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
        settings.Should().HaveCount(10);
        settings[Constants.Settings.OpenMinute].Should().Be("360");
        settings[Constants.Settings.CloseMinute].Should().Be("1440");
        settings[Constants.Settings.SlotMinutes].Should().Be("30");
        settings[Constants.Settings.DepositPercent].Should().Be("30");
        settings[Constants.Settings.FreeCancelHours].Should().Be("24");

        // Assert 3: MembershipTiers (3 tiers)
        var tiers = await context.MembershipTiers.OrderBy(t => t.SortOrder).ToListAsync();
        tiers.Should().HaveCount(3);
        tiers[0].Name.Should().Be(Constants.MembershipTiers.Bronze);
        tiers[0].DiscountPercent.Should().Be(0.0m);
        tiers[1].Name.Should().Be(Constants.MembershipTiers.Silver);
        tiers[1].DiscountPercent.Should().Be(5.0m);
        tiers[2].Name.Should().Be(Constants.MembershipTiers.Gold);
        tiers[2].DiscountPercent.Should().Be(10.0m);

        // Assert 4: PriceRules (6 rules)
        var priceRules = await context.PriceRules.ToListAsync();
        priceRules.Should().HaveCount(6);

        // Assert 5: Users (1 Admin, 3 Managers, 6 Staff, 3 Customer users)
        var users = await context.Users.ToListAsync();
        users.Should().HaveCount(13);
        users.Count(u => u.Role == UserRole.Admin).Should().Be(1);
        users.Count(u => u.Role == UserRole.Manager).Should().Be(3);
        users.Count(u => u.Role == UserRole.Staff).Should().Be(6);
        users.Count(u => u.Role == UserRole.Customer).Should().Be(3);

        // Password hash check
        foreach (var user in users)
        {
            BCrypt.Net.BCrypt.Verify("123456", user.PasswordHash).Should().BeTrue();
        }

        // Assert 6: Customers
        var customers = await context.Customers.ToListAsync();
        customers.Should().HaveCount(4);

        // Assert 7: Bookings (>= 50 sample bookings)
        var bookings = await context.Bookings.ToListAsync();
        bookings.Count.Should().BeGreaterOrEqualTo(50);
    }

    [Fact]
    public async Task SeedAsync_Should_Be_Idempotent()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        // Act: Run twice
        await DbSeeder.SeedAsync(context);
        await DbSeeder.SeedAsync(context);

        // Assert: Counts remain identical
        var branchCount = await context.Branches.CountAsync();
        var courtCount = await context.Courts.CountAsync();
        var userCount = await context.Users.CountAsync();
        var bookingCount = await context.Bookings.CountAsync();

        branchCount.Should().Be(3);
        courtCount.Should().Be(18);
        userCount.Should().Be(13);
        bookingCount.Should().BeGreaterOrEqualTo(50);
    }
}
