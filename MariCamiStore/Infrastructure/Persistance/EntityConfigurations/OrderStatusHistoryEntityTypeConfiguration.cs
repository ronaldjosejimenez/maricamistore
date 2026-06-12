using MariCamiStore.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MariCamiStore.Infrastructure.Persistance.EntityConfigurations;

public class OrderStatusHistoryEntityTypeConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory", MariCamiStoreContext.DEFAULT_SCHEMA);
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(h => h.OrderId).IsRequired();
        builder.Property(h => h.FromStatus).IsRequired().HasMaxLength(20);
        builder.Property(h => h.ToStatus).IsRequired().HasMaxLength(20);
        builder.Property(h => h.TransitionDate).IsRequired();
        builder.Property(h => h.Notes).HasMaxLength(500);
        builder.Property(h => h.Justification).HasMaxLength(1000);
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasOne<Order>()
               .WithMany()
               .HasForeignKey(h => h.OrderId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
