using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class OrderItemEntityTypeConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(oi => oi.Id);
        builder.Property(m => m.Id)
          .IsRequired(true)
          .ValueGeneratedOnAdd();

        builder.Property(oi => oi.OrderId)
            .IsRequired();

        builder.Property(oi => oi.CustomerId)
            .IsRequired();

        builder.Property(oi => oi.ProductDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(oi => oi.ProductLink)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(oi => oi.ProductSourceCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(oi => oi.ProductSourceCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(oi => oi.ProductImage);

        builder.Property(o => o.ProductTypeId)
            .IsRequired();

        builder.Property(oi => oi.ListPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.ListPriceTaxWithTax)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.EstimateShipping)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.ServiceFeeInLocal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.AgreedPriceInLocal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.RealPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.IsReceived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(oi => oi.CreatedAt)
            .IsRequired();

        builder.Property(oi => oi.UpdatedAt)
            .IsRequired();

        builder.HasOne(oi => oi.Order)
               .WithMany()
               .HasForeignKey(oi => oi.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}