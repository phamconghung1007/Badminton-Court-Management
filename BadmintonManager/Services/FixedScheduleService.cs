using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BadmintonManager.Common;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Data;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
using BadmintonManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Services;

public class FixedScheduleService : IFixedScheduleService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IPricingService _pricingService;
    private readonly ISettingService _settingService;

    public FixedScheduleService(
        IDbContextFactory<AppDbContext> factory,
        CurrentUserContext currentUser,
        IPricingService pricingService,
        ISettingService settingService)
    {
        _factory = factory;
        _currentUser = currentUser;
        _pricingService = pricingService;
        _settingService = settingService;
    }

    public async Task<FixedScheduleResultDto> CreateFixedScheduleAsync(CreateFixedScheduleRequest request)
    {
        if (request.StartMinute >= request.EndMinute)
            throw new BusinessException("Giờ kết thúc phải lớn hơn giờ bắt đầu.");

        if (request.StartMinute % 30 != 0 || request.EndMinute % 30 != 0)
            throw new BusinessException("Khung giờ bắt đầu và kết thúc phải đúng mốc 00 hoặc 30 phút.");

        int duration = request.EndMinute - request.StartMinute;
        int minDuration = await _settingService.GetIntSettingAsync(Constants.Settings.MinDurationMinutes, 60);
        int maxDuration = await _settingService.GetIntSettingAsync(Constants.Settings.MaxDurationMinutes, 240);

        if (duration < minDuration || duration > maxDuration)
            throw new BusinessException($"Thời lượng đặt sân phải từ {minDuration} phút đến {maxDuration} phút (Hiện tại: {duration} phút).");

        int openMinute = await _settingService.GetIntSettingAsync(Constants.Settings.OpenMinute, 360);
        int closeMinute = await _settingService.GetIntSettingAsync(Constants.Settings.CloseMinute, 1440);

        if (request.StartMinute < openMinute || request.EndMinute > closeMinute)
            throw new BusinessException($"Giờ đặt phải nằm trong khung giờ hoạt động ({openMinute / 60:D2}:00 - {closeMinute / 60:D2}:00).");

        await using var db = await _factory.CreateDbContextAsync();

        // 1. Kiểm tra sân & phân quyền cơ sở
        var court = await db.Courts.Include(c => c.Branch).FirstOrDefaultAsync(c => c.Id == request.CourtId);
        if (court == null || !court.IsActive)
            throw new BusinessException("Sân không tồn tại hoặc đang tạm ngưng hoạt động.");

        if ((_currentUser.IsManager || _currentUser.IsStaff) && _currentUser.BranchId.HasValue && court.BranchId != _currentUser.BranchId.Value)
        {
            throw new ForbiddenException("Bạn chỉ có quyền tạo lịch cố định cho sân thuộc chi nhánh của mình.");
        }

        if (_currentUser.IsCustomer && request.CustomerId != _currentUser.CustomerId)
        {
            throw new ForbiddenException("Bạn chỉ có quyền tạo lịch cố định cho chính mình.");
        }

        // 2. Kiểm tra khách hàng
        var customer = await db.Customers.Include(c => c.MembershipTier).FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null || !customer.IsActive)
            throw new BusinessException("Khách hàng không tồn tại hoặc tài khoản đã bị khóa.");

        // 3. Xác định số tuần cần sinh (mặc định 4 tuần theo A11 & BR-18)
        int defaultWeeks = await _settingService.GetIntSettingAsync(Constants.Settings.FixedWeeksAhead, 4);
        int weeksAhead = request.NumberOfWeeks ?? defaultWeeks;
        if (weeksAhead < 1) weeksAhead = 1;
        if (weeksAhead > 12) weeksAhead = 12;

        DateOnly validFrom = request.ValidFrom == default ? DateOnly.FromDateTime(DateTime.Today) : request.ValidFrom;
        DateOnly validTo = request.ValidTo ?? validFrom.AddDays(7 * weeksAhead);

        var schedule = new FixedSchedule
        {
            CustomerId = request.CustomerId,
            CourtId = request.CourtId,
            DayOfWeek = request.DayOfWeek,
            StartMinute = request.StartMinute,
            EndMinute = request.EndMinute,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.FixedSchedules.Add(schedule);
        await db.SaveChangesAsync();

        // 4. Sinh booking cho các tuần tới (A11, BR-18)
        var createdBookingIds = new List<int>();
        var skippedDates = new List<DateOnly>();
        var skippedDetails = new List<SkippedScheduleDateDto>();

        DateOnly cur = validFrom;
        // Di chuyển đến ngày đầu tiên có thứ khớp với DayOfWeek
        while ((int)cur.DayOfWeek != request.DayOfWeek)
        {
            cur = cur.AddDays(1);
        }

        int weeksProcessed = 0;
        int userId = _currentUser.Id ?? 1;

        while (cur <= validTo && weeksProcessed < weeksAhead)
        {
            weeksProcessed++;
            DateTime start = cur.ToDateTime(TimeOnly.MinValue).AddMinutes(request.StartMinute);
            DateTime end = cur.ToDateTime(TimeOnly.MinValue).AddMinutes(request.EndMinute);

            string dayName = cur.DayOfWeek switch
            {
                System.DayOfWeek.Sunday => "Chủ Nhật",
                System.DayOfWeek.Monday => "Thứ Hai",
                System.DayOfWeek.Tuesday => "Thứ Ba",
                System.DayOfWeek.Wednesday => "Thứ Tư",
                System.DayOfWeek.Thursday => "Thứ Năm",
                System.DayOfWeek.Friday => "Thứ Sáu",
                System.DayOfWeek.Saturday => "Thứ Bảy",
                _ => cur.DayOfWeek.ToString()
            };

            // BR-05: Kiểm tra quá khứ
            if (start < DateTime.Now)
            {
                skippedDates.Add(cur);
                skippedDetails.Add(new SkippedScheduleDateDto(cur, dayName, "Thời gian chơi trong quá khứ."));
                cur = cur.AddDays(7);
                continue;
            }

            // BR-03: Kiểm tra trùng giờ
            var overlapBooking = await db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.CourtId == request.CourtId &&
                                          b.Status != BookingStatus.Cancelled &&
                                          b.StartTime < end && start < b.EndTime);
            if (overlapBooking != null)
            {
                skippedDates.Add(cur);
                skippedDetails.Add(new SkippedScheduleDateDto(cur, dayName, $"Trùng với lịch đặt #{overlapBooking.Code} ({overlapBooking.StartTime:HH:mm} - {overlapBooking.EndTime:HH:mm})."));
                cur = cur.AddDays(7);
                continue;
            }

            // BR-04: Kiểm tra quy tắc khoảng trống 30 phút
            bool gapViolated = false;
            string gapReason = string.Empty;

            DateTime dayStart = start.Date.AddMinutes(openMinute);
            DateTime dayEnd = start.Date.AddMinutes(closeMinute);

            var sameDayBookings = await db.Bookings
                .AsNoTracking()
                .Where(b => b.CourtId == request.CourtId &&
                            b.Status != BookingStatus.Cancelled &&
                            b.StartTime >= start.Date && b.StartTime < start.Date.AddDays(1))
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            DateTime prevEnd = dayStart;
            var prevBooking = sameDayBookings.Where(b => b.EndTime <= start).OrderByDescending(b => b.EndTime).FirstOrDefault();
            if (prevBooking != null) prevEnd = prevBooking.EndTime;

            DateTime nextStart = dayEnd;
            var nextBooking = sameDayBookings.Where(b => b.StartTime >= end).OrderBy(b => b.StartTime).FirstOrDefault();
            if (nextBooking != null) nextStart = nextBooking.StartTime;

            if ((start - prevEnd).TotalMinutes == 30)
            {
                gapViolated = true;
                gapReason = "Vi phạm khoảng trống: Để lại khoảng trống đúng 30 phút so với lịch liền trước.";
            }
            else if ((nextStart - end).TotalMinutes == 30)
            {
                gapViolated = true;
                gapReason = "Vi phạm khoảng trống: Để lại khoảng trống đúng 30 phút so với lịch liền sau.";
            }

            if (gapViolated)
            {
                skippedDates.Add(cur);
                skippedDetails.Add(new SkippedScheduleDateDto(cur, dayName, gapReason));
                cur = cur.AddDays(7);
                continue;
            }

            // Tính giá & tạo booking
            var pricing = await _pricingService.CalculatePriceAsync(request.CourtId, start, end, request.CustomerId);
            string dateStr = start.ToString("yyyyMMdd");
            int countToday = await db.Bookings.CountAsync(b => b.Code.StartsWith($"BK{dateStr}")) + 1;
            string code;
            int seq = countToday;
            do
            {
                code = $"BK{dateStr}-{seq:D3}";
                seq++;
            } while (await db.Bookings.AnyAsync(b => b.Code == code));

            var booking = new Booking
            {
                Code = code,
                CourtId = request.CourtId,
                CustomerId = request.CustomerId,
                BookingType = BookingType.Fixed,
                FixedScheduleId = schedule.Id,
                StartTime = start,
                EndTime = end,
                Status = BookingStatus.Confirmed,
                SubTotal = pricing.SubTotal,
                DiscountPercent = pricing.DiscountPercent,
                DiscountAmount = pricing.DiscountAmount,
                FinalAmount = pricing.FinalAmount,
                DepositAmount = pricing.DepositAmount,
                CreatedByUserId = userId,
                ConfirmedByUserId = userId,
                CreatedAt = DateTime.Now
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            createdBookingIds.Add(booking.Id);

            cur = cur.AddDays(7);
        }

        string message = createdBookingIds.Count == weeksProcessed
            ? $"Tạo lịch cố định thành công trọn vẹn cả {createdBookingIds.Count} tuần!"
            : (createdBookingIds.Count > 0
                ? $"Đã tạo thành công {createdBookingIds.Count}/{weeksProcessed} tuần. Có {skippedDates.Count} tuần bị bỏ qua do trùng lịch hoặc vi phạm khoảng trống."
                : $"Không thể tạo buổi nào do toàn bộ {weeksProcessed} tuần đều bị trùng lịch hoặc vi phạm quy tắc.");

        return new FixedScheduleResultDto
        {
            ScheduleId = schedule.Id,
            TotalWeeks = weeksProcessed,
            CreatedCount = createdBookingIds.Count,
            CreatedBookingIds = createdBookingIds,
            SkippedDates = skippedDates,
            SkippedDetails = skippedDetails,
            Message = message
        };
    }

    public async Task CancelFixedScheduleAsync(int scheduleId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var schedule = await db.FixedSchedules
            .Include(s => s.Court)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null) throw new BusinessException("Không tìm thấy lịch cố định.");

        // Phân quyền cơ sở
        if ((_currentUser.IsManager || _currentUser.IsStaff) && _currentUser.BranchId.HasValue && schedule.Court.BranchId != _currentUser.BranchId.Value)
        {
            throw new ForbiddenException("Bạn chỉ có quyền hủy lịch cố định thuộc chi nhánh của mình.");
        }

        if (_currentUser.IsCustomer && schedule.CustomerId != _currentUser.CustomerId)
        {
            throw new ForbiddenException("Bạn không có quyền hủy lịch cố định của khách hàng khác.");
        }

        schedule.IsActive = false;

        // BR-19: Hủy các booking tương lai chưa check-in thuộc lịch đó
        DateTime now = DateTime.Now;
        var futureBookings = await db.Bookings
            .Where(b => b.FixedScheduleId == scheduleId &&
                        b.StartTime > now &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync();

        int userId = _currentUser.Id ?? 1;
        foreach (var b in futureBookings)
        {
            b.Status = BookingStatus.Cancelled;
            b.CancelledAt = now;
            b.CancelledByUserId = userId;
            b.CancelReason = "Hủy theo toàn bộ lịch cố định";
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<FixedScheduleDto>> GetFixedSchedulesAsync(int? branchId = null, int? customerId = null, bool? isActive = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.FixedSchedules
            .AsNoTracking()
            .Include(s => s.Court).ThenInclude(c => c.Branch)
            .Include(s => s.Customer)
            .Include(s => s.Bookings)
            .AsQueryable();

        // Phân quyền cơ sở
        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue)
            {
                query = query.Where(s => s.Court.BranchId == _currentUser.BranchId.Value);
            }
        }
        else if (_currentUser.IsCustomer)
        {
            query = query.Where(s => s.CustomerId == _currentUser.CustomerId);
        }
        else if (branchId.HasValue)
        {
            query = query.Where(s => s.Court.BranchId == branchId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        DateTime now = DateTime.Now;

        return await query.OrderByDescending(s => s.CreatedAt).Select(s => new FixedScheduleDto
        {
            Id = s.Id,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer.FullName,
            CustomerPhone = s.Customer.Phone,
            CourtId = s.CourtId,
            CourtName = s.Court.Name,
            BranchId = s.Court.BranchId,
            BranchName = s.Court.Branch.Name,
            DayOfWeek = s.DayOfWeek,
            StartMinute = s.StartMinute,
            EndMinute = s.EndMinute,
            ValidFrom = s.ValidFrom,
            ValidTo = s.ValidTo,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            TotalBookingsCount = s.Bookings.Count,
            FutureBookingsCount = s.Bookings.Count(b => b.StartTime > now && b.Status != BookingStatus.Cancelled)
        }).ToListAsync();
    }

    public async Task<FixedScheduleDto?> GetFixedScheduleByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var s = await db.FixedSchedules
            .AsNoTracking()
            .Include(s => s.Court).ThenInclude(c => c.Branch)
            .Include(s => s.Customer)
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (s == null) return null;

        DateTime now = DateTime.Now;

        return new FixedScheduleDto
        {
            Id = s.Id,
            CustomerId = s.CustomerId,
            CustomerName = s.Customer.FullName,
            CustomerPhone = s.Customer.Phone,
            CourtId = s.CourtId,
            CourtName = s.Court.Name,
            BranchId = s.Court.BranchId,
            BranchName = s.Court.Branch.Name,
            DayOfWeek = s.DayOfWeek,
            StartMinute = s.StartMinute,
            EndMinute = s.EndMinute,
            ValidFrom = s.ValidFrom,
            ValidTo = s.ValidTo,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            TotalBookingsCount = s.Bookings.Count,
            FutureBookingsCount = s.Bookings.Count(b => b.StartTime > now && b.Status != BookingStatus.Cancelled)
        };
    }
}
