using System.Threading;
using System.Threading.Tasks;
using BadmintonManager.Common.Exceptions;
using BadmintonManager.Models.Entities;
using BadmintonManager.Models.Enums;
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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateBusinessRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ValidateBusinessRules();
        return base.SaveChanges();
    }

    private void ValidateBusinessRules()
    {
        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                // Ràng buộc nghiệp vụ Section 4.1: Manager/Staff bắt buộc có BranchId; Admin/Customer thì null
                if ((entry.Entity.Role == UserRole.Manager || entry.Entity.Role == UserRole.Staff) && !entry.Entity.BranchId.HasValue)
                {
                    throw new BusinessException($"Tài khoản {entry.Entity.Role} '{entry.Entity.Username}' bắt buộc phải được gắn với một Cơ sở (BranchId).");
                }

                if ((entry.Entity.Role == UserRole.Admin || entry.Entity.Role == UserRole.Customer) && entry.Entity.BranchId.HasValue)
                {
                    throw new BusinessException($"Tài khoản {entry.Entity.Role} '{entry.Entity.Username}' không được gắn với Cơ sở (BranchId phải là null).");
                }
            }
        }
    }
}
