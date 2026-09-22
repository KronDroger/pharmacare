using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class LinkUsersToCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Resolve existing duplicate Customer emails BEFORE the unique index is
            // created: keep the earliest record's email, blank the duplicates so no
            // existing patient data needs to be deleted.
            migrationBuilder.Sql("""
                UPDATE Customers c
                JOIN (
                    SELECT Email, MIN(Id) AS KeepId
                    FROM Customers
                    WHERE Email IS NOT NULL AND Email <> ''
                    GROUP BY Email
                ) k ON c.Email = k.Email AND c.Id <> k.KeepId
                SET c.Email = NULL;
                """);

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            // One-time backfill: link existing accounts to the single Customer record
            // that shares their email (post-dedupe), so the portal resolves patients by
            // CustomerId instead of email from now on. New registrations set the link.
            migrationBuilder.Sql("""
                UPDATE AspNetUsers u
                JOIN Customers c
                  ON c.Email = u.Email
                LEFT JOIN Customers dup
                  ON c.Email = dup.Email AND dup.Id <> c.Id
                SET u.CustomerId = c.Id
                WHERE u.CustomerId IS NULL AND dup.Id IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AspNetUsers");
        }
    }
}