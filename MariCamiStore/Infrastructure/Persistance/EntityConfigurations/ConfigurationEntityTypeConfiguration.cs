using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations
{
    public class ConfigurationEntityTypeConfiguration : IEntityTypeConfiguration<Configuration>
    {
        public void Configure(EntityTypeBuilder<Configuration> builder)
        {
            builder.ToTable("Configurations", MariCamiStoreContext.DEFAULT_SCHEMA);
            builder.HasKey(b => b.Id);
            builder.Property(m => m.Id)
                .IsRequired(true)
                .ValueGeneratedOnAdd();

            builder.Property(c => c.OrganizationId)
                .IsRequired();

            builder.Property(c => c.ExchangeRateMargin)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.ExchangeRate)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.TaxPercentage)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(c => c.LocalCurrencyId)
                .IsRequired();

            builder.Property(c => c.OrderCurrencyIdDefault)
                .IsRequired();

            builder.Property(c => c.ProductTypeIdDefault);           

            builder.HasData(
                new Configuration 
                { 
                    Id = Guid.Parse("64B4D953-66D6-409E-929D-6036111FB712"),
                    ExchangeRate = 510,
                    ExchangeRateMargin = 20,
                    LocalCurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB710"),
                    OrganizationId = Guid.Parse("64B4D953-66D5-409E-929D-6036111FB710"),
                    TaxPercentage = 7,
                    OrderCurrencyIdDefault = Guid.Parse("64B4D953-66D5-409E-929D-6036111FB711"),
                    ProductTypeIdDefault = Guid.Parse("73B4D953-66D5-409E-929D-6036111FB712"),
                });

            // Testing org config
            builder.HasData(
                new Configuration
                {
                    Id = Guid.Parse("64B4D953-66D6-409E-929D-6036111FB713"),
                    ExchangeRate = 530,
                    ExchangeRateMargin = 20,
                    LocalCurrencyId = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB710"),   // Colones
                    OrganizationId = Guid.Parse("64B4D953-66D5-409E-929D-6036111FB712"),    // Testing
                    TaxPercentage = 13,
                    OrderCurrencyIdDefault = Guid.Parse("63B4D953-66D5-409E-929D-6036111FB711"), // USD
                    ProductTypeIdDefault = null,
                });
        }
    }
}
