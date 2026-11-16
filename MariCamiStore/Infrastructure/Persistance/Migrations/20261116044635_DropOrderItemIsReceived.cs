using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class DropOrderItemIsReceived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Spec 021: the data was already moved to Status = 'Received' by MigrateIsReceivedToStatus.
            migrationBuilder.DropColumn(
                name: "IsReceived",
                schema: "dbo",
                table: "OrderItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReceived",
                schema: "dbo",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Rebuild the flag from Status so reverting only this migration does not lose which items were received.
            migrationBuilder.Sql("UPDATE dbo.OrderItems SET IsReceived = 1 WHERE Status = 'Received';");
        }
    }
}
