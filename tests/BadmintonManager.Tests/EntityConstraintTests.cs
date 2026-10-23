using System;
using System.Threading.Tasks;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class EntityConstraintTests
{
    private AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Manager_Without_BranchId_Should_Throw_BusinessException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var manager = new AppUser
        {
            Username = "invalid_manager",
            PasswordHash = "hash",
            FullName = "Invalid Manager",
            Role = UserRole.Manager,
            BranchId = null // Invalid: Manager must have BranchId
        };

        context.Users.Add(manager);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bắt buộc phải được gắn với một Cơ sở*");
    }

    [Fact]
    public async Task Staff_Without_BranchId_Should_Throw_BusinessException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var staff = new AppUser
        {
            Username = "invalid_staff",
            PasswordHash = "hash",
            FullName = "Invalid Staff",
            Role = UserRole.Staff,
            BranchId = null // Invalid: Staff must have BranchId
        };

        context.Users.Add(staff);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bắt buộc phải được gắn với một Cơ sở*");
    }

    [Fact]
    public async Task Admin_With_BranchId_Should_Throw_BusinessException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var admin = new AppUser
        {
            Username = "invalid_admin",
            PasswordHash = "hash",
            FullName = "Invalid Admin",
            Role = UserRole.Admin,
            BranchId = 1 // Invalid: Admin cannot have BranchId
        };

        context.Users.Add(admin);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không được gắn với Cơ sở*");
    }

    [Fact]
    public async Task Customer_With_BranchId_Should_Throw_BusinessException()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var customerUser = new AppUser
        {
            Username = "invalid_customer",
            PasswordHash = "hash",
            FullName = "Invalid Customer",
            Role = UserRole.Customer,
            BranchId = 1 // Invalid: Customer cannot have BranchId
        };

        context.Users.Add(customerUser);

        // Act & Assert
        Func<Task> act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không được gắn với Cơ sở*");
    }

    [Fact]
    public async Task Valid_Users_Should_Save_Successfully()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var admin = new AppUser
        {
            Username = "valid_admin",
            PasswordHash = "hash",
            FullName = "Valid Admin",
            Role = UserRole.Admin,
            BranchId = null
        };
        var manager = new AppUser
        {
            Username = "valid_manager",
            PasswordHash = "hash",
            FullName = "Valid Manager",
            Role = UserRole.Manager,
            BranchId = 1
        };

        context.Users.AddRange(admin, manager);

        // Act
        int result = await context.SaveChangesAsync();

        // Assert
        result.Should().Be(2);
    }
}
