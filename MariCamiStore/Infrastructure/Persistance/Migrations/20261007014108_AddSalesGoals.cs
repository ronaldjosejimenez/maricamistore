using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultMonthlyGoal",
                schema: "dbo",
                table: "Configurations",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 4000000m);

            migrationBuilder.CreateTable(
                name: "Salespeople",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NickName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salespeople", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalesGoals",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalespersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    GoalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompliancePercentage = table.Column<decimal>(type: "decimal(9,1)", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGoals_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "dbo",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesGoals_Salespeople_SalespersonId",
                        column: x => x.SalespersonId,
                        principalSchema: "dbo",
                        principalTable: "Salespeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesGoalDays",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesGoalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfMonth = table.Column<int>(type: "int", nullable: false),
                    ProposedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GoalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompliancePercentage = table.Column<decimal>(type: "decimal(9,1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGoalDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGoalDays_SalesGoals_SalesGoalId",
                        column: x => x.SalesGoalId,
                        principalSchema: "dbo",
                        principalTable: "SalesGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb712"),
                column: "DefaultMonthlyGoal",
                value: 4000000m);

            migrationBuilder.UpdateData(
                schema: "dbo",
                table: "Configurations",
                keyColumn: "Id",
                keyValue: new Guid("64b4d953-66d6-409e-929d-6036111fb713"),
                column: "DefaultMonthlyGoal",
                value: 4000000m);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoalDays_SalesGoalId_DayOfMonth",
                schema: "dbo",
                table: "SalesGoalDays",
                columns: new[] { "SalesGoalId", "DayOfMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoals_CurrencyId",
                schema: "dbo",
                table: "SalesGoals",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesGoals_SalespersonId_OrganizationId_Year_Month",
                schema: "dbo",
                table: "SalesGoals",
                columns: new[] { "SalespersonId", "OrganizationId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalesGoalDays",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SalesGoals",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Salespeople",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "DefaultMonthlyGoal",
                schema: "dbo",
                table: "Configurations");
        }
    }
}
