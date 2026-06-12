using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Configurations",
                columns: new[] { "Id", "ExchangeRate", "ExchangeRateMargin", "LocalCurrencyId", "OrderCurrencyIdDefault", "OrganizationId", "ProductTypeIdDefault", "TaxPercentage" },
                values: new object[] { new Guid("64b4d953-66d6-409e-929d-6036111fb713"), 530m, 20m, new Guid("63b4d953-66d5-409e-929d-6036111fb710"), new Guid("63b4d953-66d5-409e-929d-6036111fb711"), new Guid("64b4d953-66d5-409e-929d-6036111fb712"), null, 13m });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Customers",
                columns: new[] { "Id", "Address", "Email", "LocationLink", "Name", "NickName", "PhoneNumber" },
                values: new object[] { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), "San José, Costa Rica", "prueba@test.com", null, "Cliente de Prueba", "Cliente Prueba", "8888-0000" });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Organizations",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("64b4d953-66d5-409e-929d-6036111fb712"), "Testing" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb713"));

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Customers",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));

            migrationBuilder.DeleteData(
                schema: "dbo",
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d5-409e-929d-6036111fb712"));
        }
    }
}
