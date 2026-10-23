using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class FixedScheduleConfiguration : IEntityTypeConfiguration<FixedSchedule>
{
    public void Configure(EntityTypeBuilder<FixedSchedule> builder)
    {
        builder.ToTable("FixedSchedules");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ValidFrom)
            .HasColumnType("date");

        builder.Property(s => s.ValidTo)
            .HasColumnType("date");

        builder.Property(s => s.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.FixedSchedules)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Court)
            .WithMany(c => c.FixedSchedules)
            .HasForeignKey(s => s.CourtId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
