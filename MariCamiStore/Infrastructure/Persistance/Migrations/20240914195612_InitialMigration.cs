using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Configurations",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeRateMargin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceFeeInLocalCurrency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LocalCurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Configurations",
                columns: new[] { "Id", "ExchangeRate", "ExchangeRateMargin", "LocalCurrencyId", "OrganizationId", "ServiceFeeInLocalCurrency", "TaxPercentage" },
                values: new object[] { new Guid("64b4d953-66d6-409e-929d-6036111fb712"), 520m, 30m, new Guid("63b4d953-66d5-409e-929d-6036111fb710"), new Guid("64b4d953-66d5-409e-929d-6036111fb710"), 3000m, 7m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations",
                schema: "dbo");
        }
    }
}
