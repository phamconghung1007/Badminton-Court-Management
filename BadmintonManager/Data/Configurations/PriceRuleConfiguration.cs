using BadmintonManager.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BadmintonManager.Data.Configurations;

public class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("PriceRules", t =>
        {
            t.HasCheckConstraint("CK_PriceRule_Minutes", "[StartMinute] >= 0 AND [EndMinute] <= 1440 AND [EndMinute] > [StartMinute]");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DayType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.PricePerHour)
            .HasPrecision(18, 0);
    }
}
