using System;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Models.Entities;

public class Payment
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentType Type { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.Now;
    public int ReceivedByUserId { get; set; }
    public string? Note { get; set; }

    public virtual Booking Booking { get; set; } = null!;
    public virtual AppUser ReceivedByUser { get; set; } = null!;
}
