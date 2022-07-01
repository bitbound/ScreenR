using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenR.Web.Server.Migrations
{
    public partial class DeviceNameProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ComputerName",
                table: "Devices",
                newName: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Devices",
                newName: "ComputerName");
        }
    }
}
