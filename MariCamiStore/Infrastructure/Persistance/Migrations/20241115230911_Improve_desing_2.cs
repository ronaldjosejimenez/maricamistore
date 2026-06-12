using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class Improve_desing_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductTypeIdDefault",
                schema: "dbo",
                table: "Configurations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb712"),
                column: "ProductTypeIdDefault",
                value: new Guid("73b4d953-66d5-409e-929d-6036111fb712"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductTypeIdDefault",
                schema: "dbo",
                table: "Configurations");
        }
    }
}
