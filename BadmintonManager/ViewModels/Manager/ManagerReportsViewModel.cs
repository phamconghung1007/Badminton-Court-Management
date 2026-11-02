using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.DTOs;
using BadmintonManager.Infrastructure;
using BadmintonManager.Services.Interfaces;
using BadmintonManager.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BadmintonManager.ViewModels.Manager;

public partial class ManagerReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IDialogService _dialogService;
    private readonly CurrentUserContext _currentUser;

    [ObservableProperty]
    private string _branchName = "Cơ sở trực thuộc";

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-14);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private ReportOverviewSummaryDto? _summary;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // Charts
    [ObservableProperty]
    private ISeries[] _revenueSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _revenueXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _occupancySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _occupancyXAxes = Array.Empty<Axis>();

    public ObservableCollection<DailyRevenueDto> DailyRevenues { get; } = new();
    public ObservableCollection<CourtUtilizationDto> CourtUtilizations { get; } = new();
    public ObservableCollection<TopCustomerDto> TopCustomers { get; } = new();

    public ManagerReportsViewModel(
        IReportService reportService,
        IDialogService dialogService,
        CurrentUserContext currentUser)
    {
        _reportService = reportService;
        _dialogService = dialogService;
        _currentUser = currentUser;

        BranchName = _currentUser.BranchName ?? $"Cơ sở #{_currentUser.BranchId}";

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadReportsAsync();
    }

    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        if (FromDate > ToDate)
        {
            _dialogService.ShowWarning("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
            return;
        }

        await RunAsync(async () =>
        {
            // Scoped strictly to Manager's own branch
            int? branchId = _currentUser.BranchId;

            // 1. Overview Summary for this branch
            Summary = await _reportService.GetOverviewSummaryAsync(branchId, FromDate, ToDate);

            // 2. Daily Revenues for this branch
            var daily = await _reportService.GetDailyRevenueAsync(branchId, FromDate, ToDate);
            DailyRevenues.Clear();
            foreach (var d in daily) DailyRevenues.Add(d);
            UpdateRevenueChart(daily);

            // 3. Court Utilization for courts in this branch
            var courts = await _reportService.GetCourtUtilizationAsync(branchId, FromDate, ToDate);
            CourtUtilizations.Clear();
            foreach (var c in courts) CourtUtilizations.Add(c);
            UpdateCourtOccupancyChart(courts);

            // 4. Top Customers at this branch
            var top = await _reportService.GetTopCustomersAsync(branchId, 10);
            TopCustomers.Clear();
            foreach (var t in top) TopCustomers.Add(t);
        });
    }

    private void UpdateRevenueChart(List<DailyRevenueDto> data)
    {
        if (data.Count == 0)
        {
            RevenueSeries = Array.Empty<ISeries>();
            RevenueXAxes = Array.Empty<Axis>();
            return;
        }

        var values = data.Select(d => (double)d.TotalRevenue).ToArray();
        var labels = data.Select(d => d.Date.ToString("dd/MM")).ToArray();

        RevenueSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Doanh thu chi nhánh (VNĐ)",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#1E3A8A")), // Manager Navy
                MaxBarWidth = 40
            }
        };

        RevenueXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 25
            }
        };
    }

    private void UpdateCourtOccupancyChart(List<CourtUtilizationDto> data)
    {
        if (data.Count == 0)
        {
            OccupancySeries = Array.Empty<ISeries>();
            OccupancyXAxes = Array.Empty<Axis>();
            return;
        }

        var values = data.Select(c => c.UtilizationRate).ToArray();
        var labels = data.Select(c => c.CourtName).ToArray();

        OccupancySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Tỷ lệ lấp đầy (%)",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#0D9488")), // Teal
                MaxBarWidth = 45
            }
        };

        OccupancyXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0
            }
        };
    }

    [RelayCommand]
    public void FilterToday()
    {
        FromDate = DateTime.Today;
        ToDate = DateTime.Today;
        _ = LoadReportsAsync();
    }

    [RelayCommand]
    public void FilterThisWeek()
    {
        var today = DateTime.Today;
        int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
        FromDate = today.AddDays(-diff);
        ToDate = today;
        _ = LoadReportsAsync();
    }

    [RelayCommand]
    public void FilterThisMonth()
    {
        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = today;
        _ = LoadReportsAsync();
    }

    [RelayCommand]
    public void FilterLast30Days()
    {
        ToDate = DateTime.Today;
        FromDate = DateTime.Today.AddDays(-30);
        _ = LoadReportsAsync();
    }
}
