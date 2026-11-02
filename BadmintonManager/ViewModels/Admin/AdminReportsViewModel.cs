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

namespace BadmintonManager.ViewModels.Admin;

public partial class AdminReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly IBranchCourtService _branchCourtService;
    private readonly IDialogService _dialogService;
    private readonly CurrentUserContext _currentUser;

    [ObservableProperty]
    private DateTime _fromDate = DateTime.Today.AddDays(-14);

    [ObservableProperty]
    private DateTime _toDate = DateTime.Today;

    [ObservableProperty]
    private BranchDto? _selectedBranch;

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
    private ISeries[] _branchComparisonSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _branchComparisonXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _occupancySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _occupancyXAxes = Array.Empty<Axis>();

    public ObservableCollection<BranchDto> Branches { get; } = new();
    public ObservableCollection<DailyRevenueDto> DailyRevenues { get; } = new();
    public ObservableCollection<MonthlyRevenueDto> MonthlyRevenues { get; } = new();
    public ObservableCollection<CourtUtilizationDto> CourtUtilizations { get; } = new();
    public ObservableCollection<TopCustomerDto> TopCustomers { get; } = new();
    public ObservableCollection<BranchRevenueComparisonDto> BranchRevenueComparisons { get; } = new();
    public ObservableCollection<BranchOccupancyComparisonDto> BranchOccupancyComparisons { get; } = new();

    public AdminReportsViewModel(
        IReportService reportService,
        IBranchCourtService branchCourtService,
        IDialogService dialogService,
        CurrentUserContext currentUser)
    {
        _reportService = reportService;
        _branchCourtService = branchCourtService;
        _dialogService = dialogService;
        _currentUser = currentUser;

        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadBranchesAsync();
        await LoadReportsAsync();
    }

    private async Task LoadBranchesAsync()
    {
        try
        {
            var branches = await _branchCourtService.GetAllBranchDtosAsync(onlyActive: true);
            Branches.Clear();
            foreach (var b in branches)
            {
                Branches.Add(b);
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Không thể tải danh sách chi nhánh: {ex.Message}");
        }
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
            int? branchId = SelectedBranch?.Id;

            // 1. Overview Summary
            Summary = await _reportService.GetOverviewSummaryAsync(branchId, FromDate, ToDate);

            // 2. Daily Revenues
            var daily = await _reportService.GetDailyRevenueAsync(branchId, FromDate, ToDate);
            DailyRevenues.Clear();
            foreach (var d in daily) DailyRevenues.Add(d);
            UpdateRevenueChart(daily);

            // 3. Court Utilization
            var courts = await _reportService.GetCourtUtilizationAsync(branchId, FromDate, ToDate);
            CourtUtilizations.Clear();
            foreach (var c in courts) CourtUtilizations.Add(c);
            UpdateCourtOccupancyChart(courts);

            // 4. Top Customers
            var top = await _reportService.GetTopCustomersAsync(branchId, 10);
            TopCustomers.Clear();
            foreach (var t in top) TopCustomers.Add(t);

            // 5. Branch Comparisons (Admin only feature)
            if (_currentUser.IsAdmin)
            {
                var revComp = await _reportService.GetBranchRevenueComparisonAsync(FromDate, ToDate);
                BranchRevenueComparisons.Clear();
                foreach (var rc in revComp) BranchRevenueComparisons.Add(rc);

                var occComp = await _reportService.GetBranchOccupancyComparisonAsync(FromDate, ToDate);
                BranchOccupancyComparisons.Clear();
                foreach (var oc in occComp) BranchOccupancyComparisons.Add(oc);

                UpdateBranchComparisonChart(revComp);
            }
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
                Name = "Doanh thu (VNĐ)",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
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

    private void UpdateBranchComparisonChart(List<BranchRevenueComparisonDto> data)
    {
        if (data.Count == 0)
        {
            BranchComparisonSeries = Array.Empty<ISeries>();
            BranchComparisonXAxes = Array.Empty<Axis>();
            return;
        }

        var values = data.Select(d => (double)d.TotalRevenue).ToArray();
        var labels = data.Select(d => d.BranchName).ToArray();

        BranchComparisonSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Doanh thu chi nhánh",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#059669")),
                MaxBarWidth = 50
            }
        };

        BranchComparisonXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0
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

        var topCourts = data.Take(8).ToList();
        var values = topCourts.Select(c => c.UtilizationRate).ToArray();
        var labels = topCourts.Select(c => c.CourtName).ToArray();

        OccupancySeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Tỷ lệ lấp đầy (%)",
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse("#D97706")),
                MaxBarWidth = 40
            }
        };

        OccupancyXAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 20
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

    [RelayCommand]
    public void ClearBranchFilter()
    {
        SelectedBranch = null;
        _ = LoadReportsAsync();
    }
}
