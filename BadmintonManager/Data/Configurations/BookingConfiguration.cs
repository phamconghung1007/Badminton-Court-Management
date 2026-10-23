using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", t =>
        {
            t.HasCheckConstraint("CK_Booking_EndTime_After_StartTime", "[EndTime] > [StartTime]");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(b => b.Code)
            .IsUnique();

        builder.Property(b => b.GuestName)
            .HasMaxLength(100);

        builder.Property(b => b.GuestPhone)
            .HasMaxLength(15);

        builder.Property(b => b.BookingType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.StartTime)
            .HasColumnType("datetime2");

        builder.Property(b => b.EndTime)
            .HasColumnType("datetime2");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(b => b.SubTotal)
            .HasPrecision(18, 0);

        builder.Property(b => b.DiscountPercent)
            .HasPrecision(5, 2);

        builder.Property(b => b.DiscountAmount)
            .HasPrecision(18, 0);

        builder.Property(b => b.FinalAmount)
            .HasPrecision(18, 0);

        builder.Property(b => b.DepositAmount)
            .HasPrecision(18, 0);

        builder.Property(b => b.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(b => b.CheckedInAt)
            .HasColumnType("datetime2");

        builder.Property(b => b.CancelledAt)
            .HasColumnType("datetime2");

        builder.Property(b => b.CancelReason)
            .HasMaxLength(300);

        builder.Property(b => b.RowVersion)
            .IsRowVersion();

        // Indexes
        builder.HasIndex(b => new { b.CourtId, b.StartTime, b.EndTime });
        builder.HasIndex(b => new { b.CustomerId, b.StartTime });
        builder.HasIndex(b => b.Status);

        // Foreign keys
        builder.HasOne(b => b.Court)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CourtId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.FixedSchedule)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.FixedScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreatedByUser)
            .WithMany(u => u.CreatedBookings)
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ConfirmedByUser)
            .WithMany(u => u.ConfirmedBookings)
            .HasForeignKey(b => b.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CancelledByUser)
            .WithMany(u => u.CancelledBookings)
            .HasForeignKey(b => b.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Payments)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.PointTransactions)
            .WithOne(p => p.Booking)
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
