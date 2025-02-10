using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DynastyBeacon.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "StockMaster",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StockMaster_Category",
                table: "StockMaster",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockMaster_Category",
                table: "StockMaster");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "StockMaster",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
