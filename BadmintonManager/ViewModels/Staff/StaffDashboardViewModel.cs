using System;
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

namespace BadmintonManager.ViewModels.Staff;

public partial class StaffDashboardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private int _activeCourts;
    [ObservableProperty] private int _todayBookings;
    [ObservableProperty] private int _pendingBookings;

    public StaffDashboardViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IDialogService dialogService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _dialogService = dialogService;
        BranchName = _currentUser.BranchName ?? $"Cơ sở ID #{_currentUser.BranchId}";
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        if (!_currentUser.BranchId.HasValue) return;
        int branchId = _currentUser.BranchId.Value;

        await RunAsync(async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            var branch = await db.Branches.FindAsync(branchId);
            if (branch != null) BranchName = branch.Name;

            ActiveCourts = await db.Courts.CountAsync(c => c.BranchId == branchId && c.IsActive);

            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            TodayBookings = await db.Bookings.CountAsync(b => b.Court.BranchId == branchId && b.StartTime >= today && b.StartTime < tomorrow);
            PendingBookings = await db.Bookings.CountAsync(b => b.Court.BranchId == branchId && b.Status == BookingStatus.Pending);
        }, _dialogService);
    }
}
