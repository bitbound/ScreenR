using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScreenR.Web.Server.Migrations
{
    public partial class Alias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Is64Bit",
                table: "Devices");

            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Devices");

            migrationBuilder.AddColumn<bool>(
                name: "Is64Bit",
                table: "Devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
