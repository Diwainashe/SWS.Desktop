using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SWS.Core.Models;

namespace SWS.Data.Seed;

/// <summary>
/// Seeds PointTemplates for known device types.
/// Safe to run on every startup:
/// - Inserts missing templates (by DeviceType+Key)
/// - Optionally "patches" existing templates only when values are blank/zero
/// </summary>
public static class PointTemplateSeeder
{
    /// <summary>
    /// Ensures GM9907_L5 templates exist.
    /// If overwriteExisting = false, we only patch fields that are "missing"
    /// (e.g. Address == 0, empty Label/Unit, etc.)
    /// </summary>
    public static void SeedMissing(SwsDbContext db, bool overwriteExisting = false)
    {
        // IMPORTANT: We match by (DeviceType + Key). That's your natural key.
        const string deviceType = nameof(DeviceType.GM9907_L5); // "GM9907_L5"

        var desired = BuildGm9907L5Templates(deviceType);

        // Pull existing templates for this device type in one query
        var existing = db.PointTemplates
            .Where(t => t.DeviceType == deviceType)
            .ToList();

        // Index by Key for fast lookup
        var existingByKey = existing.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);

        int inserted = 0;
        int patched = 0;

        foreach (var template in desired)
        {
            if (!existingByKey.TryGetValue(template.Key, out var row))
            {
                // Missing -> insert
                db.PointTemplates.Add(template);
                inserted++;
                continue;
            }

            // Exists -> patch/overwrite depending on mode
            if (TryPatch(row, template, overwriteExisting))
                patched++;
        }

        if (inserted > 0 || patched > 0)
            db.SaveChanges();
    }

    private static List<PointTemplate> BuildGm9907L5Templates(string deviceType) => new()
    {
        // =========================
        // Essentials
        // =========================
        new PointTemplate
        {
            DeviceType = deviceType,
            Key = "Weight.Display",
            Label = "Weight",
            Unit = "", // unit resolved using Weight.Unit in profile
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
            Unit = "", // unit resolved using Flowrate.Unit in profile
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
            Address = 113, // avoid leading zeros
            DefaultLength = 1,
            DataType = PointDataType.Bool,
            Scale = 1m,
            PollRateMs = 500,
            IsEssential = true,
            LogToHistory = false,
            HistoryIntervalMs = 0
        },

        // =========================
        // Alarm bitfields (2 x UInt16)
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
        // Helpers (decimals + units)
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
        },
    };

    /// <summary>
    /// Patches an existing row using the desired template.
    /// If overwriteExisting = false, only fills in "missing" fields.
    /// Returns true if any field changed.
    /// </summary>
    private static bool TryPatch(PointTemplate existing, PointTemplate desired, bool overwriteExisting)
    {
        bool changed = false;

        // Helper local: update a field either always (overwrite) or only if "missing"
        void Patch<T>(Func<T> get, Action<T> set, Func<T, bool> isMissing, T newValue)
        {
            var current = get();
            if (overwriteExisting || isMissing(current))
            {
                if (!EqualityComparer<T>.Default.Equals(current, newValue))
                {
                    set(newValue);
                    changed = true;
                }
            }
        }

        // “Missing” rules:
        // - strings missing if null/empty/whitespace
        // - ints missing if 0
        // - enums missing if default(0) AND desired is non-default (you can tune this)
        // - bools: usually don't patch unless overwriteExisting (because false might be intentional)
        //   but for templates we can safely patch bools if you want; I keep them overwrite-only.

        Patch(() => existing.Label, v => existing.Label = v,
            s => string.IsNullOrWhiteSpace(s), desired.Label);

        Patch(() => existing.Unit, v => existing.Unit = v,
            s => string.IsNullOrWhiteSpace(s), desired.Unit);

        Patch(() => existing.Address, v => existing.Address = v,
            i => i <= 0, desired.Address);

        Patch(() => existing.Area, v => existing.Area = v,
            a => EqualityComparer<ModbusPointArea>.Default.Equals(a, default) && !EqualityComparer<ModbusPointArea>.Default.Equals(desired.Area, default),
            desired.Area);

        Patch(() => existing.DataType, v => existing.DataType = v,
            dt => EqualityComparer<PointDataType>.Default.Equals(dt, default) && !EqualityComparer<PointDataType>.Default.Equals(desired.DataType, default),
            desired.DataType);

        Patch(() => existing.DefaultLength, v => existing.DefaultLength = v,
            l => l == 0, desired.DefaultLength);

        Patch(() => existing.Scale, v => existing.Scale = v,
            sc => sc == 0m, desired.Scale);

        Patch(() => existing.PollRateMs, v => existing.PollRateMs = v,
            ms => ms <= 0, desired.PollRateMs);

        // For these flags, only overwrite if overwriteExisting=true
        if (overwriteExisting)
        {
            if (existing.IsEssential != desired.IsEssential) { existing.IsEssential = desired.IsEssential; changed = true; }
            if (existing.LogToHistory != desired.LogToHistory) { existing.LogToHistory = desired.LogToHistory; changed = true; }
            if (existing.HistoryIntervalMs != desired.HistoryIntervalMs) { existing.HistoryIntervalMs = desired.HistoryIntervalMs; changed = true; }
        }
        else
        {
            // if missing-like (HistoryIntervalMs == 0 while LogToHistory is true), patch it
            if (existing.LogToHistory && existing.HistoryIntervalMs <= 0 && desired.HistoryIntervalMs > 0)
            {
                existing.HistoryIntervalMs = desired.HistoryIntervalMs;
                changed = true;
            }
        }

        return changed;
    }
}