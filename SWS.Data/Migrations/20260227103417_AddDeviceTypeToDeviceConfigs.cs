using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTypeToDeviceConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "DeviceType",
                table: "DeviceConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "DeviceConfigs");
        }
    }
}
