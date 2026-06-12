using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class Improve_desing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductSize",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ServiceFeeInLocalCurrency",
                schema: "dbo",
                table: "Configurations");

            migrationBuilder.RenameColumn(
                name: "ControlAmountNotInPlace",
                schema: "dbo",
                table: "Orders",
                newName: "TotalWithoutTaxes");

            migrationBuilder.RenameColumn(
                name: "ControlAmount",
                schema: "dbo",
                table: "Orders",
                newName: "TotalToPayToSupplier");

            migrationBuilder.RenameColumn(
                name: "ServiceFeeInCol",
                schema: "dbo",
                table: "OrderItems",
                newName: "ServiceFeeInLocal");

            migrationBuilder.RenameColumn(
                name: "AgreedPriceInCol",
                schema: "dbo",
                table: "OrderItems",
                newName: "AgreedPriceInLocal");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrencyId",
                schema: "dbo",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedProfitInLocal",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "NameOfOrder",
                schema: "dbo",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                schema: "dbo",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "TaxesAmount",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOfTheOrder",
                schema: "dbo",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProductImage",
                schema: "dbo",
                table: "OrderItems",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductTypeId",
                schema: "dbo",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrderCurrencyIdDefault",
                schema: "dbo",
                table: "Configurations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EstimateShipping = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceFeeInLocal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb712"),
                columns: new[] { "ExchangeRate", "ExchangeRateMargin", "OrderCurrencyIdDefault" },
                values: new object[] { 510m, 20m, new Guid("64b4d953-66d5-409e-929d-6036111fb711") });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "ProductTypes",
                columns: new[] { "Id", "CurrencyId", "Description", "EstimateShipping", "Name", "ServiceFeeInLocal" },
                values: new object[,]
                {
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb710"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Joyeria, bisuteria, adornos corporales.", 1m, "Pequeños", 800m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb711"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Uñas, prendas pequeñas", 2m, "Pequeños medio", 1000m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb712"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Camisas, vestidos, ropa interior, leggis, etc.", 3m, "Prendas de ropa normales", 1500m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb713"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Juegos de camisas, o conjuntos.", 4m, "Paquetes de Prendas", 1500m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb714"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Jeans, Jackets", 4m, "Prendas pesadas", 2500m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb715"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Zapatos o tennis livianas", 4m, "Zapatos livianos", 2000m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb716"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Zapatos o tennis", 5m, "Zapatos", 2500m },
                    { new Guid("73b4d953-66d5-409e-929d-6036111fb717"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Para armar promos propias", 3m, "Promos", 2500m }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Suppliers",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb710"), "Shein" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "Adidas" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb712"), "Puma" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb713"), "Amazon" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb714"), "Ross y otros" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb715"), "Otros" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                schema: "dbo",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "EstimatedProfitInLocal",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "NameOfOrder",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxesAmount",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalOfTheOrder",
                schema: "dbo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductImage",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                schema: "dbo",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "OrderCurrencyIdDefault",
                schema: "dbo",
                table: "Configurations");

            migrationBuilder.RenameColumn(
                name: "TotalWithoutTaxes",
                schema: "dbo",
                table: "Orders",
                newName: "ControlAmountNotInPlace");

            migrationBuilder.RenameColumn(
                name: "TotalToPayToSupplier",
                schema: "dbo",
                table: "Orders",
                newName: "ControlAmount");

            migrationBuilder.RenameColumn(
                name: "ServiceFeeInLocal",
                schema: "dbo",
                table: "OrderItems",
                newName: "ServiceFeeInCol");

            migrationBuilder.RenameColumn(
                name: "AgreedPriceInLocal",
                schema: "dbo",
                table: "OrderItems",
                newName: "AgreedPriceInCol");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "dbo",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductSize",
                schema: "dbo",
                table: "OrderItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeeInLocalCurrency",
                schema: "dbo",
                table: "Configurations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb712"),
                columns: new[] { "ExchangeRate", "ExchangeRateMargin", "ServiceFeeInLocalCurrency" },
                values: new object[] { 520m, 30m, 3000m });
        }
    }
}
