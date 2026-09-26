using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddBogoDiscountToSaleDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BogoDiscountAmount",
                table: "SaleDetails",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BogoDiscountAmount",
                table: "SaleDetails");
        }
    }
}
