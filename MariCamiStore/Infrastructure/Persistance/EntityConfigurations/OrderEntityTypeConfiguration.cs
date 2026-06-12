using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(o => o.Id);
        builder.Property(m => m.Id)
          .IsRequired(true)
          .ValueGeneratedOnAdd();

        builder.Property(o => o.OrganizationId)
            .IsRequired();

        builder.Property(o => o.NameOfOrder)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.SupplierId)
            .IsRequired();

        builder.Property(o => o.ExchangeRate)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TaxPercentage)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ShippingAmountToCR)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ShippingAmountIntern)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.DiscountAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TotalWithoutTaxes)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TaxesAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TotalToPayToSupplier)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.TotalOfTheOrder)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.EstimatedProfitInLocal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        builder.Property(o => o.TotalAgreedPriceInLocal)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(12);

        builder.Property(o => o.CurrencyId)
            .IsRequired();

        builder.Property(o => o.ActualShippingAmountToCR)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);
    }
}