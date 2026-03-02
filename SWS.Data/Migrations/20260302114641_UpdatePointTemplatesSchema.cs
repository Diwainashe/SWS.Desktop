using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWS.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePointTemplatesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultDataType",
                table: "PointTemplates",
                newName: "DataType");

            migrationBuilder.RenameColumn(
                name: "DefaultArea",
                table: "PointTemplates",
                newName: "Area");

            migrationBuilder.AddColumn<int>(
                name: "Address",
                table: "PointTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "PointTemplates");

            migrationBuilder.RenameColumn(
                name: "DataType",
                table: "PointTemplates",
                newName: "DefaultDataType");

            migrationBuilder.RenameColumn(
                name: "Area",
                table: "PointTemplates",
                newName: "DefaultArea");
        }
    }
}
