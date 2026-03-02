using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Data.Seed;

/// <summary>
/// Seeds PointTemplates for known device types.
/// SAFE to run on every startup:
/// - does NOT wipe anything
/// - only INSERTS missing (DeviceType+Key) rows
/// </summary>
public static class PointTemplateSeeder
{
    public static void SeedMissing(SwsDbContext db)
    {
        // We seed by enum-name string so it matches DeviceType.ToString()
        string deviceType = nameof(DeviceType.GM9907_L5); // "GM9907_L5"

        var templates = new List<PointTemplate>
        {
            // =========================
            // Essentials
            // =========================
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Display",
                Label = "Weight",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40007,
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
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40009,
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
                Address = 113,
                DefaultLength = 1,
                DataType = PointDataType.Bool,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            // =========================
            // Alarm bitfields (from your spreadsheet)
            // 40004 = Alarm info 1
            // 40005 = Alarm info 2
            // =========================
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Alarm.Info1",
                Label = "Alarm Info 1",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40004,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 1000,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 5000
            },

            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Alarm.Info2",
                Label = "Alarm Info 2",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40005,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 1000,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 5000
            },

            // =========================
            // Helper points for decimals + units
            // =========================
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Unit",
                Label = "Weight Unit Code",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40151,
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
                Key = "Weight.Decimal",
                Label = "Weight Decimals",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 40152,
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
                Address = 40011,
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
                Address = 40012,
                DefaultLength = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 2000,
                IsEssential = false,
                LogToHistory = false,
                HistoryIntervalMs = 0
            }
        };

        // Build a set of existing keys for this device type (fast “missing” check)
        var existingKeys = db.PointTemplates
            .Where(t => t.DeviceType == deviceType)
            .Select(t => t.Key)
            .ToHashSet();

        int added = 0;

        foreach (var t in templates)
        {
            if (existingKeys.Contains(t.Key))
                continue;

            db.PointTemplates.Add(t);
            added++;
        }

        if (added > 0)
            db.SaveChanges();
    }
}