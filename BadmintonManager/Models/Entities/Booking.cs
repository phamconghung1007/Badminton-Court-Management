using System;
using System.Collections.Generic;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Models.Entities;

public class Booking
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int CourtId { get; set; }
    public int? CustomerId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public BookingType BookingType { get; set; } = BookingType.Single;
    public int? FixedScheduleId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal SubTotal { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? ConfirmedByUserId { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Court Court { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual FixedSchedule? FixedSchedule { get; set; }
    public virtual AppUser CreatedByUser { get; set; } = null!;
    public virtual AppUser? ConfirmedByUser { get; set; }
    public virtual AppUser? CancelledByUser { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
