using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MariCamiStore.Infrastructure.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIsGeneric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGeneric",
                schema: "dbo",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Upsert "Sin Cliente" generic customer (ID pre-exists in production)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6')
BEGIN
    INSERT INTO [dbo].[Customers] ([Id],[NickName],[Name],[PhoneNumber],[Address],[LocationLink],[Email],[IsGeneric])
    VALUES ('84828E82-81CA-437D-B2F0-B9877EF044C6','Sin Cliente','Sin Cliente','0000-0000',NULL,NULL,NULL,1)
END
ELSE
BEGIN
    UPDATE [dbo].[Customers]
    SET [IsGeneric] = 1
    WHERE [Id] = '84828E82-81CA-437D-B2F0-B9877EF044C6'
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsGeneric",
                schema: "dbo",
                table: "Customers");
        }
    }
}
