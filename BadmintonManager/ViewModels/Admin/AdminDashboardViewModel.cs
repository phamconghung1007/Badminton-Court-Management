using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Data;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Enums;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.ViewModels.Admin;

public partial class AdminDashboardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private int _totalBranches;
    [ObservableProperty] private int _totalCourts;
    [ObservableProperty] private int _totalBookings;
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private int _totalUsers;

    public AdminDashboardViewModel(IDbContextFactory<AppDbContext> factory, IDialogService dialogService)
    {
        _factory = factory;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        await RunAsync(async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            TotalBranches = await db.Branches.CountAsync();
            TotalCourts = await db.Courts.CountAsync();
            TotalUsers = await db.Users.CountAsync();
            TotalBookings = await db.Bookings.CountAsync();
            TotalRevenue = await db.Bookings
                .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed)
                .SumAsync(b => b.FinalAmount);
        }, _dialogService);
    }
}
