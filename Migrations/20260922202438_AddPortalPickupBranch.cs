using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalPickupBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PickupBranchId",
                table: "CustomerSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubscriptions_PickupBranchId",
                table: "CustomerSubscriptions",
                column: "PickupBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerSubscriptions_Branches_PickupBranchId",
                table: "CustomerSubscriptions",
                column: "PickupBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerSubscriptions_Branches_PickupBranchId",
                table: "CustomerSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSubscriptions_PickupBranchId",
                table: "CustomerSubscriptions");

            migrationBuilder.DropColumn(
                name: "PickupBranchId",
                table: "CustomerSubscriptions");
        }
    }
}
