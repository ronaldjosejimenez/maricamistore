using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class MigrateIsReceivedToStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration (spec 021): OrderItem.IsReceived is replaced by OrderItem.Status = 'Received'.
            // Raw SQL skips the organization query filter, so the fix applies to every organization.
            // Voided items keep their status, and UpdatedAt is untouched: this is a data fix, not a user action.
            migrationBuilder.Sql(@"
UPDATE dbo.OrderItems SET Status = 'Received' WHERE IsReceived = 1 AND Status <> 'Voided';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reversible while the IsReceived column still exists (it is dropped by DropOrderItemIsReceived).
            migrationBuilder.Sql(@"
UPDATE dbo.OrderItems SET IsReceived = 1, Status = 'Active' WHERE Status = 'Received';
");
        }
    }
}
