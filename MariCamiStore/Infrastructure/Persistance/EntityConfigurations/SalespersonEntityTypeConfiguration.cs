using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class SalespersonEntityTypeConfiguration : IEntityTypeConfiguration<Salesperson>
{
    public void Configure(EntityTypeBuilder<Salesperson> builder)
    {
        builder.ToTable("Salespeople", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.Property(s => s.NickName).HasMaxLength(50);
        builder.Property(s => s.PhoneNumber).HasMaxLength(20);
        builder.Property(s => s.Email).HasMaxLength(100);
        builder.Property(s => s.IsActive).IsRequired().HasDefaultValue(true);
    }
}
