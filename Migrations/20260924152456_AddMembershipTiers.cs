using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MembershipTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FreeDelivery = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxFamilyAccounts = table.Column<int>(type: "int", nullable: false),
                    HasDedicatedPharmacist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PriorityDispensing = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipTiers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CustomerMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    MembershipTierId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NextBillingDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerMemberships_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerMemberships_MembershipTiers_MembershipTierId",
                        column: x => x.MembershipTierId,
                        principalTable: "MembershipTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMemberships_CustomerId",
                table: "CustomerMemberships",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerMemberships_MembershipTierId",
                table: "CustomerMemberships",
                column: "MembershipTierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerMemberships");

            migrationBuilder.DropTable(
                name: "MembershipTiers");
        }
    }
}
