using System;
using System.Linq;
using System.Threading;
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

public class TestDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public AppDbContext CreateDbContext() => new AppDbContext(_options);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new AppDbContext(_options));
}

public class AuthServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser) CreateTestSetup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        return (factory, currentUser);
    }

    [Fact]
    public async Task LoginAsync_Admin_ShouldSucceed_WithCorrectRoleAndMetadata()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act (Seeded password is "123456")
        var response = await authService.LoginAsync(new LoginRequest("admin", "123456"));

        // Assert
        response.Should().NotBeNull();
        response.Role.Should().Be(UserRole.Admin);
        response.Username.Should().Be("admin");
        response.FullName.Should().Be("Hệ Thống Admin");
        response.BranchId.Should().BeNull();

        currentUser.IsAuthenticated.Should().BeTrue();
        currentUser.Role.Should().Be(UserRole.Admin);
        currentUser.Username.Should().Be("admin");
    }

    [Fact]
    public async Task LoginAsync_Manager_ShouldSucceed_WithBranchAffiliation()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act
        var response = await authService.LoginAsync(new LoginRequest("manager1", "123456"));

        // Assert
        response.Should().NotBeNull();
        response.Role.Should().Be(UserRole.Manager);
        response.BranchId.Should().Be(1);
        response.BranchName.Should().Be("Cơ sở Cầu Giấy");

        currentUser.BranchId.Should().Be(1);
        currentUser.BranchName.Should().Be("Cơ sở Cầu Giấy");
    }

    [Fact]
    public async Task LoginAsync_Staff_ShouldSucceed_WithBranchAffiliation()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act (staff1_1 is seeded)
        var response = await authService.LoginAsync(new LoginRequest("staff1_1", "123456"));

        // Assert
        response.Should().NotBeNull();
        response.Role.Should().Be(UserRole.Staff);
        response.BranchId.Should().Be(1);
        currentUser.Role.Should().Be(UserRole.Staff);
    }

    [Fact]
    public async Task LoginAsync_Customer_ShouldSucceed_WithTierInformation()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act (customer1 is seeded with Tier Vàng)
        var response = await authService.LoginAsync(new LoginRequest("customer1", "123456"));

        // Assert
        response.Should().NotBeNull();
        response.Role.Should().Be(UserRole.Customer);
        response.CustomerId.Should().NotBeNull();
        response.TierName.Should().Be("Vàng");

        currentUser.CustomerId.Should().NotBeNull();
        currentUser.TierName.Should().Be("Vàng");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowExactGenericErrorMessage()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act
        var act = async () => await authService.LoginAsync(new LoginRequest("admin", "WrongPassword999!"));

        // Assert (DoD Section 8.5 requirement)
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Sai tên đăng nhập hoặc mật khẩu.");
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ShouldThrowExactGenericErrorMessage()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);

        // Act
        var act = async () => await authService.LoginAsync(new LoginRequest("user_does_not_exist", "SomePassword123"));

        // Assert: must NOT disclose if user exists
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Sai tên đăng nhập hoặc mật khẩu.");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ShouldThrowAccountLockedException()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
            var staff = await db.Users.FirstAsync(u => u.Username == "staff1_1");
            staff.IsActive = false;
            await db.SaveChangesAsync();
        }

        var authService = new AuthService(factory, currentUser);

        // Act
        var act = async () => await authService.LoginAsync(new LoginRequest("staff1_1", "123456"));

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
    }

    [Fact]
    public async Task RegisterCustomerAsync_NewPhone_ShouldCreateCustomerAndUser()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);
        var registerDto = new RegisterCustomerRequest(
            "nguyenvana",
            "Password@123",
            "Nguyễn Văn A",
            "0988776655"
        );

        // Act
        var response = await authService.RegisterCustomerAsync(registerDto);

        // Assert
        response.Should().NotBeNull();
        response.FullName.Should().Be("Nguyễn Văn A");
        response.Phone.Should().Be("0988776655");

        await using (var db = await factory.CreateDbContextAsync())
        {
            var user = await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Username == "nguyenvana");
            user.Should().NotBeNull();
            user!.Customer.Should().NotBeNull();
            user.Customer!.Phone.Should().Be("0988776655");
            user.Customer.MembershipTierId.Should().Be(1); // Đồng
        }
    }

    [Fact]
    public async Task RegisterCustomerAsync_ExistingCounterPhone_ShouldLinkCustomerRecord()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);
        // customer4 has phone "0911000004" and UserId == null
        var registerDto = new RegisterCustomerRequest(
            "dungpham",
            "Password@123",
            "Phạm Minh Dũng",
            "0911000004"
        );

        // Act
        var response = await authService.RegisterCustomerAsync(registerDto);

        // Assert
        response.Should().NotBeNull();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var user = await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Username == "dungpham");
            user.Should().NotBeNull();
            user!.Customer.Should().NotBeNull();
            user.Customer!.Phone.Should().Be("0911000004");
            user.Customer.UserId.Should().Be(user.Id);
        }
    }

    [Fact]
    public async Task RegisterCustomerAsync_DuplicateUsername_ShouldThrowBusinessException()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
        }

        var authService = new AuthService(factory, currentUser);
        var registerDto = new RegisterCustomerRequest(
            "admin", // existing username
            "Password@123",
            "Fake Admin",
            "0911223344"
        );

        // Act
        var act = async () => await authService.RegisterCustomerAsync(registerDto);

        // Assert
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("Tên đăng nhập đã tồn tại trong hệ thống.");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCredentials_ShouldUpdatePasswordHash()
    {
        // Arrange
        var (factory, currentUser) = CreateTestSetup();
        int adminId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);
            var admin = await db.Users.FirstAsync(u => u.Username == "admin");
            adminId = admin.Id;
        }

        var authService = new AuthService(factory, currentUser);

        // Act: change password from "123456" to "NewAdmin@456"
        await authService.ChangePasswordAsync(adminId, new ChangePasswordRequest("123456", "NewAdmin@456"));

        // Assert: login with old password fails
        var failAct = async () => await authService.LoginAsync(new LoginRequest("admin", "123456"));
        await failAct.Should().ThrowAsync<BusinessException>();

        // Assert: login with new password succeeds
        var successResponse = await authService.LoginAsync(new LoginRequest("admin", "NewAdmin@456"));
        successResponse.Username.Should().Be("admin");
    }
}
