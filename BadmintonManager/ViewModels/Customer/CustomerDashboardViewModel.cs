using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
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

    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _tierName = "Đồng";
    [ObservableProperty] private decimal _discountPercent;
    [ObservableProperty] private int _totalPoints;
    [ObservableProperty] private int _activeBookingsCount;
    [ObservableProperty] private int _completedBookingsCount;

    public CustomerDashboardViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IDialogService dialogService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _dialogService = dialogService;
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
            }

            ActiveBookingsCount = await db.Bookings.CountAsync(b => b.CustomerId == customerId && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
            CompletedBookingsCount = await db.Bookings.CountAsync(b => b.CustomerId == customerId && b.Status == BookingStatus.Completed);
        }, _dialogService);
    }
}
