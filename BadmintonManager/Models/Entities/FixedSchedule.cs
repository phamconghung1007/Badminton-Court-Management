using System;
using System.Collections.Generic;

namespace BadmintonManager.Models.Entities;

public class FixedSchedule
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int CourtId { get; set; }
    public int DayOfWeek { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Customer Customer { get; set; } = null!;
    public virtual Court Court { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
