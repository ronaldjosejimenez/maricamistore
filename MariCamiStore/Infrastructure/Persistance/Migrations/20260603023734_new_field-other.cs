using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class new_fieldother : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "dbo",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ListPriceTax",
                schema: "dbo",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "dbo",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ListPriceTax",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "dbo",
                table: "OrderItems",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");
        }
    }
}
