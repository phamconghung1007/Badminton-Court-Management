using System;
using System.Collections.Generic;
using BadmintonManager.Models.Enums;

namespace BadmintonManager.Models.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }

    public virtual Branch? Branch { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Booking> ConfirmedBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Booking> CancelledBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Payment> ReceivedPayments { get; set; } = new List<Payment>();
}
