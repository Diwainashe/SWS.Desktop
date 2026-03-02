using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class Gm9907L5Profile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.GM9907_L5;

    // Alarm.Info1 (40004) bit meanings (from your spreadsheet)
    private static readonly IReadOnlyDictionary<int, string> AlarmInfo1Bits =
        new Dictionary<int, string>
        {
            { 0,  "Delivery Done Alarm" },
            { 1,  "Fail, Zero Over Range" },
            { 2,  "Fail, Weight Unstable" },
            { 3,  "Fail, Process Running" },
            { 4,  "Target Is 0, Unable To Start" },
            { 5,  "Over/Under Alarm" },
            { 6,  "Weight OFL, Unable To Start" },
            { 7,  "Continuous Flowrate Low" },
            { 8,  "Stable Judge Overtime scale unstable" },
            { 9,  "Target Error, Unable To Start" },
            { 10, "Clear ACUM Before Next Run" },
            { 11, "Discharge Gate Not Closed" },
            { 12, "Feeding Gate Not Closed" },
            { 13, "Feeding Gate Close Overtime" },
            { 14, "Discharge Gate Open Overtime" },
            { 15, "Discharge Gate Close Overtime" },
        };

    // Alarm.Info2 (40005) bit meanings (from your spreadsheet)
    private static readonly IReadOnlyDictionary<int, string> AlarmInfo2Bits =
        new Dictionary<int, string>
        {
            { 0,  "Motor Parameter Error" },
            { 1,  "Calibration Fail, Unstable" },
            { 2,  "Calibration Fail, Loadcell Input High (None Weight zero voltage input > 15625)" },
            { 3,  "Calibration Fail, Loadcell Input Low (None Weight zero voltage input < 2)" },
            { 4,  "Calibration Fail, Unstable" },
            { 5,  "Calibration Fail, Weight Over (None Weight gain voltage input > 15625)" },
            { 6,  "Calibration Fail, Weight Under (relative voltage negative)" },
            { 7,  "Calibration Fail, Weight Value Error (write value is 0 or > Capacity)" },
            { 8,  "Calibration Fail, Over Resolution (too high calibration resolution)" },
            { 9,  "Calibration Fail, No Gain Voltage Record" },
            { 10, "Over&Under Pause" },
            { 11, "Fill Timeout" },
            { 12, "Disc Timeout" },
            { 13, "Reserved" },
        };

    public string FormatDisplay(
        string key,
        decimal? value,
        IReadOnlyList<LatestReadingSnapshot> allReadings)
    {
        // If missing/bad -> dash
        if (value == null)
            return "—";

        // Find this point's datatype so ON/OFF is purely datatype-driven
        var thisPoint = allReadings.FirstOrDefault(x => x.Key == key);
        var dataType = thisPoint?.DataType ?? PointDataType.UInt16;

        // ✅ Only show ON/OFF if the point itself is Bool
        if (dataType == PointDataType.Bool)
            return value.Value == 0m ? "OFF" : "ON";

        // ---- Device-specific special formatting ----

        // Weight + Flowrate: decimals + unit code come from helper registers
        if (key == "Flowrate.Actual")
        {
            int decimals = GetInt(allReadings, "Flowrate.Decimal");
            int unitCode = GetInt(allReadings, "Flowrate.Unit");

            string unit = unitCode switch
            {
                0 => "g/h",
                1 => "kg/h",
                2 => "t/h",
                3 => "lb/h",
                _ => ""
            };

            return FormatDecimal(value.Value, decimals, unit);
        }

        if (key == "Weight.Display")
        {
            int decimals = GetInt(allReadings, "Weight.Decimal");
            int unitCode = GetInt(allReadings, "Weight.Unit");

            string unit = unitCode switch
            {
                0 => "g",
                1 => "kg",
                2 => "t",
                3 => "lb",
                _ => ""
            };

            return FormatDecimal(value.Value, decimals, unit);
        }

        // Alarm bitfields: show active alarm list
        if (key == "Alarm.Info1")
            return FormatAlarmBits(value.Value, AlarmInfo1Bits);

        if (key == "Alarm.Info2")
            return FormatAlarmBits(value.Value, AlarmInfo2Bits);

        // Default numeric formatting
        return value.Value.ToString("0.###");
    }

    private static string FormatDecimal(decimal value, int decimals, string unit)
    {
        if (decimals < 0) decimals = 0;
        if (decimals > 6) decimals = 6;

        string num = Math.Round(value, decimals).ToString($"F{decimals}");
        return string.IsNullOrWhiteSpace(unit) ? num : $"{num} {unit}";
    }

    private static int GetInt(IReadOnlyList<LatestReadingSnapshot> list, string key)
    {
        var item = list.FirstOrDefault(x => x.Key == key);
        if (item?.ValueNumeric is null)
            return 0;

        return (int)item.ValueNumeric.Value;
    }

    /// <summary>
    /// Decodes a UInt16 bitfield into "OK" or "Active: A, B, C"
    /// </summary>
    private static string FormatAlarmBits(decimal value, IReadOnlyDictionary<int, string> map)
    {
        // Alarm registers are UInt16 in templates, but stored as decimal.
        // Clamp safely.
        int bits = (int)Math.Clamp(value, 0m, 65535m);

        if (bits == 0)
            return "OK";

        var active = new List<string>();

        foreach (var kv in map.OrderBy(k => k.Key))
        {
            int bit = kv.Key;
            bool isSet = (bits & (1 << bit)) != 0;

            if (isSet)
                active.Add(kv.Value);
        }

        return active.Count == 0
            ? $"Active: (unknown bits) [{bits}]"
            : "Active: " + string.Join(", ", active);
    }
}