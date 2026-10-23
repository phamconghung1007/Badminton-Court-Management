using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("Courts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Note)
            .HasMaxLength(500);

        builder.HasIndex(c => new { c.BranchId, c.Name })
            .IsUnique();

        builder.HasMany(c => c.Bookings)
            .WithOne(b => b.Court)
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.FixedSchedules)
            .WithOne(s => s.Court)
            .HasForeignKey(s => s.CourtId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
