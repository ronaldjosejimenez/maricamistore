using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Abbreviation", "Name" },
                values: new object[,]
                {
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb710"), "COL", "Colones" },
                    { new Guid("63b4d953-66d5-409e-929d-6036111fb711"), "USD", "Dolares" }
                });

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("64b4d953-66d5-409e-929d-6036111fb710"), "MariCamiStore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
