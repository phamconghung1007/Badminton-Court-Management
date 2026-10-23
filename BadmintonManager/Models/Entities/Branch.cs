using System;
using System.Collections.Generic;

namespace BadmintonManager.Models.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<Court> Courts { get; set; } = new List<Court>();
    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
