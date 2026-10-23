using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Services.Interfaces;

public interface IBookingService
{
    Task<List<BookingItemDto>> GetBookingsAsync(int? branchId, DateTime? date, BookingStatus? status = null, int? courtId = null, int? customerId = null);
    Task<BookingItemDto> GetBookingByIdAsync(int id);
    Task<BookingItemDto> CreateBookingAsync(CreateBookingRequest request);
    Task<BookingItemDto> UpdateBookingAsync(UpdateBookingRequest request);
    Task ConfirmBookingAsync(int bookingId);
    Task CheckInAsync(int bookingId);
    Task MarkNoShowAsync(int bookingId);
    Task CancelBookingAsync(CancelBookingRequest request);
    Task<FixedScheduleResultDto> CreateFixedScheduleAsync(CreateFixedScheduleRequest request);
    Task CancelFixedScheduleAsync(int scheduleId);
}
