using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class SalesGoalDayEntityTypeConfiguration : IEntityTypeConfiguration<SalesGoalDay>
{
    public void Configure(EntityTypeBuilder<SalesGoalDay> builder)
    {
        builder.ToTable("SalesGoalDays", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(d => d.SalesGoalId).IsRequired();
        builder.Property(d => d.DayOfMonth).IsRequired();
        builder.Property(d => d.ProposedAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(d => d.GoalAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(d => d.ActualAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(d => d.CompliancePercentage).IsRequired().HasColumnType("decimal(9,1)");

        builder.HasOne(d => d.SalesGoal)
            .WithMany(g => g.Days)
            .HasForeignKey(d => d.SalesGoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.SalesGoalId, d.DayOfMonth }).IsUnique();
    }
}
