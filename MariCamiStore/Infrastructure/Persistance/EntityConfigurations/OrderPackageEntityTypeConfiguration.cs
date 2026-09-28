using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class OrderPackageEntityTypeConfiguration : IEntityTypeConfiguration<OrderPackage>
{
    public void Configure(EntityTypeBuilder<OrderPackage> builder)
    {
        builder.ToTable("OrderPackages", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.OrderId)
            .IsRequired();

        builder.Property(p => p.DeliveryDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CurrencyId)
            .IsRequired();

        builder.Property(p => p.Description)
            .IsRequired(false)
            .HasMaxLength(OrderPackage.DescriptionMaxLength);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Restrict (NO ACTION), not Cascade: Orders -> OrderPackages -> CxPEntries and Orders -> CxPEntries
        // would otherwise be multiple cascade paths (SQL Server error 1785). Orders with packages are never deleted
        // (only Pending orders can be deleted, and packages only exist in Active/Delivering or later).
        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.OrderId);
    }
}
