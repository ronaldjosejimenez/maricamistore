using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class SalesGoalEntityTypeConfiguration : IEntityTypeConfiguration<SalesGoal>
{
    public void Configure(EntityTypeBuilder<SalesGoal> builder)
    {
        builder.ToTable("SalesGoals", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(g => g.SalespersonId).IsRequired();
        builder.Property(g => g.OrganizationId).IsRequired();
        builder.Property(g => g.Month).IsRequired();
        builder.Property(g => g.Year).IsRequired();
        builder.Property(g => g.GoalAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(g => g.ActualAmount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(g => g.CompliancePercentage).IsRequired().HasColumnType("decimal(9,1)");
        builder.Property(g => g.CurrencyId).IsRequired();

        builder.HasOne(g => g.Salesperson)
            .WithMany()
            .HasForeignKey(g => g.SalespersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Currency)
            .WithMany()
            .HasForeignKey(g => g.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.SalespersonId, g.OrganizationId, g.Year, g.Month })
            .IsUnique();
    }
}
