using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class SupplierEntityTypeConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(c => c.Id);
        builder.Property(m => m.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasData(
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB710"), Name = "Shein"},
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711"), Name = "Adidas"},
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB712"), Name = "Puma" },
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB713"), Name = "Amazon" },
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB714"), Name = "Ross y otros" },
            new Supplier { Id = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB715"), Name = "Otros" });
    }
}