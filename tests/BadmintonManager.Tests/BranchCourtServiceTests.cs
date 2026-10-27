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

public class BranchCourtServiceTests
{
    private (TestDbContextFactory factory, CurrentUserContext currentUser, BranchCourtService service) CreateTestSetup(
        UserRole role = UserRole.Admin,
        int? branchId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var factory = new TestDbContextFactory(options);
        var currentUser = new CurrentUserContext();
        currentUser.SetUser(1, "testuser", "Test User", role, branchId);

        var service = new BranchCourtService(factory, currentUser);
        return (factory, currentUser, service);
    }

    private async Task SeedDataAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        var b1 = new Branch { Id = 1, Name = "Cơ sở Cầu Giấy", Address = "123 Cầu Giấy", Phone = "0241234567", IsActive = true };
        var b2 = new Branch { Id = 2, Name = "Cơ sở Đống Đa", Address = "456 Đống Đa", Phone = "0242345678", IsActive = true };
        db.Branches.AddRange(b1, b2);

        var c1 = new Court { Id = 1, BranchId = 1, Name = "Sân 1", IsActive = true, Note = "Thảm chuẩn" };
        var c2 = new Court { Id = 2, BranchId = 1, Name = "Sân 2", IsActive = true, Note = "Thảm chuẩn" };
        var c3 = new Court { Id = 3, BranchId = 2, Name = "Sân 1", IsActive = true, Note = "Sàn gỗ" };
        db.Courts.AddRange(c1, c2, c3);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_CanCreateBranch_Successfully()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        var request = new SaveBranchRequest("Cơ sở Thanh Xuân", "789 Nguyễn Trãi", "0243456789", true);

        // Act
        var result = await service.CreateBranchAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Cơ sở Thanh Xuân");

        await using var db = await factory.CreateDbContextAsync();
        var saved = await db.Branches.FirstOrDefaultAsync(b => b.Name == "Cơ sở Thanh Xuân");
        saved.Should().NotBeNull();
        saved!.Phone.Should().Be("0243456789");
    }

    [Fact]
    public async Task Manager_CannotCreateBranch_ThrowsForbiddenException()
    {
        // Arrange
        var (_, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        var request = new SaveBranchRequest("Cơ sở mới", "Địa chỉ", "0123456789", true);

        // Act & Assert
        var act = async () => await service.CreateBranchAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Chỉ Quản trị viên (Admin) mới có quyền tạo cơ sở mới*");
    }

    [Fact]
    public async Task CreateBranch_DuplicateName_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        var request = new SaveBranchRequest("Cơ sở Cầu Giấy", "Địa chỉ khác", "0999999999", true);

        // Act & Assert
        var act = async () => await service.CreateBranchAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã tồn tại trong hệ thống*");
    }

    [Fact]
    public async Task Admin_CanGetAllBranches_ReturnsAll()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        // Act
        var branches = await service.GetAllBranchDtosAsync();

        // Assert
        branches.Should().HaveCount(2);
    }

    [Fact]
    public async Task Manager_GetAllBranches_ReturnsOnlyTheirBranch()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        await SeedDataAsync(factory);

        // Act
        var branches = await service.GetAllBranchDtosAsync();

        // Assert
        branches.Should().HaveCount(1);
        branches.First().Id.Should().Be(1);
        branches.First().Name.Should().Be("Cơ sở Cầu Giấy");
    }

    [Fact]
    public async Task Admin_CanGetAllCourts_AcrossAllBranches()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        // Act
        var courts = await service.GetAllCourtDtosAsync();

        // Assert
        courts.Should().HaveCount(3);
    }

    [Fact]
    public async Task Manager_GetCourts_RestrictedToTheirOwnBranch()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        await SeedDataAsync(factory);

        // Act
        var courts = await service.GetAllCourtDtosAsync();

        // Assert
        courts.Should().HaveCount(2);
        courts.All(c => c.BranchId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task Manager_CannotAccessCourtFromOtherBranch_ThrowsForbiddenException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        await SeedDataAsync(factory);

        // Act & Assert
        // Court 3 belongs to branch 2
        var act = async () => await service.GetCourtByIdAsync(3);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền truy cập thông tin sân của cơ sở khác*");
    }

    [Fact]
    public async Task CreateCourt_DuplicateNameSameBranch_ThrowsBusinessException()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        var request = new SaveCourtRequest(1, "Sân 1", true, "Ghi chú");

        // Act & Assert
        var act = async () => await service.CreateCourtAsync(request);
        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*đã tồn tại trong cơ sở này*");
    }

    [Fact]
    public async Task CreateCourt_SameNameDifferentBranches_Allowed()
    {
        // Arrange: Sân 2 already exists in Branch 1, we add Sân 2 to Branch 2
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        var request = new SaveCourtRequest(2, "Sân 2", true, "Ghi chú");

        // Act
        var result = await service.CreateCourtAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.BranchId.Should().Be(2);
        result.Name.Should().Be("Sân 2");
    }

    [Fact]
    public async Task Manager_CanCreateCourtInOwnBranch()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        await SeedDataAsync(factory);

        var request = new SaveCourtRequest(1, "Sân 3", true, "Mới tạo");

        // Act
        var result = await service.CreateCourtAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Sân 3");
    }

    [Fact]
    public async Task Manager_CannotCreateCourtInOtherBranch_ThrowsForbiddenException()
    {
        // Arrange: Manager of Branch 1 trying to add court to Branch 2
        var (factory, _, service) = CreateTestSetup(UserRole.Manager, branchId: 1);
        await SeedDataAsync(factory);

        var request = new SaveCourtRequest(2, "Sân 99", true, "Ghi chú");

        // Act & Assert
        var act = async () => await service.CreateCourtAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*không có quyền thêm sân cho cơ sở này*");
    }

    [Fact]
    public async Task Admin_CanToggleCourtStatus()
    {
        // Arrange
        var (factory, _, service) = CreateTestSetup(UserRole.Admin);
        await SeedDataAsync(factory);

        // Act: court 1 initially active -> toggle
        await service.ToggleCourtStatusAsync(1);

        // Assert
        await using var db = await factory.CreateDbContextAsync();
        var court = await db.Courts.FindAsync(1);
        court!.IsActive.Should().BeFalse();
    }
}
