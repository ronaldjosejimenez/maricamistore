using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class OneOpenPeriodPerOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PeriodControls_OneOpenPerOrganization",
                schema: "dbo",
                table: "PeriodControls",
                column: "OrganizationId",
                unique: true,
                filter: "[IsClosed] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeriodControls_OneOpenPerOrganization",
                schema: "dbo",
                table: "PeriodControls");
        }
    }
}
