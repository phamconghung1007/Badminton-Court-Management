using System;
using System.Collections.Generic;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.DTOs;

public class ScheduleTimeSlot
{
    public int SlotIndex { get; set; }           // 0..35
    public int StartMinute { get; set; }        // 360, 390, ..., 1410
    public int EndMinute { get; set; }          // 390, 420, ..., 1440
    public string TimeDisplay { get; set; } = string.Empty;       // "06:00", "06:30"
    public string TimeRangeDisplay { get; set; } = string.Empty;  // "06:00 - 06:30"
}

public class ScheduleCourtHeader
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }        // 0..5 (0-based)
    public bool IsActive { get; set; }
}

public class ScheduleBookingBlock
{
    public int BookingId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }        // 0-based index of court
    public int RowIndex { get; set; }           // 0..35 slot index
    public int RowSpan { get; set; }            // count of 30-min slots
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string CustomerDisplayName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string TimeRangeText { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string BackgroundHex { get; set; } = "#2563EB";
    public string BorderHex { get; set; } = "#1D4ED8";
    public string TextHex { get; set; } = "#FFFFFF";

    public bool IsPending => Status == BookingStatus.Pending;
    public bool IsConfirmed => Status == BookingStatus.Confirmed;
    public bool IsCheckedIn => Status == BookingStatus.CheckedIn;
    public bool IsCompleted => Status == BookingStatus.Completed;
}

public class ScheduleEmptySlot
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }        // 0-based
    public int RowIndex { get; set; }           // 0..35
    public DateTime SlotStartTime { get; set; }
    public DateTime SlotEndTime { get; set; }
    public string TimeRangeText { get; set; } = string.Empty;
}
