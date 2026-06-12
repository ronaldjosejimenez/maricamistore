using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCxPModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualShippingAmountToCR",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PeriodControls",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionMonth = table.Column<int>(type: "int", nullable: false),
                    TransactionYear = table.Column<int>(type: "int", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PagosRealizados = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    EnCuenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodControls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CxPEntries",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CxPEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CxPEntries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "dbo",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CxPEntries_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CxPEntries_PeriodControls_PeriodControlId",
                        column: x => x.PeriodControlId,
                        principalSchema: "dbo",
                        principalTable: "PeriodControls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CxPEntries_CurrencyId",
                schema: "dbo",
                table: "CxPEntries",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CxPEntries_OrderId",
                schema: "dbo",
                table: "CxPEntries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CxPEntries_PeriodControlId",
                schema: "dbo",
                table: "CxPEntries",
                column: "PeriodControlId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodControls_OrganizationId_TransactionMonth_TransactionYear",
                schema: "dbo",
                table: "PeriodControls",
                columns: new[] { "OrganizationId", "TransactionMonth", "TransactionYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CxPEntries",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PeriodControls",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "ActualShippingAmountToCR",
                schema: "dbo",
                table: "Orders");
        }
    }
}
