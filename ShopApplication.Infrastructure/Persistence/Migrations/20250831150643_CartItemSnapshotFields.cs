using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopApplication.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CartItemSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "CartItems");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "CartItems",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "CartItems",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "CartItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CartItems",
                newName: "ProductName");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Carts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Carts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "CartItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "CartItems",
                type: "TEXT",
                nullable: true);
        }
    }
}
