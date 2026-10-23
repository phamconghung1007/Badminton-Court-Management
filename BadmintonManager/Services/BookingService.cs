using System;
using System.Collections.Generic;
using System.Data;
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

public class BookingService : IBookingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly CurrentUserContext _currentUser;
    private readonly IPricingService _pricingService;
    private readonly ISettingService _settingService;

    public BookingService(
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

    public async Task<List<BookingItemDto>> GetBookingsAsync(int? branchId, DateTime? date, BookingStatus? status = null, int? courtId = null, int? customerId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Court).ThenInclude(c => c.Branch)
            .Include(b => b.Customer)
            .Include(b => b.Payments)
            .AsQueryable();

        // Security filtering (Quy tắc kỹ thuật bắt buộc)
        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue)
            {
                query = query.Where(b => b.Court.BranchId == _currentUser.BranchId.Value);
            }
        }
        else if (_currentUser.IsCustomer)
        {
            if (_currentUser.CustomerId.HasValue)
            {
                query = query.Where(b => b.CustomerId == _currentUser.CustomerId.Value);
            }
            else
            {
                return new List<BookingItemDto>();
            }
        }
        else if (branchId.HasValue && _currentUser.IsAdmin)
        {
            query = query.Where(b => b.Court.BranchId == branchId.Value);
        }

        if (date.HasValue)
        {
            DateTime startOfDay = date.Value.Date;
            DateTime endOfDay = startOfDay.AddDays(1);
            query = query.Where(b => b.StartTime >= startOfDay && b.StartTime < endOfDay);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (courtId.HasValue)
        {
            query = query.Where(b => b.CourtId == courtId.Value);
        }

        if (customerId.HasValue && !_currentUser.IsCustomer)
        {
            query = query.Where(b => b.CustomerId == customerId.Value);
        }

        var list = await query.OrderBy(b => b.StartTime).ToListAsync();

        return list.Select(b => new BookingItemDto
        {
            Id = b.Id,
            Code = b.Code,
            CourtId = b.CourtId,
            CourtName = b.Court.Name,
            BranchId = b.Court.BranchId,
            BranchName = b.Court.Branch.Name,
            CustomerId = b.CustomerId,
            CustomerName = b.Customer?.FullName,
            GuestName = b.GuestName,
            GuestPhone = b.GuestPhone,
            BookingType = b.BookingType,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Status = b.Status,
            SubTotal = b.SubTotal,
            DiscountPercent = b.DiscountPercent,
            DiscountAmount = b.DiscountAmount,
            FinalAmount = b.FinalAmount,
            DepositAmount = b.DepositAmount,
            PaidAmount = b.Payments.Where(p => p.Type != PaymentType.Refund).Sum(p => p.Amount) - b.Payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount),
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    public async Task<BookingItemDto> GetBookingByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var b = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Court).ThenInclude(c => c.Branch)
            .Include(b => b.Customer)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (b == null)
        {
            throw new BusinessException("Không tìm thấy thông tin đặt sân.");
        }

        if (_currentUser.IsManager || _currentUser.IsStaff)
        {
            if (_currentUser.BranchId.HasValue && b.Court.BranchId != _currentUser.BranchId.Value)
            {
                throw new ForbiddenException();
            }
        }
        else if (_currentUser.IsCustomer)
        {
            if (b.CustomerId != _currentUser.CustomerId)
            {
                throw new ForbiddenException();
            }
        }

        return new BookingItemDto
        {
            Id = b.Id,
            Code = b.Code,
            CourtId = b.CourtId,
            CourtName = b.Court.Name,
            BranchId = b.Court.BranchId,
            BranchName = b.Court.Branch.Name,
            CustomerId = b.CustomerId,
            CustomerName = b.Customer?.FullName,
            GuestName = b.GuestName,
            GuestPhone = b.GuestPhone,
            BookingType = b.BookingType,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Status = b.Status,
            SubTotal = b.SubTotal,
            DiscountPercent = b.DiscountPercent,
            DiscountAmount = b.DiscountAmount,
            FinalAmount = b.FinalAmount,
            DepositAmount = b.DepositAmount,
            PaidAmount = b.Payments.Where(p => p.Type != PaymentType.Refund).Sum(p => p.Amount) - b.Payments.Where(p => p.Type == PaymentType.Refund).Sum(p => p.Amount),
            CreatedAt = b.CreatedAt
        };
    }

    public async Task<BookingItemDto> CreateBookingAsync(CreateBookingRequest request)
    {
        // 1. Kiểm tra giờ & quy tắc cơ bản (BR-01, 02, 05)
        ValidateBookingTime(request.StartTime, request.EndTime, request.BookingType == BookingType.WalkIn);

        await using var db = await _factory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var court = await db.Courts.Include(c => c.Branch).FirstOrDefaultAsync(c => c.Id == request.CourtId);
            if (court == null || !court.IsActive)
            {
                throw new BusinessException("Sân không tồn tại hoặc đang tạm ngưng hoạt động.");
            }

            // Role check
            if (_currentUser.IsManager || _currentUser.IsStaff)
            {
                if (_currentUser.BranchId.HasValue && court.BranchId != _currentUser.BranchId.Value)
                {
                    throw new ForbiddenException("Bạn chỉ có thể đặt sân thuộc cơ sở của mình.");
                }
            }

            int? customerId = request.CustomerId;
            if (_currentUser.IsCustomer)
            {
                customerId = _currentUser.CustomerId;
            }

            // Check overlap & 30-min gap rule (BR-03, BR-04)
            await ValidateOverlapAndGapRulesAsync(db, request.CourtId, request.StartTime, request.EndTime, null);

            // Calculate price
            var pricing = await _pricingService.CalculatePriceAsync(request.CourtId, request.StartTime, request.EndTime, customerId);

            // Determine status (A7)
            BookingStatus initialStatus = _currentUser.IsCustomer
                ? BookingStatus.Pending
                : (request.BookingType == BookingType.WalkIn ? BookingStatus.CheckedIn : BookingStatus.Confirmed);

            int userId = _currentUser.Id ?? 1;

            // Generate Booking Code
            string dateStr = request.StartTime.ToString("yyyyMMdd");
            int todaySeq = await db.Bookings.CountAsync(b => b.Code.StartsWith($"BK{dateStr}")) + 1;
            string code = $"BK{dateStr}-{todaySeq:D3}";

            var booking = new Booking
            {
                Code = code,
                CourtId = request.CourtId,
                CustomerId = customerId,
                GuestName = request.GuestName,
                GuestPhone = request.GuestPhone,
                BookingType = request.BookingType,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = initialStatus,
                SubTotal = pricing.SubTotal,
                DiscountPercent = pricing.DiscountPercent,
                DiscountAmount = pricing.DiscountAmount,
                FinalAmount = pricing.FinalAmount,
                DepositAmount = pricing.DepositAmount,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now,
                ConfirmedByUserId = initialStatus != BookingStatus.Pending ? userId : null,
                CheckedInAt = initialStatus == BookingStatus.CheckedIn ? DateTime.Now : null
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetBookingByIdAsync(booking.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw new BusinessException("Khung giờ vừa có người khác đặt, vui lòng chọn lại.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BookingItemDto> UpdateBookingAsync(UpdateBookingRequest request)
    {
        ValidateBookingTime(request.StartTime, request.EndTime, false);

        await using var db = await _factory.CreateDbContextAsync();
        using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var booking = await db.Bookings.Include(b => b.Court).FirstOrDefaultAsync(b => b.Id == request.BookingId);
            if (booking == null) throw new BusinessException("Không tìm thấy đơn đặt sân.");

            if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
            {
                throw new BusinessException("Chỉ có thể chỉnh sửa đơn ở trạng thái Chờ duyệt (Pending) hoặc Đã xác nhận (Confirmed).");
            }

            if (DateTime.Now >= booking.StartTime)
            {
                throw new BusinessException("Không thể chỉnh sửa đơn đặt sân đã đến giờ bắt đầu.");
            }

            // Customer đổi lịch: A9 nếu dưới 24h
            if (_currentUser.IsCustomer)
            {
                if (booking.CustomerId != _currentUser.CustomerId)
                {
                    throw new ForbiddenException();
                }
                if ((booking.StartTime - DateTime.Now).TotalHours < 24)
                {
                    throw new BusinessException("Chỉ được đổi lịch trước giờ chơi ít nhất 24 giờ.");
                }
            }

            await ValidateOverlapAndGapRulesAsync(db, request.CourtId, request.StartTime, request.EndTime, booking.Id);

            var pricing = await _pricingService.CalculatePriceAsync(request.CourtId, request.StartTime, request.EndTime, booking.CustomerId);

            booking.CourtId = request.CourtId;
            booking.StartTime = request.StartTime;
            booking.EndTime = request.EndTime;
            booking.SubTotal = pricing.SubTotal;
            booking.DiscountPercent = pricing.DiscountPercent;
            booking.DiscountAmount = pricing.DiscountAmount;
            booking.FinalAmount = pricing.FinalAmount;
            booking.DepositAmount = pricing.DepositAmount;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetBookingByIdAsync(booking.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConfirmBookingAsync(int bookingId)
    {
        if (_currentUser.IsCustomer)
        {
            throw new ForbiddenException("Khách hàng không có quyền duyệt đơn đặt sân.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking == null) throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (booking.Status != BookingStatus.Pending)
        {
            throw new BusinessException("Chỉ có thể duyệt đơn ở trạng thái Chờ duyệt (Pending).");
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedByUserId = _currentUser.Id ?? 1;

        await db.SaveChangesAsync();
    }

    public async Task CheckInAsync(int bookingId)
    {
        if (_currentUser.IsCustomer)
        {
            throw new ForbiddenException("Khách hàng không có quyền thực hiện check-in.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking == null) throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BusinessException("Chỉ đơn đã xác nhận (Confirmed) mới có thể Check-in.");
        }

        booking.Status = BookingStatus.CheckedIn;
        booking.CheckedInAt = DateTime.Now;

        await db.SaveChangesAsync();
    }

    public async Task MarkNoShowAsync(int bookingId)
    {
        if (_currentUser.IsCustomer)
        {
            throw new ForbiddenException();
        }

        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings.FindAsync(bookingId);
        if (booking == null) throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BusinessException("Chỉ có thể đánh dấu Vắng mặt (No-Show) cho đơn đã xác nhận.");
        }

        booking.Status = BookingStatus.NoShow;
        await db.SaveChangesAsync();
    }

    public async Task CancelBookingAsync(CancelBookingRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId);

        if (booking == null) throw new BusinessException("Không tìm thấy đơn đặt sân.");

        if (booking.Status != BookingStatus.Pending && booking.Status != BookingStatus.Confirmed)
        {
            throw new BusinessException("Chỉ có thể hủy đơn ở trạng thái Pending hoặc Confirmed.");
        }

        if (_currentUser.IsCustomer && booking.CustomerId != _currentUser.CustomerId)
        {
            throw new ForbiddenException();
        }

        int userId = _currentUser.Id ?? 1;
        DateTime now = DateTime.Now;
        double hoursUntilStart = (booking.StartTime - now).TotalHours;

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.CancelledByUserId = userId;
        booking.CancelReason = request.Reason;

        // BR-14: Hủy trước giờ chơi >= 24h: hoàn cọc. Dưới 24h: mất cọc.
        int freeCancelHours = await _settingService.GetIntSettingAsync(Constants.Settings.FreeCancelHours, 24);
        if (hoursUntilStart >= freeCancelHours)
        {
            decimal paidDeposit = booking.Payments
                .Where(p => p.Type == PaymentType.Deposit)
                .Sum(p => p.Amount);

            if (paidDeposit > 0)
            {
                var refund = new Payment
                {
                    BookingId = booking.Id,
                    Amount = paidDeposit,
                    Method = PaymentMethod.BankTransfer,
                    Type = PaymentType.Refund,
                    ReferenceNo = $"REF-{booking.Code}",
                    PaidAt = now,
                    ReceivedByUserId = userId,
                    Note = $"Hoàn tiền cọc do hủy trước {freeCancelHours} giờ"
                };
                db.Payments.Add(refund);
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<FixedScheduleResultDto> CreateFixedScheduleAsync(CreateFixedScheduleRequest request)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var schedule = new FixedSchedule
        {
            CustomerId = request.CustomerId,
            CourtId = request.CourtId,
            DayOfWeek = request.DayOfWeek,
            StartMinute = request.StartMinute,
            EndMinute = request.EndMinute,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.FixedSchedules.Add(schedule);
        await db.SaveChangesAsync();

        int createdCount = 0;
        var skippedDates = new List<DateOnly>();

        // Generate for up to 4 weeks ahead (BR-18)
        DateOnly current = request.ValidFrom;
        while (current <= request.ValidTo)
        {
            if ((int)current.DayOfWeek == request.DayOfWeek)
            {
                DateTime start = current.ToDateTime(TimeOnly.MinValue).AddMinutes(request.StartMinute);
                DateTime end = current.ToDateTime(TimeOnly.MinValue).AddMinutes(request.EndMinute);

                try
                {
                    // Check overlap
                    bool hasOverlap = await db.Bookings.AnyAsync(b =>
                        b.CourtId == request.CourtId &&
                        b.Status != BookingStatus.Cancelled &&
                        b.StartTime < end && start < b.EndTime);

                    if (hasOverlap)
                    {
                        skippedDates.Add(current);
                    }
                    else
                    {
                        var pricing = await _pricingService.CalculatePriceAsync(request.CourtId, start, end, request.CustomerId);
                        string dateStr = start.ToString("yyyyMMdd");
                        int count = await db.Bookings.CountAsync(b => b.Code.StartsWith($"BK{dateStr}")) + 1;

                        var b = new Booking
                        {
                            Code = $"BK{dateStr}-{count:D3}",
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
                            CreatedByUserId = _currentUser.Id ?? 1,
                            CreatedAt = DateTime.Now,
                            ConfirmedByUserId = _currentUser.Id ?? 1
                        };
                        db.Bookings.Add(b);
                        createdCount++;
                    }
                }
                catch
                {
                    skippedDates.Add(current);
                }
            }
            current = current.AddDays(1);
        }

        await db.SaveChangesAsync();

        return new FixedScheduleResultDto
        {
            CreatedCount = createdCount,
            SkippedDates = skippedDates
        };
    }

    public async Task CancelFixedScheduleAsync(int scheduleId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var schedule = await db.FixedSchedules.FindAsync(scheduleId);
        if (schedule == null) throw new BusinessException("Không tìm thấy lịch cố định.");

        schedule.IsActive = false;

        // Cancel future un-checked-in bookings (BR-19)
        DateTime now = DateTime.Now;
        var futureBookings = await db.Bookings
            .Where(b => b.FixedScheduleId == scheduleId && b.StartTime > now && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .ToListAsync();

        foreach (var b in futureBookings)
        {
            b.Status = BookingStatus.Cancelled;
            b.CancelledAt = now;
            b.CancelledByUserId = _currentUser.Id ?? 1;
            b.CancelReason = "Hủy theo toàn bộ lịch cố định";
        }

        await db.SaveChangesAsync();
    }

    private static void ValidateBookingTime(DateTime start, DateTime end, bool isWalkIn)
    {
        // BR-01: Giờ đặt nằm trong [06:00, 24:00], bắt đầu và kết thúc phải là phút 00 hoặc 30
        if (start.Minute != 0 && start.Minute != 30)
        {
            throw new BusinessException("Giờ bắt đầu phải là mốc đúng giờ (00 phút) hoặc rưỡi (30 phút).");
        }
        if (end.Minute != 0 && end.Minute != 30)
        {
            throw new BusinessException("Giờ kết thúc phải là mốc đúng giờ (00 phút) hoặc rưỡi (30 phút).");
        }

        int startMin = start.Hour * 60 + start.Minute;
        int endMin = (end.Date > start.Date || (end.Hour == 0 && end.Minute == 0)) ? 1440 : (end.Hour * 60 + end.Minute);

        if (startMin < 360 || endMin > 1440)
        {
            throw new BusinessException("Thời gian đặt sân phải nằm trong khung giờ hoạt động (06:00 - 24:00).");
        }

        // BR-02: Thời lượng trong [60, 240] phút
        int duration = (int)(end - start).TotalMinutes;
        if (duration < 60 || duration > 240)
        {
            throw new BusinessException("Thời lượng đặt sân phải từ 60 phút đến tối đa 4 giờ (240 phút).");
        }

        // BR-05: Không đặt trong quá khứ
        if (!isWalkIn && start < DateTime.Now.AddMinutes(-5))
        {
            throw new BusinessException("Không thể đặt sân trong quá khứ.");
        }
    }

    private static async Task ValidateOverlapAndGapRulesAsync(AppDbContext db, int courtId, DateTime s, DateTime e, int? excludeBookingId)
    {
        DateTime dayStart = s.Date.AddHours(6);   // 06:00
        DateTime dayEnd = s.Date.AddDays(1);      // 24:00 (= 00:00 hôm sau)

        var existingBookings = await db.Bookings
            .Where(b => b.CourtId == courtId &&
                        b.Status != BookingStatus.Cancelled &&
                        b.StartTime >= s.Date &&
                        b.StartTime < s.Date.AddDays(1) &&
                        (!excludeBookingId.HasValue || b.Id != excludeBookingId.Value))
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        // BR-03: Kiểm tra chồng lấn thời gian (a.Start < b.End && b.Start < a.End)
        bool hasOverlap = existingBookings.Any(b => b.StartTime < e && s < b.EndTime);
        if (hasOverlap)
        {
            throw new BusinessException("Khung giờ này đã có người đặt trên sân đã chọn.");
        }

        // BR-04: Quy tắc khoảng trống 30 phút
        // prevEnd = max EndTime <= s (không có thì 06:00)
        DateTime prevEnd = dayStart;
        var prevs = existingBookings.Where(b => b.EndTime <= s).ToList();
        if (prevs.Any())
        {
            prevEnd = prevs.Max(b => b.EndTime);
        }

        // nextStart = min StartTime >= e (không có thì 24:00)
        DateTime nextStart = dayEnd;
        var nexts = existingBookings.Where(b => b.StartTime >= e).ToList();
        if (nexts.Any())
        {
            nextStart = nexts.Min(b => b.StartTime);
        }

        int gapBefore = (int)(s - prevEnd).TotalMinutes;
        int gapAfter = (int)(nextStart - e).TotalMinutes;

        if (gapBefore == 30 || gapAfter == 30)
        {
            throw new BusinessException("Quy tắc khoảng trống: Không được để lại khoảng trống đúng 30 phút giữa các lịch đặt (Hãy đặt sát giờ hoặc chừa từ 1 giờ trở lên).");
        }
    }
}
