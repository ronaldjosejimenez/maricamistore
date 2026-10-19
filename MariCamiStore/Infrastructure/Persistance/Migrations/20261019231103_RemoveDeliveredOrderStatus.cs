using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeliveredOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration (spec 017): the order status 'Delivered' was removed.
            // Every order still in 'Delivered' moves to 'Completed', leaving one history row per order.
            // No Transactions or CxP entries are created. Raw SQL skips the organization query filter,
            // so the fix applies to every organization.
            migrationBuilder.Sql(@"
INSERT INTO dbo.OrderStatusHistory (Id, OrderId, FromStatus, ToStatus, TransitionDate, Notes, Justification, CreatedAt)
SELECT NEWID(), Id, 'Delivered', 'Completed', SYSUTCDATETIME(),
       N'Migración automática: el estado Entregada fue eliminado (spec 017).', NULL, SYSUTCDATETIME()
FROM dbo.Orders WHERE Status = 'Delivered';

UPDATE dbo.Orders SET Status = 'Completed', UpdatedAt = SYSUTCDATETIME() WHERE Status = 'Delivered';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: this is an irreversible data fix. The 'Delivered' status no longer
            // exists in code, so restoring it would leave orders the application cannot load.
        }
    }
}
