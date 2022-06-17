using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenR.Web.Server.Migrations
{
    public partial class Devicestorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TotalStorage",
                table: "Devices",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "UsedStorage",
                table: "Devices",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalStorage",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "UsedStorage",
                table: "Devices");
        }
    }
}
