using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class ProductTypeEntityTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("ProductTypes", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(c => c.Id);
        builder.Property(m => m.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(oi => oi.EstimateShipping)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(oi => oi.ServiceFeeInLocal)
            .IsRequired()
            .HasColumnType("decimal(18,2)");


        builder.Property(o => o.CurrencyId)
            .IsRequired();

        builder.HasData(
            new ProductType
            { 
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB710"),
                Name = "Pequeños",
                Description = "Joyeria, bisuteria, adornos corporales.",
                EstimateShipping = 1,
                ServiceFeeInLocal = 800,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB711"),
                Name = "Pequeños medio",
                Description = "Uñas, prendas pequeñas",
                EstimateShipping = 2,
                ServiceFeeInLocal = 1000,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB712"),
                Name = "Prendas de ropa normales",
                Description = "Camisas, vestidos, ropa interior, leggis, etc.",
                EstimateShipping = 3,
                ServiceFeeInLocal = 1500,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB713"),
                Name = "Paquetes de Prendas",
                Description = "Juegos de camisas, o conjuntos.",
                EstimateShipping = 4,
                ServiceFeeInLocal = 1500,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB714"),
                Name = "Prendas pesadas",
                Description = "Jeans, Jackets",
                EstimateShipping = 4,
                ServiceFeeInLocal = 2500,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB715"),
                Name = "Zapatos livianos",
                Description = "Zapatos o tennis livianas",
                EstimateShipping = 4,
                ServiceFeeInLocal = 2000,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB716"),
                Name = "Zapatos",
                Description = "Zapatos o tennis",
                EstimateShipping = 5,
                ServiceFeeInLocal = 2500,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            },
            new ProductType
            {
                Id = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB717"),
                Name = "Promos",
                Description = "Para armar promos propias",
                EstimateShipping = 3,
                ServiceFeeInLocal = 2500,
                CurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711")
            });
    }
}