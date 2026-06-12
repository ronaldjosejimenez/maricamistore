using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class CxPEntryEntityTypeConfiguration : IEntityTypeConfiguration<CxPEntry>
{
    public void Configure(EntityTypeBuilder<CxPEntry> builder)
    {
        builder.ToTable("CxPEntries", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(e => e.PeriodControlId).IsRequired();
        builder.Property(e => e.CurrencyId).IsRequired();
        builder.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(e => e.Reference).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(30);
        builder.Property(e => e.OrderId).IsRequired(false);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.Period)
            .WithMany(p => p.Entries)
            .HasForeignKey(e => e.PeriodControlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Currency)
            .WithMany()
            .HasForeignKey(e => e.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
