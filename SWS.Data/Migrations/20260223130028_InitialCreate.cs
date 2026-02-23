using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWS.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<byte>(type: "tinyint", nullable: false),
                    PollMs = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LatestReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceConfigId = table.Column<int>(type: "int", nullable: false),
                    PointConfigId = table.Column<int>(type: "int", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ErrorText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quality = table.Column<int>(type: "int", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceConfigId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<int>(type: "int", nullable: false),
                    Length = table.Column<int>(type: "int", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PollRateMs = table.Column<int>(type: "int", nullable: false),
                    IsEssential = table.Column<bool>(type: "bit", nullable: false),
                    LogToHistory = table.Column<bool>(type: "bit", nullable: false),
                    HistoryIntervalMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PointTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefaultArea = table.Column<int>(type: "int", nullable: false),
                    DefaultDataType = table.Column<int>(type: "int", nullable: false),
                    DefaultLength = table.Column<int>(type: "int", nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PollRateMs = table.Column<int>(type: "int", nullable: false),
                    IsEssential = table.Column<bool>(type: "bit", nullable: false),
                    LogToHistory = table.Column<bool>(type: "bit", nullable: false),
                    HistoryIntervalMs = table.Column<int>(type: "int", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTemplates", x => x.Id);
                });

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
                name: "IX_PointTemplates_Key",
                table: "PointTemplates",
                column: "Key",
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
                name: "DeviceConfigs");

            migrationBuilder.DropTable(
                name: "LatestReadings");

            migrationBuilder.DropTable(
                name: "PointConfigs");

            migrationBuilder.DropTable(
                name: "PointTemplates");

            migrationBuilder.DropTable(
                name: "ReadingHistories");
        }
    }
}
