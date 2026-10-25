using System;
using System.Collections.ObjectModel;
using BadmintonManager.Common;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.ViewModels.Admin;
using BadmintonManager.ViewModels.Base;
using BadmintonManager.ViewModels.Customer;
using BadmintonManager.ViewModels.Manager;
using BadmintonManager.ViewModels.Shared;
using BadmintonManager.ViewModels.Staff;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BadmintonManager.ViewModels;

public record MenuItem(string Title, string Icon, string PageKey);

public partial class ShellViewModel : ViewModelBase
{
    private readonly CurrentUserContext _currentUser;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

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

    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    public ObservableCollection<MenuItem> MenuItems { get; } = new();

    public event Action? LogoutRequested;

    public ShellViewModel(
        CurrentUserContext currentUser,
        INavigationService navigationService,
        IServiceProvider serviceProvider)
    {
        _currentUser = currentUser;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;

        _navigationService.Navigated += OnNavigated;

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
            ? (_currentUser.BranchName ?? $"Cơ sở ID #{_currentUser.BranchId}")
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
                MenuItems.Add(new MenuItem("Tài khoản cá nhân", "AccountCircle", "UserProfile"));
                break;

            case UserRole.Manager:
                MenuItems.Add(new MenuItem("Bảng điều khiển cơ sở", "ViewDashboard", "ManagerDashboard"));
                MenuItems.Add(new MenuItem("Lịch đặt sân cơ sở", "CalendarMonth", "ScheduleGrid"));
                MenuItems.Add(new MenuItem("Duyệt & Quản lý booking", "BookCheck", "BookingList"));
                MenuItems.Add(new MenuItem("Sân của cơ sở", "Grid", "ManagerCourts"));
                MenuItems.Add(new MenuItem("Nhân viên cơ sở", "AccountMultiple", "ManagerStaff"));
                MenuItems.Add(new MenuItem("Khách hàng", "AccountGroup", "Customers"));
                MenuItems.Add(new MenuItem("Báo cáo doanh thu cơ sở", "ChartLine", "ManagerReports"));
                MenuItems.Add(new MenuItem("Tài khoản cá nhân", "AccountCircle", "UserProfile"));
                break;

            case UserRole.Staff:
                MenuItems.Add(new MenuItem("Bảng điều khiển ca trực", "ViewDashboard", "StaffDashboard"));
                MenuItems.Add(new MenuItem("Lịch sân (Chính)", "CalendarMonth", "ScheduleGrid"));
                MenuItems.Add(new MenuItem("Đặt sân / Check-in", "CalendarPlus", "CreateBooking"));
                MenuItems.Add(new MenuItem("Danh sách booking", "ClipboardList", "BookingList"));
                MenuItems.Add(new MenuItem("Khách hàng", "AccountGroup", "Customers"));
                MenuItems.Add(new MenuItem("Tài khoản cá nhân", "AccountCircle", "UserProfile"));
                break;

            case UserRole.Customer:
                MenuItems.Add(new MenuItem("Tổng quan & Điểm", "ViewDashboard", "CustomerDashboard"));
                MenuItems.Add(new MenuItem("Tìm & Đặt sân", "CalendarSearch", "CustomerBookCourt"));
                MenuItems.Add(new MenuItem("Lịch sử đặt sân", "History", "CustomerBookingHistory"));
                MenuItems.Add(new MenuItem("Điểm thưởng & Hạng", "StarOutline", "CustomerPoints"));
                MenuItems.Add(new MenuItem("Hồ sơ cá nhân", "AccountCircle", "UserProfile"));
                break;
        }

        if (MenuItems.Count > 0)
        {
            SelectedMenuItem = MenuItems[0];
            CurrentPageTitle = SelectedMenuItem.Title;
            _navigationService.NavigateTo(SelectedMenuItem.PageKey);
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

    private void OnNavigated(string pageKey, object? parameter)
    {
        CurrentPageViewModel = ResolvePageViewModel(pageKey, parameter);
    }

    private ViewModelBase ResolvePageViewModel(string pageKey, object? parameter)
    {
        return pageKey switch
        {
            "AdminDashboard" => _serviceProvider.GetRequiredService<AdminDashboardViewModel>(),
            "ManagerDashboard" => _serviceProvider.GetRequiredService<ManagerDashboardViewModel>(),
            "StaffDashboard" => _serviceProvider.GetRequiredService<StaffDashboardViewModel>(),
            "CustomerDashboard" => _serviceProvider.GetRequiredService<CustomerDashboardViewModel>(),
            "UserProfile" or "CustomerProfile" => _serviceProvider.GetRequiredService<UserProfileViewModel>(),

            "AdminBranches" => new PlaceholderPageViewModel(
                "Quản lý cơ sở (Branches)",
                "Quản lý danh sách chi nhánh cơ sở toàn hệ thống: thêm mới, sửa đổi thông tin, thiết lập địa chỉ và cấu hình số lượng sân.",
                "Ngày 3 - Quản trị Danh mục Cơ sở & Sân",
                "Domain"),

            "AdminCourts" or "ManagerCourts" => new PlaceholderPageViewModel(
                "Quản lý sân cầu lông (Courts)",
                "Quản lý danh sách sân, trạng thái hoạt động (Hoạt động / Bảo trì / Tạm ngưng) và quy định gắn với từng cơ sở.",
                "Ngày 3 - Quản trị Danh mục Cơ sở & Sân",
                "Grid"),

            "AdminUsers" or "ManagerStaff" => new PlaceholderPageViewModel(
                "Quản lý tài khoản & Phân quyền",
                "Tạo và quản lý tài khoản nhân sự (Admin, Manager, Staff), khóa/mở khóa tài khoản, phân bổ quản lý cơ sở và reset mật khẩu.",
                "Ngày 3 - Quản trị Nhân sự & Phân quyền",
                "AccountMultiple"),

            "AdminPricing" => new PlaceholderPageViewModel(
                "Bảng giá gốc & Khung giờ",
                "Thiết lập ma trận khung giờ giá theo Ngày thường (Thứ 2 - Thứ 6) và Cuối tuần (Thứ 7 - CN), giờ cao điểm / thấp điểm theo Section 3.",
                "Ngày 4 - Cấu hình Bảng giá & Hạng hội viên",
                "CurrencyUsd"),

            "AdminTiers" or "CustomerPoints" => new PlaceholderPageViewModel(
                "Hạng hội viên & Điểm thưởng",
                "Chính sách phân hạng hội viên (Đồng, Bạc, Vàng), tỷ lệ tích điểm và chiết khấu giảm giá trực tiếp theo quy định Section 3.3.",
                "Ngày 4 - Cấu hình Bảng giá & Hạng hội viên",
                "CardAccountDetailsStar"),

            "Customers" => new PlaceholderPageViewModel(
                "Quản lý khách hàng",
                "Tra cứu danh bạ khách hàng qua SĐT/CCCD, xem lịch sử đặt sân, số dư điểm thưởng và cập nhật thông tin hội viên.",
                "Ngày 4 - Quản lý Khách hàng & Hội viên",
                "AccountGroup"),

            "ScheduleGrid" => new PlaceholderPageViewModel(
                "Lịch sân trực quan (Schedule Grid)",
                "Ma trận lịch đặt sân trực quan theo thời gian thực (06:00 - 24:00), hiển thị tình trạng Trống, Đã đặt, Đang sử dụng, Bảo trì.",
                "Ngày 5 - Ma trận Lịch sân Trực quan",
                "CalendarMonth"),

            "CreateBooking" or "CustomerBookCourt" => new PlaceholderPageViewModel(
                "Đặt sân & Check-in",
                "Quy trình đặt sân trực tuyến và tại quầy: chọn sân, ngày giờ, tính toán tiền theo khung giờ, check-in và áp dụng mã giảm giá.",
                "Ngày 6 - Quy trình Đặt sân & Check-in",
                "CalendarPlus"),

            "BookingList" or "CustomerBookingHistory" => new PlaceholderPageViewModel(
                "Danh sách & Lịch sử đặt sân",
                "Quản lý tất cả phiếu đặt sân, lọc theo trạng thái (Pending, Confirmed, Completed, Cancelled), huỷ lịch và hoàn tiền.",
                "Ngày 6 - Quản lý Phiếu đặt sân & Lịch sử",
                "ClipboardList"),

            "AdminReports" or "ManagerReports" => new PlaceholderPageViewModel(
                "Báo cáo & Thống kê doanh thu",
                "Biểu đồ trực quan LiveCharts thể hiện doanh thu theo ngày/tháng/cơ sở, tỷ lệ lấp đầy sân và xuất file Excel chuẩn EPPlus.",
                "Ngày 7 - Báo cáo Thống kê & Xuất Excel",
                "ChartLine"),

            _ => new PlaceholderPageViewModel(
                pageKey,
                "Chức năng đang được phát triển theo lộ trình dự án.",
                "Đang hoàn thiện",
                "ApplicationOutline")
        };
    }

    [RelayCommand]
    private void Logout()
    {
        _navigationService.Navigated -= OnNavigated;
        _currentUser.Clear();
        LogoutRequested?.Invoke();
    }
}
