using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using MariCamiStore.Model;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class TransactionEntityTypeConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(t => t.Id);
        builder.Property(m => m.Id)
          .IsRequired(true)
          .ValueGeneratedOnAdd();

        builder.Property(t => t.SourceId)
            .IsRequired(false);

        builder.Property(t => t.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.CustomerId);

        builder.Property(t => t.TransactionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.TransactionDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.TransactionAmount)
            .IsRequired()
            .HasColumnType("decimal(18,4)");

        builder.Property(t => t.TransactionDate)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.Property(o => o.CurrencyId)
            .IsRequired();
    }
}