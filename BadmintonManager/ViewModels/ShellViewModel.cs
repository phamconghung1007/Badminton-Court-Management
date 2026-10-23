using System;
using System.Collections.ObjectModel;
using BadmintonManager.Common;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BadmintonManager.ViewModels;

public record MenuItem(string Title, string Icon, string PageKey);

public partial class ShellViewModel : ViewModelBase
{
    private readonly CurrentUserContext _currentUser;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _currentUserName = string.Empty;

    [ObservableProperty]
    private string _currentUserRole = string.Empty;

    [ObservableProperty]
    private string _currentBranchInfo = string.Empty;

    [ObservableProperty]
    private string _currentPageTitle = "Tổng quan";

    [ObservableProperty]
    private MenuItem? _selectedMenuItem;

    public ObservableCollection<MenuItem> MenuItems { get; } = new();

    public event Action? LogoutRequested;

    public ShellViewModel(CurrentUserContext currentUser, INavigationService navigationService)
    {
        _currentUser = currentUser;
        _navigationService = navigationService;
        InitializeUserInfo();
        BuildMenu();
    }

    private void InitializeUserInfo()
    {
        CurrentUserName = _currentUser.FullName ?? _currentUser.Username ?? "Người dùng";
        CurrentUserRole = _currentUser.Role switch
        {
            UserRole.Admin => "Quản trị viên (Admin)",
            UserRole.Manager => "Quản lý cơ sở (Manager)",
            UserRole.Staff => "Nhân viên (Staff)",
            UserRole.Customer => "Khách hàng (Customer)",
            _ => "Người dùng"
        };

        CurrentBranchInfo = _currentUser.BranchId.HasValue
            ? $"Cơ sở ID #{_currentUser.BranchId}"
            : "Toàn hệ thống";
    }

    private void BuildMenu()
    {
        MenuItems.Clear();

        switch (_currentUser.Role)
        {
            case UserRole.Admin:
                MenuItems.Add(new MenuItem("Bảng điều khiển", "ViewDashboard", "AdminDashboard"));
                MenuItems.Add(new MenuItem("Quản lý cơ sở", "Domain", "AdminBranches"));
                MenuItems.Add(new MenuItem("Quản lý sân", "Grid", "AdminCourts"));
                MenuItems.Add(new MenuItem("Tài khoản người dùng", "AccountMultiple", "AdminUsers"));
                MenuItems.Add(new MenuItem("Bảng giá & Khung giờ", "CurrencyUsd", "AdminPricing"));
                MenuItems.Add(new MenuItem("Hạng hội viên", "CardAccountDetailsStar", "AdminTiers"));
                MenuItems.Add(new MenuItem("Quản lý khách hàng", "AccountGroup", "Customers"));
                MenuItems.Add(new MenuItem("Lịch đặt toàn hệ thống", "CalendarMonth", "ScheduleGrid"));
                MenuItems.Add(new MenuItem("Báo cáo doanh thu", "ChartLine", "AdminReports"));
                break;

            case UserRole.Manager:
                MenuItems.Add(new MenuItem("Bảng điều khiển cơ sở", "ViewDashboard", "ManagerDashboard"));
                MenuItems.Add(new MenuItem("Lịch đặt sân", "CalendarMonth", "ScheduleGrid"));
                MenuItems.Add(new MenuItem("Duyệt & Quản lý booking", "BookCheck", "BookingList"));
                MenuItems.Add(new MenuItem("Sân của cơ sở", "Grid", "ManagerCourts"));
                MenuItems.Add(new MenuItem("Nhân viên cơ sở", "AccountMultiple", "ManagerStaff"));
                MenuItems.Add(new MenuItem("Khách hàng cơ sở", "AccountGroup", "Customers"));
                MenuItems.Add(new MenuItem("Báo cáo doanh thu cơ sở", "ChartLine", "ManagerReports"));
                break;

            case UserRole.Staff:
                MenuItems.Add(new MenuItem("Lịch sân (Chính)", "CalendarMonth", "ScheduleGrid"));
                MenuItems.Add(new MenuItem("Đặt sân / Check-in", "CalendarPlus", "CreateBooking"));
                MenuItems.Add(new MenuItem("Danh sách booking", "ClipboardList", "BookingList"));
                MenuItems.Add(new MenuItem("Khách hàng", "AccountGroup", "Customers"));
                break;

            case UserRole.Customer:
                MenuItems.Add(new MenuItem("Tìm & Đặt sân", "CalendarSearch", "CustomerBookCourt"));
                MenuItems.Add(new MenuItem("Lịch sử đặt sân", "History", "CustomerBookingHistory"));
                MenuItems.Add(new MenuItem("Điểm thưởng & Hạng", "StarOutline", "CustomerPoints"));
                MenuItems.Add(new MenuItem("Hồ sơ cá nhân", "AccountOutline", "CustomerProfile"));
                break;
        }

        if (MenuItems.Count > 0)
        {
            SelectedMenuItem = MenuItems[0];
            CurrentPageTitle = SelectedMenuItem.Title;
        }
    }

    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        if (value != null)
        {
            CurrentPageTitle = value.Title;
            _navigationService.NavigateTo(value.PageKey);
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _currentUser.Clear();
        LogoutRequested?.Invoke();
    }
}
