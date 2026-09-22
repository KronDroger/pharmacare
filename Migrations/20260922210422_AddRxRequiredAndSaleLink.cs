using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddRxRequiredAndSaleLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SaleId",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RxRequired",
                table: "Medicines",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_SaleId",
                table: "Prescriptions",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Sales_SaleId",
                table: "Prescriptions",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Sales_SaleId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_SaleId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "SaleId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "RxRequired",
                table: "Medicines");
        }
    }
}
