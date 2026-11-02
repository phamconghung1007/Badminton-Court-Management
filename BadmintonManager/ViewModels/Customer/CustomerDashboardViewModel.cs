using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.ViewModels.Customer;

public partial class CustomerDashboardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly IBookingService _bookingService;

    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _tierName = "Đồng";
    [ObservableProperty] private decimal _discountPercent;
    [ObservableProperty] private int _totalPoints;
    [ObservableProperty] private int _activeBookingsCount;
    [ObservableProperty] private int _completedBookingsCount;

    // Tiến độ nâng hạng hội viên
    [ObservableProperty] private string _nextTierName = "Hạng cao nhất";
    [ObservableProperty] private int _pointsToNextTier;
    [ObservableProperty] private double _tierProgressPercent = 100.0;
    [ObservableProperty] private string _tierProgressSummary = string.Empty;

    public ObservableCollection<BookingItemDto> UpcomingBookings { get; } = new();

    public CustomerDashboardViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IDialogService dialogService,
        INavigationService navigationService,
        IBookingService bookingService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _bookingService = bookingService;
        CustomerName = _currentUser.FullName ?? _currentUser.Username ?? "Khách hàng";
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        if (!_currentUser.CustomerId.HasValue) return;
        int customerId = _currentUser.CustomerId.Value;

        await RunAsync(async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            var customer = await db.Customers
                .Include(c => c.MembershipTier)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer != null)
            {
                CustomerName = customer.FullName;
                TierName = customer.MembershipTier.Name;
                DiscountPercent = customer.MembershipTier.DiscountPercent;
                TotalPoints = customer.TotalPoints;

                // Tính toán hạng kế tiếp
                var allTiers = await db.MembershipTiers
                    .OrderBy(t => t.MinPoints)
                    .ToListAsync();

                var nextTier = allTiers.FirstOrDefault(t => t.MinPoints > customer.TotalPoints);
                if (nextTier != null)
                {
                    NextTierName = nextTier.Name;
                    PointsToNextTier = Math.Max(0, nextTier.MinPoints - customer.TotalPoints);
                    var currentTierMin = customer.MembershipTier.MinPoints;
                    int range = nextTier.MinPoints - currentTierMin;
                    int progress = customer.TotalPoints - currentTierMin;
                    TierProgressPercent = range > 0 ? Math.Min(100.0, Math.Max(0.0, (double)progress / range * 100.0)) : 0.0;
                    TierProgressSummary = $"Tích lũy thêm {PointsToNextTier:N0} điểm để thăng hạng {NextTierName} (Ưu đãi {nextTier.DiscountPercent:0.#}%)";
                }
                else
                {
                    NextTierName = "Đạt hạng tối đa";
                    PointsToNextTier = 0;
                    TierProgressPercent = 100.0;
                    TierProgressSummary = "Quý khách đang sở hữu hạng hội viên cao cấp nhất hệ thống!";
                }
            }

            ActiveBookingsCount = await db.Bookings.CountAsync(b => b.CustomerId == customerId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
            CompletedBookingsCount = await db.Bookings.CountAsync(b => b.CustomerId == customerId && b.Status == BookingStatus.Completed);

            // Nạp 5 lịch sắp diễn ra của riêng khách
            var upcoming = await _bookingService.GetBookingsAsync(null, null, null, null, customerId);
            UpcomingBookings.Clear();
            foreach (var b in upcoming.Where(b => b.StartTime >= DateTime.Now && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)).Take(5))
            {
                UpcomingBookings.Add(b);
            }
        }, _dialogService);
    }

    [RelayCommand]
    public void NavigateToBookCourt()
    {
        _navigationService.NavigateTo("CustomerBookCourt");
    }

    [RelayCommand]
    public void NavigateToBookingHistory()
    {
        _navigationService.NavigateTo("CustomerBookingHistory");
    }

    [RelayCommand]
    public void NavigateToPoints()
    {
        _navigationService.NavigateTo("CustomerPoints");
    }
}
