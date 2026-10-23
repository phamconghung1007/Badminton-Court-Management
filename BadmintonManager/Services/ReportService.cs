using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
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

    public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int? branchId, DateTime fromDate, DateTime toDate)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        int? effectiveBranchId = _currentUser.IsManager ? _currentUser.BranchId : branchId;
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
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court)
            .Where(b => b.StartTime.Year == year)
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

        int? effectiveBranchId = _currentUser.IsManager ? _currentUser.BranchId : branchId;
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
        await using var db = await _factory.CreateDbContextAsync();
        var courtsQuery = db.Courts.AsNoTracking().Include(c => c.Branch).Where(c => c.IsActive);

        int? effectiveBranchId = _currentUser.IsManager ? _currentUser.BranchId : branchId;
        if (effectiveBranchId.HasValue)
        {
            courtsQuery = courtsQuery.Where(c => c.BranchId == effectiveBranchId.Value);
        }

        var courts = await courtsQuery.ToListAsync();

        var bookingsQuery = db.Bookings
            .AsNoTracking()
            .Where(b => b.StartTime >= fromDate.Date && b.StartTime < toDate.Date.AddDays(1))
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.CheckedIn || b.Status == BookingStatus.Confirmed);

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
                court.Branch.Name,
                Math.Round(rate, 1),
                bookedMinutes,
                totalOpenMinutesPerCourt
            ));
        }

        return result;
    }

    public async Task<List<TopCustomerDto>> GetTopCustomersAsync(int? branchId, int topCount = 10)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Court)
            .Where(b => b.CustomerId.HasValue)
            .Where(b => b.Status == BookingStatus.Completed);

        int? effectiveBranchId = _currentUser.IsManager ? _currentUser.BranchId : branchId;
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
}
