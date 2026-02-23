using System.Linq;
using SWS.Core.Models;

namespace SWS.Data.Seed;

/// <summary>
/// Seeds standard template keys for a device family (GM9907-L5).
/// No addresses here. Users map addresses in PointConfigs per device.
/// </summary>
public static class PointTemplateSeeder
{
    public static void SeedIfEmpty(SwsDbContext db)
    {
        // Only seed once (safe on every startup)
        if (db.PointTemplates.Any())
            return;

        const string deviceType = "GM9907-L5";

        // --- Essentials for a tile (generic across many scale controllers) ---
        db.PointTemplates.AddRange(
            // 1) Weight shown on main screen (engineering unit)
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Weight.Display",
                Label = "Weight",
                Unit = "kg",
                DefaultArea = ModbusPointArea.HoldingRegister,
                DefaultDataType = PointDataType.Int32,   // common in controllers (scaled)
                DefaultLength = 2,
                Scale = 0.001m,                          // placeholder until confirmed by manual
                PollRateMs = 250,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 1000
            },

            // 2) Flowrate shown on main screen (t/h as per HMI)
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Flowrate.Actual",
                Label = "Flowrate",
                Unit = "t/h",
                DefaultArea = ModbusPointArea.HoldingRegister,
                DefaultDataType = PointDataType.Int32,   // typical; user can switch to Float32 if manual says so
                DefaultLength = 2,
                Scale = 0.001m,                          // placeholder until confirmed by manual
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 2000
            },

            // 3) Running/Stopped flag (often coil/discrete, but leave as default + user sets it)
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Status.RunState",
                Label = "Run State",
                Unit = "",
                DefaultArea = ModbusPointArea.Coil,
                DefaultDataType = PointDataType.UInt16,
                DefaultLength = 1,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = false,
                HistoryIntervalMs = 0
            },

            // 4) Alarm code (numeric, app maps to meaning; store text only on errors)
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Alarm.Code",
                Label = "Alarm Code",
                Unit = "",
                DefaultArea = ModbusPointArea.HoldingRegister,
                DefaultDataType = PointDataType.UInt16,
                DefaultLength = 1,
                Scale = 1m,
                PollRateMs = 1000,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 10000
            },

            // 5) Target weight (seen on top bar)
            new PointTemplate
            {
                DeviceType = deviceType,
                Key = "Recipe.TargetWeight",
                Label = "Target",
                Unit = "kg",
                DefaultArea = ModbusPointArea.HoldingRegister,
                DefaultDataType = PointDataType.Int32,
                DefaultLength = 2,
                Scale = 0.001m, // placeholder
                PollRateMs = 2000,
                IsEssential = true,
                LogToHistory = false,
                HistoryIntervalMs = 0
            }
        );

        db.SaveChanges();
    }
}