using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class MembershipTierConfiguration : IEntityTypeConfiguration<MembershipTier>
{
    public void Configure(EntityTypeBuilder<MembershipTier> builder)
    {
        builder.ToTable("MembershipTiers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.DiscountPercent)
            .HasPrecision(5, 2);

        builder.HasMany(m => m.Customers)
            .WithOne(c => c.MembershipTier)
            .HasForeignKey(c => c.MembershipTierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
