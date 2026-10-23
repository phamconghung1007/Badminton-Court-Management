using System;

namespace BadmintonManager.Models.Entities;

public class PointTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? BookingId { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Customer Customer { get; set; } = null!;
    public virtual Booking? Booking { get; set; }
}
