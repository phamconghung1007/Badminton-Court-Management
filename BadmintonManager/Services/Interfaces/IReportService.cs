using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;

namespace BadmintonManager.Services.Interfaces;

public interface IReportService
{
    Task<ReportOverviewSummaryDto> GetOverviewSummaryAsync(int? branchId, DateTime fromDate, DateTime toDate);
    Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int? branchId, DateTime fromDate, DateTime toDate);
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(int? branchId, int year);
    Task<List<CourtUtilizationDto>> GetCourtUtilizationAsync(int? branchId, DateTime fromDate, DateTime toDate);
    Task<List<TopCustomerDto>> GetTopCustomersAsync(int? branchId, int topCount = 10);
    Task<List<BranchRevenueComparisonDto>> GetBranchRevenueComparisonAsync(DateTime fromDate, DateTime toDate);
    Task<List<BranchOccupancyComparisonDto>> GetBranchOccupancyComparisonAsync(DateTime fromDate, DateTime toDate);
}
