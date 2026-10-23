using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Data;
using BadmintonManager.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class PriceRuleCoverageTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Theory]
    [InlineData(DayType.Weekday)]
    [InlineData(DayType.Weekend)]
    public async Task PriceRules_Should_Cover_0600_To_2400_Without_Gaps_Or_Overlaps(DayType dayType)
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        await DbSeeder.SeedAsync(context);

        // Act
        var rules = await context.PriceRules
            .Where(r => r.DayType == dayType && r.IsActive)
            .OrderBy(r => r.StartMinute)
            .ToListAsync();

        // Assert
        rules.Should().NotBeEmpty();

        // Must start at 06:00 (360 minutes)
        rules.First().StartMinute.Should().Be(360, "Khung giờ phải bắt đầu đúng từ 06:00");

        // Must end at 24:00 (1440 minutes)
        rules.Last().EndMinute.Should().Be(1440, "Khung giờ phải kết thúc đúng lúc 24:00");

        // Verify contiguous blocks
        for (int i = 0; i < rules.Count - 1; i++)
        {
            rules[i].EndMinute.Should().Be(rules[i + 1].StartMinute,
                $"Khung giờ thứ {i} và {i + 1} phải tiếp nối liên tục không có khoảng trống hoặc chồng lấn");
        }

        // Verify prices > 0
        rules.All(r => r.PricePerHour > 0).Should().BeTrue();
    }
}
