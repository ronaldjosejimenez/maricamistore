using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class SupplierDefaultsAndRealPriceInLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyIdDefault",
                schema: "dbo",
                table: "Suppliers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateDefault",
                schema: "dbo",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsActiveForCalculator",
                schema: "dbo",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCostDefault",
                schema: "dbo",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxPercentageDefault",
                schema: "dbo",
                table: "Suppliers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "HasLegacyPriceDefaults",
                schema: "dbo",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RealPriceInLocal",
                schema: "dbo",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb710"),
                columns: new[] { "CurrencyIdDefault", "IsActiveForCalculator" },
                values: new object[] { null, true });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb711"),
                column: "CurrencyIdDefault",
                value: null);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb712"),
                column: "CurrencyIdDefault",
                value: null);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb713"),
                column: "CurrencyIdDefault",
                value: null);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb714"),
                column: "CurrencyIdDefault",
                value: null);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb715"),
                column: "CurrencyIdDefault",
                value: null);

            // Runs after the seed UpdateData above so the filled values are not overwritten (spec 018, FR-004).
            // HasLegacyPriceDefaults marks every item that exists when the migration runs, exactly once;
            // items created afterwards keep the column default (false).
            migrationBuilder.Sql(@"
UPDATE s
SET s.TaxPercentageDefault = c.TaxPercentage,
    s.CurrencyIdDefault    = c.OrderCurrencyIdDefault,
    s.ExchangeRateDefault  = c.ExchangeRate
FROM dbo.Suppliers s
CROSS JOIN (SELECT TOP 1 TaxPercentage, OrderCurrencyIdDefault, ExchangeRate
            FROM dbo.Configurations ORDER BY Id) c;

UPDATE dbo.Suppliers SET IsActiveForCalculator = 1 WHERE Name = N'Shein';

UPDATE oi
SET oi.RealPriceInLocal       = ROUND(oi.RealPrice * o.ExchangeRate, 2),
    oi.HasLegacyPriceDefaults = 1
FROM dbo.OrderItems oi
INNER JOIN dbo.Orders o ON o.Id = oi.OrderId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrencyIdDefault",
                schema: "dbo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ExchangeRateDefault",
                schema: "dbo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "IsActiveForCalculator",
                schema: "dbo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ShippingCostDefault",
                schema: "dbo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TaxPercentageDefault",
                schema: "dbo",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "HasLegacyPriceDefaults",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "RealPriceInLocal",
                schema: "dbo",
                table: "OrderItems");
        }
    }
}
