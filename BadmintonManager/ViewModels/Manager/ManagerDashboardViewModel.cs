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

namespace BadmintonManager.ViewModels.Manager;

public partial class ManagerDashboardViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private string _branchName = string.Empty;
    [ObservableProperty] private int _courtCount;
    [ObservableProperty] private int _staffCount;
    [ObservableProperty] private int _bookingCount;
    [ObservableProperty] private decimal _branchRevenue;

    public ManagerDashboardViewModel(
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

            CourtCount = await db.Courts.CountAsync(c => c.BranchId == branchId);
            StaffCount = await db.Users.CountAsync(u => u.BranchId == branchId && u.Role == UserRole.Staff);
            BookingCount = await db.Bookings.CountAsync(b => b.Court.BranchId == branchId);
            BranchRevenue = await db.Bookings
                .Where(b => b.Court.BranchId == branchId && (b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed))
                .SumAsync(b => b.FinalAmount);
        }, _dialogService);
    }
}
