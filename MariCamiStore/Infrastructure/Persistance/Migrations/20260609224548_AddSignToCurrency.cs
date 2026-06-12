using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddSignToCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sign",
                schema: "dbo",
                table: "Currencies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb710"),
                column: "Sign",
                value: "₡");

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Currencies",
                keyColumn: "Id",
                keyValue: new Guid("63b4d953-66d5-409e-929d-6036111fb711"),
                column: "Sign",
                value: "$");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sign",
                schema: "dbo",
                table: "Currencies");
        }
    }
}
