using System;
using System.Linq;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels;
using BadmintonManager.ViewModels.Admin;
using BadmintonManager.ViewModels.Base;
using BadmintonManager.ViewModels.Customer;
using BadmintonManager.ViewModels.Manager;
using BadmintonManager.ViewModels.Shared;
using BadmintonManager.ViewModels.Staff;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BadmintonManager.Tests;

public class StubDialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Thông báo") { }
    public void ShowWarning(string message, string title = "Cảnh báo") { }
    public void ShowError(string message, string title = "Lỗi") { }
    public bool ShowConfirmation(string message, string title = "Xác nhận") => true;
}

public class ShellNavigationTests
{
    private IServiceProvider CreateTestServiceProvider(CurrentUserContext currentUser)
    {
        var services = new ServiceCollection();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new TestDbContextFactory(options);

        services.AddSingleton<IDbContextFactory<AppDbContext>>(factory);
        services.AddSingleton(currentUser);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, StubDialogService>();
        services.AddTransient<IAuthService, AuthService>();

        // ViewModels
        services.AddTransient<AdminDashboardViewModel>();
        services.AddTransient<ManagerDashboardViewModel>();
        services.AddTransient<StaffDashboardViewModel>();
        services.AddTransient<CustomerDashboardViewModel>();
        services.AddTransient<UserProfileViewModel>();
        services.AddTransient<ShellViewModel>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Day2_DoD_FourRoles_ShouldDisplayFourDistinctMenus()
    {
        // 1. Admin
        var adminContext = new CurrentUserContext();
        adminContext.SetUser(1, "admin", "Admin User", UserRole.Admin, null);
        var spAdmin = CreateTestServiceProvider(adminContext);
        var adminShell = spAdmin.GetRequiredService<ShellViewModel>();
        var adminMenuKeys = adminShell.MenuItems.Select(m => m.PageKey).ToList();

        // 2. Manager
        var managerContext = new CurrentUserContext();
        managerContext.SetUser(2, "manager1", "Manager User", UserRole.Manager, 1, "Cơ sở Cầu Giấy");
        var spManager = CreateTestServiceProvider(managerContext);
        var managerShell = spManager.GetRequiredService<ShellViewModel>();
        var managerMenuKeys = managerShell.MenuItems.Select(m => m.PageKey).ToList();

        // 3. Staff
        var staffContext = new CurrentUserContext();
        staffContext.SetUser(3, "staff1", "Staff User", UserRole.Staff, 1, "Cơ sở Cầu Giấy");
        var spStaff = CreateTestServiceProvider(staffContext);
        var staffShell = spStaff.GetRequiredService<ShellViewModel>();
        var staffMenuKeys = staffShell.MenuItems.Select(m => m.PageKey).ToList();

        // 4. Customer
        var customerContext = new CurrentUserContext();
        customerContext.SetUser(4, "customer1", "Customer User", UserRole.Customer, null, null, 1, "Đồng");
        var spCustomer = CreateTestServiceProvider(customerContext);
        var customerShell = spCustomer.GetRequiredService<ShellViewModel>();
        var customerMenuKeys = customerShell.MenuItems.Select(m => m.PageKey).ToList();

        // Assert: 4 roles have 4 distinct menus (Day 2 DoD requirement)
        adminMenuKeys.Should().NotEqual(managerMenuKeys);
        adminMenuKeys.Should().NotEqual(staffMenuKeys);
        adminMenuKeys.Should().NotEqual(customerMenuKeys);
        managerMenuKeys.Should().NotEqual(staffMenuKeys);
        managerMenuKeys.Should().NotEqual(customerMenuKeys);
        staffMenuKeys.Should().NotEqual(customerMenuKeys);

        // Assert role-specific menu items
        adminMenuKeys.Should().Contain(new[] { "AdminDashboard", "AdminBranches", "AdminCourts", "AdminUsers", "AdminPricing", "AdminTiers", "AdminReports" });
        managerMenuKeys.Should().Contain(new[] { "ManagerDashboard", "ManagerCourts", "ManagerStaff", "ManagerReports" });
        managerMenuKeys.Should().NotContain("AdminBranches"); // Manager cannot manage system branches
        staffMenuKeys.Should().Contain(new[] { "StaffDashboard", "ScheduleGrid", "CreateBooking", "BookingList" });
        staffMenuKeys.Should().NotContain("ManagerStaff"); // Staff cannot manage staff
        customerMenuKeys.Should().Contain(new[] { "CustomerDashboard", "CustomerBookCourt", "CustomerBookingHistory", "CustomerPoints" });
    }

    [Fact]
    public void ShellViewModel_Admin_InitializesWithAdminDashboard()
    {
        // Arrange
        var adminContext = new CurrentUserContext();
        adminContext.SetUser(1, "admin", "Admin User", UserRole.Admin, null);
        var sp = CreateTestServiceProvider(adminContext);

        // Act
        var shell = sp.GetRequiredService<ShellViewModel>();

        // Assert
        shell.SelectedMenuItem.Should().NotBeNull();
        shell.SelectedMenuItem!.PageKey.Should().Be("AdminDashboard");
        shell.CurrentPageViewModel.Should().BeOfType<AdminDashboardViewModel>();
    }

    [Fact]
    public void ShellViewModel_Manager_InitializesWithManagerDashboard()
    {
        // Arrange
        var context = new CurrentUserContext();
        context.SetUser(2, "manager1", "Manager User", UserRole.Manager, 1, "Cơ sở Cầu Giấy");
        var sp = CreateTestServiceProvider(context);

        // Act
        var shell = sp.GetRequiredService<ShellViewModel>();

        // Assert
        shell.SelectedMenuItem.Should().NotBeNull();
        shell.SelectedMenuItem!.PageKey.Should().Be("ManagerDashboard");
        shell.CurrentPageViewModel.Should().BeOfType<ManagerDashboardViewModel>();
        shell.CurrentBranchInfo.Should().Be("Cơ sở Cầu Giấy");
    }

    [Fact]
    public void ShellViewModel_NavigateToUserProfile_ResolvesUserProfileViewModel()
    {
        // Arrange
        var context = new CurrentUserContext();
        context.SetUser(1, "admin", "Admin User", UserRole.Admin, null);
        var sp = CreateTestServiceProvider(context);
        var shell = sp.GetRequiredService<ShellViewModel>();

        // Act
        var profileItem = shell.MenuItems.First(m => m.PageKey == "UserProfile");
        shell.SelectedMenuItem = profileItem;

        // Assert
        shell.CurrentPageViewModel.Should().BeOfType<UserProfileViewModel>();
        shell.CurrentPageTitle.Should().Be(profileItem.Title);
    }

    [Fact]
    public void ShellViewModel_NavigateToUpcomingFeature_ResolvesPlaceholderPageWithRoadmapTarget()
    {
        // Arrange
        var context = new CurrentUserContext();
        context.SetUser(1, "admin", "Admin User", UserRole.Admin, null);
        var sp = CreateTestServiceProvider(context);
        var shell = sp.GetRequiredService<ShellViewModel>();

        // Act
        var scheduleItem = shell.MenuItems.First(m => m.PageKey == "ScheduleGrid");
        shell.SelectedMenuItem = scheduleItem;

        // Assert
        shell.CurrentPageViewModel.Should().BeOfType<PlaceholderPageViewModel>();
        var placeholder = (PlaceholderPageViewModel)shell.CurrentPageViewModel!;
        placeholder.DayTarget.Should().Contain("Ngày 5");
    }

    [Fact]
    public void ShellViewModel_Logout_ClearsCurrentUserContext_AndRaisesEvent()
    {
        // Arrange
        var context = new CurrentUserContext();
        context.SetUser(1, "admin", "Admin User", UserRole.Admin, null);
        var sp = CreateTestServiceProvider(context);
        var shell = sp.GetRequiredService<ShellViewModel>();

        bool logoutFired = false;
        shell.LogoutRequested += () => logoutFired = true;

        // Act
        shell.LogoutCommand.Execute(null);

        // Assert
        logoutFired.Should().BeTrue();
        context.IsAuthenticated.Should().BeFalse();
        context.Id.Should().BeNull();
        context.Username.Should().BeNull();
    }
}
