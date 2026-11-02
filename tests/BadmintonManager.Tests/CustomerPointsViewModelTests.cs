using System;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using BadmintonManager.ViewModels.Customer;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BadmintonManager.Tests;

public class CustomerPointsViewModelTests
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly CustomerService _customerService;
    private readonly RecordingDialogService _dialogService;

    public CustomerPointsViewModelTests()
    {
        var dbName = $"Badminton_CustPointsVm_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _factory = new TestDbContextFactory(options);
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(10, "cust1", "Nguyễn Văn Cust1", UserRole.Customer, null, null, 100, "Vàng");

        _customerService = new CustomerService(_factory, _currentUser);
        _dialogService = new RecordingDialogService();

        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        var tierDong = new MembershipTier { Id = 1, Name = "Đồng", MinPoints = 0, DiscountPercent = 0, SortOrder = 1 };
        var tierBac = new MembershipTier { Id = 2, Name = "Bạc", MinPoints = 500, DiscountPercent = 5, SortOrder = 2 };
        var tierVang = new MembershipTier { Id = 3, Name = "Vàng", MinPoints = 1500, DiscountPercent = 10, SortOrder = 3 };
        db.MembershipTiers.AddRange(tierDong, tierBac, tierVang);

        var cust1 = new Customer { Id = 100, FullName = "Nguyễn Văn Cust1", Phone = "0900000001", UserId = 10, MembershipTierId = 3, TotalPoints = 1600, MembershipCardNo = "VIP-001", IsActive = true, CreatedAt = DateTime.Now };
        db.Customers.Add(cust1);

        db.PointTransactions.Add(new PointTransaction
        {
            Id = 1,
            CustomerId = 100,
            Points = 50,
            Reason = "Tích điểm hoàn tất đơn BK01",
            CreatedAt = DateTime.Now.AddDays(-2)
        });

        db.PointTransactions.Add(new PointTransaction
        {
            Id = 2,
            CustomerId = 100,
            Points = 100,
            Reason = "Tích điểm hoàn tất đơn BK02",
            CreatedAt = DateTime.Now.AddDays(-1)
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task LoadPointsData_LoadsTiersAndPointTransactionsHistory()
    {
        var vm = new CustomerPointsViewModel(_factory, _currentUser, _customerService, _dialogService);

        await vm.LoadPointsDataAsync();

        vm.CustomerName.Should().Be("Nguyễn Văn Cust1");
        vm.CurrentTierName.Should().Be("Vàng");
        vm.DiscountPercent.Should().Be(10m);
        vm.TotalPoints.Should().Be(1600);
        vm.MembershipCardNo.Should().Be("VIP-001");

        vm.MembershipTiers.Should().HaveCount(3);
        vm.MembershipTiers.Select(t => t.Name).Should().Contain(new[] { "Đồng", "Bạc", "Vàng" });

        vm.PointHistory.Should().HaveCount(2);
    }
}
