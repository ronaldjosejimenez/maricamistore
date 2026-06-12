using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "Organizations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Currencies",
                newName: "Currencies",
                newSchema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Organizations",
                schema: "dbo",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "Currencies",
                schema: "dbo",
                newName: "Currencies");
        }
    }
}
