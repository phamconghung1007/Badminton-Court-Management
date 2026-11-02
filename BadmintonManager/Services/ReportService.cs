using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;

    public ReportService(IDbContextFactory<AppDbContext> factory, CurrentUserContext currentUser)
    {
        _factory = factory;
        _currentUser = currentUser;
    }

    private int? ResolveAndValidateBranch(int? requestedBranchId)
    {
        if (_currentUser.IsAdmin)
        {
            return requestedBranchId;
        }

        if (_currentUser.IsManager)
        {
            if (requestedBranchId.HasValue && requestedBranchId.Value != _currentUser.BranchId)
            {
                throw new ForbiddenException("Quản lý chỉ có quyền xem báo cáo của cơ sở mình trực thuộc.");
            }
            return _currentUser.BranchId;
        }

        throw new UnauthorizedAccessException("Bạn không có quyền truy cập báo cáo thống kê.");
    }

    private void EnsureAdmin()
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException("Chỉ Quản trị viên mới có quyền xem báo cáo so sánh giữa các cơ sở.");
        }
    }

    public async Task<ReportOverviewSummaryDto> GetOverviewSummaryAsync(int? branchId, DateTime fromDate, DateTime toDate)
    {
        int? effectiveBranchId = ResolveAndValidateBranch(branchId);

        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        if (effectiveBranchId.HasValue)
        {
            query = query.Where(b => b.Court.BranchId == effectiveBranchId.Value);
        }

        var bookings = await query.ToListAsync();

        var courtsQuery = db.Courts.AsNoTracking().Where(c => c.IsActive);
        if (effectiveBranchId.HasValue)
        {
            courtsQuery = courtsQuery.Where(c => c.BranchId == effectiveBranchId.Value);
        }
        int courtCount = await courtsQuery.CountAsync();

        decimal totalRevenue = bookings.Sum(b => b.FinalAmount);
        int totalBookings = bookings.Count;
        double totalBookedMinutes = bookings.Sum(b => (b.EndTime - b.StartTime).TotalMinutes);
        double totalBookedHours = Math.Round(totalBookedMinutes / 60.0, 1);

        int totalDays = Math.Max(1, (int)(toDate.Date - fromDate.Date).TotalDays + 1);
        double totalOpenMinutes = courtCount * totalDays * 18 * 60; // 06:00 to 24:00 = 18h
        double avgRate = totalOpenMinutes > 0 ? Math.Round((totalBookedMinutes / totalOpenMinutes) * 100, 1) : 0;

        return new ReportOverviewSummaryDto(totalRevenue, totalBookings, totalBookedHours, avgRate);
    }

    public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int? branchId, DateTime fromDate, DateTime toDate)
    {
        int? effectiveBranchId = ResolveAndValidateBranch(branchId);

        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        if (effectiveBranchId.HasValue)
        {
            query = query.Where(b => b.Court.BranchId == effectiveBranchId.Value);
        }

        var list = await query.ToListAsync();

        return list
            .GroupBy(b => b.StartTime.Date)
            .Select(g => new DailyRevenueDto(
                g.Key,
                g.Sum(b => b.FinalAmount),
                g.Count()
            ))
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(int? branchId, int year)
    {
        int? effectiveBranchId = ResolveAndValidateBranch(branchId);

        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime.Year == year)
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        if (effectiveBranchId.HasValue)
        {
            query = query.Where(b => b.Court.BranchId == effectiveBranchId.Value);
        }

        var list = await query.ToListAsync();

        return list
            .GroupBy(b => b.StartTime.Month)
            .Select(g => new MonthlyRevenueDto(
                g.Key,
                year,
                g.Sum(b => b.FinalAmount),
                g.Count()
            ))
            .OrderBy(r => r.Month)
            .ToList();
    }

    public async Task<List<CourtUtilizationDto>> GetCourtUtilizationAsync(int? branchId, DateTime fromDate, DateTime toDate)
    {
        int? effectiveBranchId = ResolveAndValidateBranch(branchId);

        await using var db = await _factory.CreateDbContextAsync();
        var courtsQuery = db.Courts.AsNoTracking().Include(c => c.Branch).Where(c => c.IsActive);

        if (effectiveBranchId.HasValue)
        {
            courtsQuery = courtsQuery.Where(c => c.BranchId == effectiveBranchId.Value);
        }

        var courts = await courtsQuery.ToListAsync();

        var bookingsQuery = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        if (effectiveBranchId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(b => b.Court.BranchId == effectiveBranchId.Value);
        }

        var bookings = await bookingsQuery.ToListAsync();

        int totalDays = Math.Max(1, (int)(toDate.Date - fromDate.Date).TotalDays + 1);
        double totalOpenMinutesPerCourt = totalDays * (24 - 6) * 60; // 18 hours/day * 60 = 1080 min/day

        var result = new List<CourtUtilizationDto>();

        foreach (var court in courts)
        {
            var courtBookings = bookings.Where(b => b.CourtId == court.Id).ToList();
            double bookedMinutes = courtBookings.Sum(b => (b.EndTime - b.StartTime).TotalMinutes);
            double rate = totalOpenMinutesPerCourt > 0 ? (bookedMinutes / totalOpenMinutesPerCourt) * 100 : 0;

            result.Add(new CourtUtilizationDto(
                court.Id,
                court.Name,
                court.BranchId,
                court.Branch?.Name ?? $"Chi nhánh #{court.BranchId}",
                Math.Round(rate, 1),
                bookedMinutes,
                totalOpenMinutesPerCourt
            ));
        }

        return result.OrderByDescending(r => r.UtilizationRate).ToList();
    }

    public async Task<List<TopCustomerDto>> GetTopCustomersAsync(int? branchId, int topCount = 10)
    {
        int? effectiveBranchId = ResolveAndValidateBranch(branchId);

        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Court)
            .Where(b => b.CustomerId.HasValue)
            .Where(b => b.Status == BookingStatus.Completed);

        if (effectiveBranchId.HasValue)
        {
            query = query.Where(b => b.Court.BranchId == effectiveBranchId.Value);
        }

        var list = await query.ToListAsync();

        return list
            .GroupBy(b => b.CustomerId!.Value)
            .Select(g => new TopCustomerDto(
                g.Key,
                g.First().Customer?.FullName ?? "N/A",
                g.First().Customer?.Phone ?? "N/A",
                g.Count(),
                g.Sum(b => b.FinalAmount)
            ))
            .OrderByDescending(c => c.TotalSpent)
            .Take(topCount)
            .ToList();
    }

    public async Task<List<BranchRevenueComparisonDto>> GetBranchRevenueComparisonAsync(DateTime fromDate, DateTime toDate)
    {
        EnsureAdmin();

        await using var db = await _factory.CreateDbContextAsync();
        var branches = await db.Branches.AsNoTracking().Where(b => b.IsActive).ToListAsync();

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        var result = new List<BranchRevenueComparisonDto>();
        foreach (var branch in branches)
        {
            var branchBookings = bookings.Where(b => b.Court.BranchId == branch.Id).ToList();
            decimal rev = branchBookings.Sum(b => b.FinalAmount);
            int count = branchBookings.Count;
            result.Add(new BranchRevenueComparisonDto(branch.Id, branch.Name, rev, count));
        }

        return result;
    }

    public async Task<List<BranchOccupancyComparisonDto>> GetBranchOccupancyComparisonAsync(DateTime fromDate, DateTime toDate)
    {
        EnsureAdmin();

        await using var db = await _factory.CreateDbContextAsync();
        var branches = await db.Branches.AsNoTracking().Include(b => b.Courts).Where(b => b.IsActive).ToListAsync();

        var bookings = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        int totalDays = Math.Max(1, (int)(toDate.Date - fromDate.Date).TotalDays + 1);

        var result = new List<BranchOccupancyComparisonDto>();
        foreach (var branch in branches)
        {
            int activeCourts = branch.Courts.Count(c => c.IsActive);
            double totalOpenHours = activeCourts * totalDays * 18.0;

            var branchBookings = bookings.Where(b => b.Court.BranchId == branch.Id).ToList();
            double bookedMinutes = branchBookings.Sum(b => (b.EndTime - b.StartTime).TotalMinutes);
            double bookedHours = Math.Round(bookedMinutes / 60.0, 1);
            double rate = totalOpenHours > 0 ? Math.Round((bookedHours / totalOpenHours) * 100, 1) : 0;

            result.Add(new BranchOccupancyComparisonDto(branch.Id, branch.Name, rate, bookedHours, totalOpenHours));
        }

        return result;
    }
}
