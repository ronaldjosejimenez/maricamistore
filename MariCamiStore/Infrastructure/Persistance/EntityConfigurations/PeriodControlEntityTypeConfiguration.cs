using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class PeriodControlEntityTypeConfiguration : IEntityTypeConfiguration<PeriodControl>
{
    public void Configure(EntityTypeBuilder<PeriodControl> builder)
    {
        builder.ToTable("PeriodControls", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.TransactionMonth).IsRequired();
        builder.Property(p => p.TransactionYear).IsRequired();
        builder.Property(p => p.ExchangeRate).IsRequired().HasColumnType("decimal(18,4)");
        builder.Property(p => p.PagosRealizados).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(p => p.EnCuenta).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(p => p.IsClosed).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.OrganizationId, p.TransactionMonth, p.TransactionYear }).IsUnique();
    }
}
