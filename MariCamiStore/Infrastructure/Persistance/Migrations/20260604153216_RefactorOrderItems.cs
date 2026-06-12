using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class RefactorOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedAt",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ListPriceTax",
                schema: "dbo",
                table: "OrderItems",
                newName: "ListPriceTaxWithTax");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAgreedPriceInLocal",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalAgreedPriceInLocal",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "ListPriceTaxWithTax",
                schema: "dbo",
                table: "OrderItems",
                newName: "ListPriceTax");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedAt",
                schema: "dbo",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }
    }
}
