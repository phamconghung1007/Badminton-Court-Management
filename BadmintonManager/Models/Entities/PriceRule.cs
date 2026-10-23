using BadmintonManager.Models.Enums;

namespace BadmintonManager.Models.Entities;

public class PriceRule
{
    public int Id { get; set; }
    public DayType DayType { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
    public decimal PricePerHour { get; set; }
    public bool IsActive { get; set; } = true;
}
