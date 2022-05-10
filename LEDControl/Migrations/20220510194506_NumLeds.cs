using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LEDControl.Migrations
{
    public partial class NumLeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumLeds",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumLeds",
                table: "Devices");
        }
    }
}
