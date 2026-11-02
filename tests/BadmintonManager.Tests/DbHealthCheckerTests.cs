using System;
using System.Threading.Tasks;
using BadmintonManager.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class DbHealthCheckerTests
{
    private readonly DbContextOptions<AppDbContext> _options;

    public DbHealthCheckerTests()
    {
        var dbName = $"Badminton_HealthTest_{Guid.NewGuid():N}";
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Fact]
    public async Task VerifyDay1DoD_WhenSeeded_ShouldPassAllChecks()
    {
        // Arrange
        await using var db = new AppDbContext(_options);
        await DbSeeder.SeedAsync(db);

        // Act
        var report = await DbHealthChecker.VerifyDay1DoDAsync(db);

        // Assert
        report.Should().NotBeNull();
        var failedItems = report.Items.Where(i => !i.IsPassed).ToList();
        failedItems.Should().BeEmpty(string.Join("; ", failedItems.Select(f => $"{f.Category}: {f.Details}")));
        report.IsAllPassed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyDay10FinalDoD_WhenSeeded_ShouldPassAllChecks()
    {
        // Arrange
        await using var db = new AppDbContext(_options);
        await DbSeeder.SeedAsync(db);

        // Act
        var report = await DbHealthChecker.VerifyDay10FinalDoDAsync(db);

        // Assert
        report.Should().NotBeNull();
        var failedItems = report.Items.Where(i => !i.IsPassed).ToList();
        failedItems.Should().BeEmpty(string.Join("; ", failedItems.Select(f => $"{f.Category}: {f.Details}")));
        report.IsAllPassed.Should().BeTrue();

        // PrintReport should execute without exceptions
        var actPrint = () => DbHealthChecker.PrintReport(report, "TEST TITLE");
        actPrint.Should().NotThrow();
    }

    [Fact]
    public async Task VerifyDay10FinalDoD_WhenEmptyDb_ShouldFailChecks()
    {
        // Arrange
        var emptyDbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Badminton_Empty_{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(emptyDbOptions);

        // Act
        var report = await DbHealthChecker.VerifyDay10FinalDoDAsync(db);

        // Assert
        report.Should().NotBeNull();
        report.IsAllPassed.Should().BeFalse();
        report.Items.Should().Contain(i => !i.IsPassed);
    }
}
