using System;
using System.Collections.Generic;
using System.Linq;
using BadmintonManager.DTOs;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Common;

public static class ScheduleMatrixHelper
{
    public const int OpenMinute = 360;      // 06:00
    public const int CloseMinute = 1440;    // 24:00
    public const int SlotInterval = 30;     // 30 minutes
    public const int TotalSlots = 36;       // (1440 - 360) / 30 = 36 slots

    public static List<ScheduleTimeSlot> GenerateTimeSlots()
    {
        var slots = new List<ScheduleTimeSlot>(TotalSlots);
        for (int i = 0; i < TotalSlots; i++)
        {
            int start = OpenMinute + i * SlotInterval;
            int end = start + SlotInterval;

            int startHour = start / 60;
            int startMin = start % 60;
            int endHour = end / 60;
            int endMin = end % 60;

            string startStr = $"{startHour:D2}:{startMin:D2}";
            string endStr = (endHour == 24 && endMin == 0) ? "24:00" : $"{endHour:D2}:{endMin:D2}";

            slots.Add(new ScheduleTimeSlot
            {
                SlotIndex = i,
                StartMinute = start,
                EndMinute = end,
                TimeDisplay = startStr,
                TimeRangeDisplay = $"{startStr} - {endStr}"
            });
        }
        return slots;
    }

    public static int CalculateRowIndex(DateTime startTime)
    {
        int startMin = startTime.Hour * 60 + startTime.Minute;
        int rowIndex = (startMin - OpenMinute) / SlotInterval;
        if (rowIndex < 0) rowIndex = 0;
        if (rowIndex >= TotalSlots) rowIndex = TotalSlots - 1;
        return rowIndex;
    }

    public static int CalculateRowSpan(DateTime startTime, DateTime endTime)
    {
        int startMin = startTime.Hour * 60 + startTime.Minute;
        int endMin = (endTime.Date > startTime.Date || (endTime.Hour == 0 && endTime.Minute == 0))
            ? CloseMinute
            : (endTime.Hour * 60 + endTime.Minute);

        int span = (endMin - startMin) / SlotInterval;
        if (span < 1) span = 1;
        return span;
    }

    public static (string BackgroundHex, string BorderHex, string TextHex, string StatusText) GetStatusStyle(BookingStatus status)
    {
        return status switch
        {
            BookingStatus.Pending => ("#F59E0B", "#D97706", "#FFFFFF", "Chờ duyệt"),
            BookingStatus.Confirmed => ("#2563EB", "#1D4ED8", "#FFFFFF", "Đã xác nhận"),
            BookingStatus.CheckedIn => ("#10B981", "#059669", "#FFFFFF", "Đang chơi"),
            BookingStatus.Completed => ("#6B7280", "#4B5563", "#FFFFFF", "Hoàn thành"),
            BookingStatus.NoShow => ("#EF4444", "#DC2626", "#FFFFFF", "Vắng mặt"),
            BookingStatus.Cancelled => ("#E5E7EB", "#9CA3AF", "#6B7280", "Đã hủy"),
            _ => ("#3B82F6", "#2563EB", "#FFFFFF", status.ToString())
        };
    }

    public static ScheduleBookingBlock BuildBookingBlock(BookingItemDto booking, int columnIndex)
    {
        int rowIndex = CalculateRowIndex(booking.StartTime);
        int rowSpan = CalculateRowSpan(booking.StartTime, booking.EndTime);
        var (bg, border, text, statusText) = GetStatusStyle(booking.Status);

        string customerName = !string.IsNullOrWhiteSpace(booking.CustomerName)
            ? booking.CustomerName
            : (!string.IsNullOrWhiteSpace(booking.GuestName) ? $"{booking.GuestName} (Vãng lai)" : "Khách đặt");

        string phone = !string.IsNullOrWhiteSpace(booking.GuestPhone) ? booking.GuestPhone : string.Empty;

        return new ScheduleBookingBlock
        {
            BookingId = booking.Id,
            Code = booking.Code,
            CourtId = booking.CourtId,
            CourtName = booking.CourtName,
            ColumnIndex = columnIndex,
            RowIndex = rowIndex,
            RowSpan = rowSpan,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            StatusText = statusText,
            CustomerDisplayName = customerName,
            CustomerPhone = phone,
            TimeRangeText = $"{booking.StartTime:HH:mm} - {(booking.EndTime.Hour == 0 && booking.EndTime.Minute == 0 && booking.EndTime.Date > booking.StartTime.Date ? "24:00" : booking.EndTime.ToString("HH:mm"))}",
            FinalAmount = booking.FinalAmount,
            DepositAmount = booking.DepositAmount,
            PaidAmount = booking.PaidAmount,
            BackgroundHex = bg,
            BorderHex = border,
            TextHex = text
        };
    }

    public static List<ScheduleEmptySlot> BuildEmptySlots(
        List<ScheduleCourtHeader> courts,
        List<ScheduleBookingBlock> activeBookings,
        DateTime date)
    {
        var emptySlots = new List<ScheduleEmptySlot>();
        var courtDict = courts.ToDictionary(c => c.CourtId);

        // Pre-map occupied slots: (CourtId, SlotIndex)
        var occupied = new HashSet<(int CourtId, int SlotIndex)>();
        foreach (var b in activeBookings.Where(b => b.Status != BookingStatus.Cancelled))
        {
            for (int s = b.RowIndex; s < b.RowIndex + b.RowSpan && s < TotalSlots; s++)
            {
                occupied.Add((b.CourtId, s));
            }
        }

        for (int s = 0; s < TotalSlots; s++)
        {
            int startMin = OpenMinute + s * SlotInterval;
            int endMin = startMin + SlotInterval;

            DateTime slotStart = date.Date.AddMinutes(startMin);
            DateTime slotEnd = date.Date.AddMinutes(endMin);

            string startStr = $"{startMin / 60:D2}:{startMin % 60:D2}";
            string endStr = (endMin == 1440) ? "24:00" : $"{endMin / 60:D2}:{endMin % 60:D2}";
            string timeRange = $"{startStr} - {endStr}";

            foreach (var court in courts)
            {
                if (!occupied.Contains((court.CourtId, s)))
                {
                    emptySlots.Add(new ScheduleEmptySlot
                    {
                        CourtId = court.CourtId,
                        CourtName = court.CourtName,
                        ColumnIndex = court.ColumnIndex,
                        RowIndex = s,
                        SlotStartTime = slotStart,
                        SlotEndTime = slotEnd,
                        TimeRangeText = timeRange
                    });
                }
            }
        }

        return emptySlots;
    }
}
