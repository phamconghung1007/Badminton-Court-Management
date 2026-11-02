using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using BadmintonManager.ViewModels.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class CustomerListViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly StubDialogService _dialogService;
    private readonly CustomerService _customerService;

    public CustomerListViewModelTests()
    {
        var dbName = $"Badminton_CustVm_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(1, "staff1", "Staff One", UserRole.Staff, 1);
        _dialogService = new StubDialogService();
        _customerService = new CustomerService(_factory, _currentUser);

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();
        var bronze = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        var silver = new MembershipTier { Id = 2, Name = "Bạc", MinPoints = 500, DiscountPercent = 5, SortOrder = 2 };
        var gold = new MembershipTier { Id = 3, Name = "Vàng", MinPoints = 1500, DiscountPercent = 10, SortOrder = 3 };
        db.MembershipTiers.AddRange(bronze, silver, gold);

        var cust1 = new Customer
        {
            Id = 1,
            FullName = "Nguyễn Văn A",
            Phone = "0911222333",
            MembershipTierId = 3, // Vàng
            TotalPoints = 1600,
            MembershipCardNo = "VIP-GOLD",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var cust2 = new Customer
        {
            Id = 2,
            FullName = "Trần Thị B",
            Phone = "0922333444",
            MembershipTierId = 2, // Bạc
            TotalPoints = 800,
            MembershipCardNo = "VIP-SILVER",
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        var cust3 = new Customer
        {
            Id = 3,
            FullName = "Lê Hoàng C",
            Phone = "0933444555",
            MembershipTierId = 1, // Đồng
            TotalPoints = 50,
            MembershipCardNo = null,
            IsActive = false, // Tạm khóa
            CreatedAt = DateTime.Now
        };

        db.Customers.AddRange(cust1, cust2, cust3);
        db.SaveChanges();
    }

    [Fact]
    public async Task InitializeAsync_LoadsAllCustomersAndKPIs()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);

        await vm.InitializeAsync();

        vm.Customers.Should().HaveCount(3);
        vm.TotalCustomerCount.Should().Be(3);
        vm.ActiveCustomerCount.Should().Be(2);
        vm.GoldTierCount.Should().Be(1);
        vm.SilverTierCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_FiltersCustomersByKeyword()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);
        await vm.InitializeAsync();

        vm.SearchKeyword = "0922";
        await vm.SearchAsync();

        vm.Customers.Should().HaveCount(1);
        vm.Customers.First().FullName.Should().Be("Trần Thị B");
    }

    [Fact]
    public async Task SaveCustomerAsync_CreateNewCustomer_Success()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);
        await vm.InitializeAsync();

        vm.OpenCreateDrawer();
        vm.IsEditDrawerOpen.Should().BeTrue();
        vm.IsEditing.Should().BeFalse();

        vm.EditFullName = "Phạm Văn Mới";
        vm.EditPhone = "0944555666";
        vm.EditCardNo = "VIP-NEW";

        await vm.SaveCustomerAsync();

        vm.IsEditDrawerOpen.Should().BeFalse();
        vm.FormErrorMessage.Should().BeNull();
        vm.Customers.Should().HaveCount(4);
        vm.Customers.Any(c => c.Phone == "0944555666").Should().BeTrue();
    }

    [Fact]
    public async Task SaveCustomerAsync_EmptyName_SetsFormErrorMessage()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);
        await vm.InitializeAsync();

        vm.OpenCreateDrawer();
        vm.EditFullName = "   "; // Trống
        vm.EditPhone = "0912345678";

        await vm.SaveCustomerAsync();

        vm.IsEditDrawerOpen.Should().BeTrue();
        vm.FormErrorMessage.Should().Be("Vui lòng nhập họ và tên khách hàng.");
    }

    [Fact]
    public async Task ToggleCustomerStatusAsync_TogglesIsActive()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);
        await vm.InitializeAsync();

        var target = vm.Customers.First(c => c.Id == 1);
        target.IsActive.Should().BeTrue();

        await vm.ToggleCustomerStatusAsync(target);

        var updated = vm.Customers.First(c => c.Id == 1);
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ViewCustomerDetailAsync_LoadsDetailsAndOpensDrawer()
    {
        var vm = new CustomerListViewModel(_customerService, _currentUser, _dialogService);
        await vm.InitializeAsync();

        var target = vm.Customers.First(c => c.Id == 1);
        await vm.ViewCustomerDetailAsync(target);

        vm.IsDetailDrawerOpen.Should().BeTrue();
        vm.CurrentDetail.Should().NotBeNull();
        vm.CurrentDetail!.Customer.FullName.Should().Be("Nguyễn Văn A");
        vm.CurrentDetail.Customer.TierName.Should().Be("Vàng");
    }
}
