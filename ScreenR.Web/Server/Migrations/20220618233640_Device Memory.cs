using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenR.Web.Server.Migrations
{
    public partial class DeviceMemory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TotalMemory",
                table: "Devices",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "UsedMemory",
                table: "Devices",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalMemory",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "UsedMemory",
                table: "Devices");
        }
    }
}
