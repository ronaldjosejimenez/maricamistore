using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class CurrencyEntityTypeConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(c => c.Id);
        builder.Property(m => m.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Abbreviation)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Sign)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.HasData(
            new Currency { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB710"), Name = "Colones", Abbreviation = "COL", Sign = "₡" },
            new Currency { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711"), Name = "Dolares", Abbreviation = "USD", Sign = "$" });
    }
}