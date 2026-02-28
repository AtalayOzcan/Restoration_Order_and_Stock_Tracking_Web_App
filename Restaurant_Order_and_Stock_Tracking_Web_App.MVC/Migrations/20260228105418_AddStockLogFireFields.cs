using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLogFireFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "stock_logs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "stock_logs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "stock_logs",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "stock_logs");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "stock_logs");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "stock_logs");
        }
    }
}
