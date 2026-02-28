using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class Addiswaitercalledtotables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWaiterCalled",
                table: "tables",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWaiterCalled",
                table: "tables");
        }
    }
}
