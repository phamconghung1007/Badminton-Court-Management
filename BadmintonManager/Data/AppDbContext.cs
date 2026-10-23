using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BadmintonManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<FixedSchedule> FixedSchedules => Set<FixedSchedule>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
