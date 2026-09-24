using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_TaxId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_TaxId",
                table: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Suppliers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_DocumentType_TaxId",
                table: "Suppliers",
                columns: new[] { "DocumentType", "TaxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DocumentType_TaxId",
                table: "Customers",
                columns: new[] { "DocumentType", "TaxId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_DocumentType_TaxId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_DocumentType_TaxId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_TaxId",
                table: "Suppliers",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TaxId",
                table: "Customers",
                column: "TaxId",
                unique: true);
        }
    }
}
