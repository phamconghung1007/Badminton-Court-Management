using System.Collections.Generic;

namespace BadmintonManager.Models.Entities;

public class Court
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }

    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<FixedSchedule> FixedSchedules { get; set; } = new List<FixedSchedule>();
}
