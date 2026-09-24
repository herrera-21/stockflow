using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnitOfMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_MinimumStock",
                table: "Products");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumStock",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentStock",
                table: "Products",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "BaseUnit",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unit");

            migrationBuilder.AddColumn<string>(
                name: "PurchaseUnit",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unit");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseUnitFactor",
                table: "Products",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products",
                sql: "[CurrentStock] >= 0 AND ([BaseUnit] NOT IN ('Unit', 'Dozen', 'Box', 'Pack') OR [CurrentStock] = FLOOR([CurrentStock]))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_MinimumStock",
                table: "Products",
                sql: "[MinimumStock] >= 0 AND ([BaseUnit] NOT IN ('Unit', 'Dozen', 'Box', 'Pack') OR [MinimumStock] = FLOOR([MinimumStock]))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_PurchaseUnitFactor",
                table: "Products",
                sql: "[PurchaseUnitFactor] > 0 AND ([BaseUnit] NOT IN ('Unit', 'Dozen', 'Box', 'Pack') OR [PurchaseUnitFactor] = FLOOR([PurchaseUnitFactor]))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_MinimumStock",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_PurchaseUnitFactor",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BaseUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchaseUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchaseUnitFactor",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "MinimumStock",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "CurrentStock",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products",
                sql: "[CurrentStock] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_MinimumStock",
                table: "Products",
                sql: "[MinimumStock] >= 0");
        }
    }
}
