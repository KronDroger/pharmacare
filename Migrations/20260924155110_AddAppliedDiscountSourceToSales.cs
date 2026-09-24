using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddAppliedDiscountSourceToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedDiscountSource",
                table: "Sales",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedDiscountSource",
                table: "Sales");
        }
    }
}
