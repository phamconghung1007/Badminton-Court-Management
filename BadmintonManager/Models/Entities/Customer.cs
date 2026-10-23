using System;
using System.Collections.Generic;

namespace BadmintonManager.Models.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public int MembershipTierId { get; set; }
    public int TotalPoints { get; set; } = 0;
    public string? MembershipCardNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    public virtual AppUser? User { get; set; }
    public virtual MembershipTier MembershipTier { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<FixedSchedule> FixedSchedules { get; set; } = new List<FixedSchedule>();
    public virtual ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
