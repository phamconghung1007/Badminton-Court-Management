using System.Collections.Generic;

namespace BadmintonManager.Models.Entities;

public class MembershipTier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public int SortOrder { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
