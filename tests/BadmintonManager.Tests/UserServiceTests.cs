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

public class UserServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser, UserService service) CreateTestSetup(
        UserRole role = UserRole.Admin,
        int? branchId = null,
        int currentUserId = 1)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        currentUser.SetUser(currentUserId, "user_" + currentUserId, "User " + currentUserId, role, branchId);

        var service = new UserService(factory, currentUser);
        return (factory, currentUser, service);
    }

    private async Task SeedUsersAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0241", IsActive = true };
        var b2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Đống Đa", Phone = "0242", IsActive = true };
        db.Branches.AddRange(b1, b2);

        var admin = new AppUser
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Admin Tổng",
            Role = UserRole.Admin,
            BranchId = null,
            IsActive = true
        };

        var mgr1 = new AppUser
        {
            Id = 2,
            Username = "manager1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Quản lý 1",
            Role = UserRole.Manager,
            BranchId = 1,
            IsActive = true
        };

        var mgr2 = new AppUser
        {
            Id = 3,
            Username = "manager2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Quản lý 2",
            Role = UserRole.Manager,
            BranchId = 2,
            IsActive = true
        };

        var staff1 = new AppUser
        {
            Id = 4,
            Username = "staff1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Nhân viên 1",
            Role = UserRole.Staff,
            BranchId = 1,
            IsActive = true
        };

        var staff2 = new AppUser
        {
            Id = 5,
            Username = "staff2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Nhân viên 2",
            Role = UserRole.Staff,
            BranchId = 2,
            IsActive = true
        };

        db.Users.AddRange(admin, mgr1, mgr2, staff1, staff2);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_CanGetAllUsers_WithoutRestrictions()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        // Act
        var users = await service.GetUsersAsync();

        // Assert
        users.Should().HaveCount(5);
    }

    [Fact]
    public async Task Manager_GetUsers_ReturnsOnlyStaffOfOwnBranch()
    {
        // Arrange: Manager 1 of Branch 1
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        // Act
        var users = await service.GetUsersAsync();

        // Assert: should only see staff1
        users.Should().HaveCount(1);
        users.First().Username.Should().Be("staff1");
        users.First().BranchId.Should().Be(1);
    }

    [Fact]
    public async Task Manager_CannotAccessUserFromOtherBranch_ThrowsForbiddenException()
    {
        // Arrange: Manager 1 trying to get Staff 2 (belongs to branch 2)
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        // Act & Assert
        var act = async () => await service.GetUserByIdAsync(5);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền truy cập tài khoản này*");
    }

    [Fact]
    public async Task Admin_CanCreateManager_WithValidBranch()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("newmgr", "Pass@123", "Quản lý mới", UserRole.Manager, 1);

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("newmgr");
        result.Role.Should().Be(UserRole.Manager);
        result.BranchId.Should().Be(1);

        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == "newmgr");
        user.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("Pass@123", user!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Admin_CreatingManagerWithoutBranch_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("badmgr", "Pass@123", "Quản lý lỗi", UserRole.Manager, null);

        // Act & Assert
        var act = async () => await service.CreateUserAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*bắt buộc phải gắn với một chi nhánh cơ sở*");
    }

    [Fact]
    public async Task Admin_CreatingAdminWithBranch_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("badadmin", "Pass@123", "Admin lỗi", UserRole.Admin, 1);

        // Act & Assert
        var act = async () => await service.CreateUserAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không được gắn với cơ sở cụ thể*");
    }

    [Fact]
    public async Task Manager_CanCreateStaffInOwnBranch()
    {
        // Arrange: Manager 1 creates a staff in branch 1
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("newstaff1", "Pass@123", "Nhân viên mới 1", UserRole.Staff, 1);

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("newstaff1");
        result.Role.Should().Be(UserRole.Staff);
        result.BranchId.Should().Be(1);
    }

    [Fact]
    public async Task Manager_CannotCreateManagerOrAdmin_ThrowsForbiddenException()
    {
        // Arrange: Manager 1 tries to create another Manager
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("illegal_mgr", "Pass@123", "Tự phong Manager", UserRole.Manager, 1);

        // Act & Assert
        var act = async () => await service.CreateUserAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*chỉ có quyền tạo tài khoản Nhân viên (Staff)*");
    }

    [Fact]
    public async Task Manager_CannotCreateStaffInOtherBranch_ThrowsForbiddenException()
    {
        // Arrange: Manager 1 tries to create staff for branch 2
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("cross_staff", "Pass@123", "Nhân viên cơ sở 2", UserRole.Staff, 2);

        // Act & Assert
        var act = async () => await service.CreateUserAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*chỉ được tạo nhân viên thuộc chi nhánh cơ sở của mình*");
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        var request = new CreateUserRequest("admin", "Pass@123", "Trùng admin", UserRole.Staff, 1);

        // Act & Assert
        var act = async () => await service.CreateUserAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã tồn tại trong hệ thống*");
    }

    [Fact]
    public async Task Admin_CannotDeactivateSelf_ThrowsBusinessException()
    {
        // Arrange: Admin (id 1) tries to toggle user 1
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        // Act & Assert
        var act = async () => await service.ToggleUserStatusAsync(1);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*không thể tự khóa tài khoản của chính mình*");
    }

    [Fact]
    public async Task Admin_CanResetPassword_Successfully()
    {
        // Arrange: Admin resets password for staff1 (id 4)
        var (factory, _, service) = CreateTestSetup(UserRole.Admin, currentUserId: 1);
        await SeedUsersAsync(factory);

        // Act
        await service.ResetPasswordAsync(4, "NewSecret@999");

        // Assert
        await using var db = await factory.CreateDbContextAsync();
        var staff = await db.Users.FindAsync(4);
        BCrypt.Net.BCrypt.Verify("NewSecret@999", staff!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Manager_CannotResetPassword_ForOtherBranchStaff_ThrowsForbiddenException()
    {
        // Arrange: Manager 1 tries to reset password for staff2 (id 5, branch 2)
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1, currentUserId: 2);
        await SeedUsersAsync(factory);

        // Act & Assert
        var act = async () => await service.ResetPasswordAsync(5, "Hack@123");
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*chỉ có quyền reset mật khẩu cho nhân viên thuộc cơ sở của mình*");
    }
}
