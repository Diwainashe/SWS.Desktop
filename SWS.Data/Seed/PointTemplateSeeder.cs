using System.Linq;
using SWS.Core.Models;

namespace SWS.Data.Seed;

/// <summary>
/// Seeds PointTemplates for known device types.
/// Safe to run on every startup (it only seeds if empty).
/// </summary>
public static class PointTemplateSeeder
{
    public static void SeedIfEmpty(SwsDbContext db)
    {
        // Only seed once
        if (db.PointTemplates.Any())
            return;

        const string deviceType = nameof(DeviceType.GM9907_L5); // "GM9907_L5"

        db.PointTemplates.AddRange(

            // =========================
            // Essentials (tile + overview)
            // =========================

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Display",
                Label = "Weight",
                Unit = "", // unit will be resolved by profile helpers later
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,                 // leave 0 until you confirm manual mapping
                DefaultLength = 2,
                DataType = PointDataType.Int32,
                Scale = 1m,
                PollRateMs = 250,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 1000
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Flowrate.Actual",
                Label = "Flowrate",
                Unit = "", // unit resolved by profile helpers later
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 2,
                DataType = PointDataType.Int32,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 2000
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Status.RunState",
                Label = "Run State",
                Unit = "",
                Area = ModbusPointArea.Coil,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.Bool,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Alarm.Code",
                Label = "Alarm Code",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 1000,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 10000
            },

            // =========================
            // Helper points for decoding (decimals + units)
            // =========================

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Decimal",
                Label = "Weight Decimals",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 2000,
                IsEssential = false,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Unit",
                Label = "Weight Unit Code",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 2000,
                IsEssential = false,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Flowrate.Decimal",
                Label = "Flowrate Decimals",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 2000,
                IsEssential = false,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Flowrate.Unit",
                Label = "Flowrate Unit Code",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 0,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 2000,
                IsEssential = false,
                LogToHistory = false,
                HistoryIntervalMs = 0
            }
        );

        db.SaveChanges();
    }
}