using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Models.Entities;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.ViewModels.Customer;

public partial class CustomerPointsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly ICustomerService _customerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private string _customerName = string.Empty;
    [ObservableProperty] private string _currentTierName = "Đồng";
    [ObservableProperty] private decimal _discountPercent;
    [ObservableProperty] private int _totalPoints;
    [ObservableProperty] private string? _membershipCardNo;

    public ObservableCollection<MembershipTierDto> MembershipTiers { get; } = new();
    public ObservableCollection<CustomerPointHistoryDto> PointHistory { get; } = new();

    public record MembershipTierDto(int Id, string Name, int MinPoints, decimal DiscountPercent, int SortOrder);

    public CustomerPointsViewModel(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        ICustomerService customerService,
        IDialogService dialogService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _customerService = customerService;
        _dialogService = dialogService;
        CustomerName = _currentUser.FullName ?? _currentUser.Username ?? "Khách hàng";
    }

    [RelayCommand]
    public async Task LoadPointsDataAsync()
    {
        if (!_currentUser.CustomerId.HasValue) return;
        int customerId = _currentUser.CustomerId.Value;

        await RunAsync(async () =>
        {
            // 1. Nạp chính sách hạng thành viên
            await using var db = await _factory.CreateDbContextAsync();
            var tiers = await db.MembershipTiers
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            MembershipTiers.Clear();
            foreach (var t in tiers)
            {
                MembershipTiers.Add(new MembershipTierDto(t.Id, t.Name, t.MinPoints, t.DiscountPercent, t.SortOrder));
            }

            // 2. Nạp chi tiết hồ sơ & lịch sử điểm của khách
            var detail = await _customerService.GetCustomerDetailAsync(customerId);
            if (detail != null)
            {
                CustomerName = detail.Customer.FullName;
                CurrentTierName = detail.Customer.TierName;
                DiscountPercent = detail.Customer.DiscountPercent;
                TotalPoints = detail.Customer.TotalPoints;
                MembershipCardNo = detail.Customer.MembershipCardNo ?? "CHƯA CẤP";

                PointHistory.Clear();
                foreach (var pt in detail.PointHistory.OrderByDescending(p => p.CreatedAt))
                {
                    PointHistory.Add(pt);
                }
            }
        }, _dialogService);
    }
}
