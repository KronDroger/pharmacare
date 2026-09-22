using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarePlusPharmacy.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Sales",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Sales",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Sales",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedById",
                table: "Sales",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_VoidedById",
                table: "Sales",
                column: "VoidedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_VoidedById",
                table: "Sales",
                column: "VoidedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_VoidedById",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_VoidedById",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidedById",
                table: "Sales");
        }
    }
}
