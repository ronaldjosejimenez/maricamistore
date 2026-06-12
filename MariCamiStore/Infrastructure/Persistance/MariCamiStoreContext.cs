using MariCamiStore.Infrastructure.Persistance.EntityConfigurations;
using MariCamiStore.Model;
using MariCamiStore.Services;
using Microsoft.EntityFrameworkCore;

namespace MariCamiStore.Infrastructure.Persistance;

/// <summary>A mari cami store context.</summary>
public class MariCamiStoreContext(
    DbContextOptions<MariCamiStoreContext> options,
    ICurrentOrganizationService currentOrganizationService)
    : DbContext(options)
{
    /// <summary>(Immutable) the default schema.</summary>
    public const string DEFAULT_SCHEMA = "dbo";

    /// <summary>Gets or sets the configurations.</summary>
    /// <value>The configurations.</value>
    public DbSet<Configuration> Configurations { get; set; }

    /// <summary>Gets or sets the currencies.</summary>
    /// <value>The currencies.</value>
    public DbSet<Currency> Currencies { get; set; }

    /// <summary>Gets or sets the organizations.</summary>
    /// <value>The organizations.</value>
    public DbSet<Organization> Organizations { get; set; }

    /// <summary>Gets or sets the customers.</summary>
    /// <value>The customers.</value>
    public DbSet<Customer> Customers { get; set; }

    /// <summary>Gets or sets the orders.</summary>
    /// <value>The orders.</value>
    public DbSet<Order> Orders { get; set; }

    /// <summary>Gets or sets the order items.</summary>
    /// <value>The order items.</value>
    public DbSet<OrderItem> OrderItems { get; set; }

    /// <summary>Gets or sets the transactions.</summary>
    /// <value>The transactions.</value>
    public DbSet<Transaction> Transactions { get; set; }

    /// <summary>Gets or sets a list of types of the products.</summary>
    /// <value>A list of types of the products.</value>
    public DbSet<ProductType> ProductTypes { get; set; }

    /// <summary>Gets or sets the suppliers.</summary>
    /// <value>The suppliers.</value>
    public DbSet<Supplier> Suppliers { get; set; }

    /// <summary>Gets or sets the order status histories.</summary>
    /// <value>The order status histories.</value>
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public DbSet<PeriodControl> PeriodControls { get; set; }
    public DbSet<CxPEntry> CxPEntries { get; set; }

    /// <summary>Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.</summary>
    /// <remarks><para>
    ///                 If a model is explicitly set on the options for this context (via <see cref="M:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
    ///                 then this method will not be run. However, it will still run when creating a compiled model.
    ///             </para>
    /// <para>
    ///                 See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more information and
    ///                 examples.
    ///             </para></remarks>
    /// <param name="builder">The builder being used to construct the model for this context. Databases (and other extensions) typically
    /// define extension methods on this object that allow you to configure aspects of the model that are specific
    /// to a given database.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ConfigurationEntityTypeConfiguration());
        builder.ApplyConfiguration(new CurrencyEntityTypeConfiguration());
        builder.ApplyConfiguration(new OrganizationEntityTypeConfiguration());
        builder.ApplyConfiguration(new CustomerEntityTypeConfiguration());
        builder.ApplyConfiguration(new OrderEntityTypeConfiguration());
        builder.ApplyConfiguration(new OrderItemEntityTypeConfiguration());
        builder.ApplyConfiguration(new TransactionEntityTypeConfiguration());
        builder.ApplyConfiguration(new ProductTypeEntityTypeConfiguration());
        builder.ApplyConfiguration(new SupplierEntityTypeConfiguration());
        builder.ApplyConfiguration(new OrderStatusHistoryEntityTypeConfiguration());
        builder.ApplyConfiguration(new PeriodControlEntityTypeConfiguration());
        builder.ApplyConfiguration(new CxPEntryEntityTypeConfiguration());

        // Capture the service reference (not value) so EF re-evaluates per DbContext instance
        builder.Entity<Configuration>().HasQueryFilter(c => c.OrganizationId == currentOrganizationService.OrganizationId);
        builder.Entity<Order>().HasQueryFilter(o => o.OrganizationId == currentOrganizationService.OrganizationId);
        builder.Entity<OrderItem>().HasQueryFilter(oi => oi.Order.OrganizationId == currentOrganizationService.OrganizationId);
        builder.Entity<Transaction>().HasQueryFilter(t => t.OrganizationId == currentOrganizationService.OrganizationId);
        builder.Entity<PeriodControl>().HasQueryFilter(p => p.OrganizationId == currentOrganizationService.OrganizationId);
    }
}
