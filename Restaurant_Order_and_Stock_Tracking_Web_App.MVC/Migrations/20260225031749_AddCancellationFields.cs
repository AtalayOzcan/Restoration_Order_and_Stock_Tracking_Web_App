using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "order_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledQuantity",
                table: "order_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsWasted",
                table: "order_items",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "CancelledQuantity",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "IsWasted",
                table: "order_items");
        }
    }
}
