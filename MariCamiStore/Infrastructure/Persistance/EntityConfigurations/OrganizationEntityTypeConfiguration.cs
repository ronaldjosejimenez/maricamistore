using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class OrganizationEntityTypeConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations", MariCamiStoreContext.DEFAULT_SCHEMA);

        builder.HasKey(o => o.Id);
        builder.Property(m => m.Id)
            .IsRequired(true)
            .ValueGeneratedOnAdd();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasData(
            new Organization { Id = Guid.Parse("64B4D953-66D5-409E-929D-6036111FB710"), Name = "MariCamiStore" });

        builder.HasData(
            new Organization { Id = Guid.Parse("64B4D953-66D5-409E-929D-6036111FB712"), Name = "Testing" });
    }
}