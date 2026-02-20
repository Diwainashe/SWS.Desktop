using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorianAndNumericOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValueText",
                table: "LatestReadings",
                newName: "ErrorText");

            migrationBuilder.AddColumn<int>(
                name: "Area",
                table: "PointConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HistoryIntervalMs",
                table: "PointConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LogToHistory",
                table: "PointConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ReadingHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceConfigId = table.Column<int>(type: "int", nullable: false),
                    PointConfigId = table.Column<int>(type: "int", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ErrorText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quality = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LatestReadings_DeviceConfigId_PointConfigId",
                table: "LatestReadings",
                columns: new[] { "DeviceConfigId", "PointConfigId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_DeviceConfigId_TimestampUtc",
                table: "ReadingHistories",
                columns: new[] { "DeviceConfigId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingHistories_PointConfigId_TimestampUtc",
                table: "ReadingHistories",
                columns: new[] { "PointConfigId", "TimestampUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingHistories");

            migrationBuilder.DropIndex(
                name: "IX_LatestReadings_DeviceConfigId_PointConfigId",
                table: "LatestReadings");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "PointConfigs");

            migrationBuilder.DropColumn(
                name: "HistoryIntervalMs",
                table: "PointConfigs");

            migrationBuilder.DropColumn(
                name: "LogToHistory",
                table: "PointConfigs");

            migrationBuilder.RenameColumn(
                name: "ErrorText",
                table: "LatestReadings",
                newName: "ValueText");
        }
    }
}
