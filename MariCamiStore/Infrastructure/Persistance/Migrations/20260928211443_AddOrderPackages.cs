using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualShippingAmountToCR",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderPackageId",
                schema: "dbo",
                table: "CxPEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderPackages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPackages_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "dbo",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPackages_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CxPEntries_OrderPackageId",
                schema: "dbo",
                table: "CxPEntries",
                column: "OrderPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_CurrencyId",
                schema: "dbo",
                table: "OrderPackages",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPackages_OrderId",
                schema: "dbo",
                table: "OrderPackages",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CxPEntries_OrderPackages_OrderPackageId",
                schema: "dbo",
                table: "CxPEntries",
                column: "OrderPackageId",
                principalSchema: "dbo",
                principalTable: "OrderPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CxPEntries_OrderPackages_OrderPackageId",
                schema: "dbo",
                table: "CxPEntries");

            migrationBuilder.DropTable(
                name: "OrderPackages",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_CxPEntries_OrderPackageId",
                schema: "dbo",
                table: "CxPEntries");

            migrationBuilder.DropColumn(
                name: "OrderPackageId",
                schema: "dbo",
                table: "CxPEntries");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualShippingAmountToCR",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
