using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class CustomerEntityTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", MariCamiStoreContext.DEFAULT_SCHEMA);
    
        builder.HasKey(c => c.Id);
        builder.Property(m => m.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .HasMaxLength(150);

        builder.Property(c => c.NickName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Address)
            .HasMaxLength(250);

        builder.Property(c => c.LocationLink)
            .HasMaxLength(500);

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.IsGeneric)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasData(
            new Customer
            {
                Id = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                NickName = "Cliente Prueba",
                Name = "Cliente de Prueba",
                PhoneNumber = "8888-0000",
                Email = "prueba@test.com",
                Address = "San José, Costa Rica",
                LocationLink = null,
                IsGeneric = false,
            });
    }
}