using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("datetime2");

        builder.HasOne(u => u.Customer)
            .WithOne(c => c.User)
            .HasForeignKey<Customer>(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.CreatedBookings)
            .WithOne(b => b.CreatedByUser)
            .HasForeignKey(b => b.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ConfirmedBookings)
            .WithOne(b => b.ConfirmedByUser)
            .HasForeignKey(b => b.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.CancelledBookings)
            .WithOne(b => b.CancelledByUser)
            .HasForeignKey(b => b.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.ReceivedPayments)
            .WithOne(p => p.ReceivedByUser)
            .HasForeignKey(p => p.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
