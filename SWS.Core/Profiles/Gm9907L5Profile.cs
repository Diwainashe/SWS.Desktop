using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class Gm9907L5Profile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.GM9907_L5;

    // Alarm Info 1 (40004) bit meanings (from your spreadsheet)
    private static readonly IReadOnlyDictionary<int, string> AlarmInfo1Bits = new Dictionary<int, string>
    {
        [0] = "Emergency Stop",
        [1] = "Overload Alarm",
        [2] = "Motor Alarm",
        [3] = "Loadcell Signal Unstable",
        [4] = "Loadcell Over Range",
        [5] = "Loadcell Under Range",
        [6] = "Loadcell Error",
        [7] = "Overweight",
        [8] = "Underweight",
        [9] = "Over Tolerance",
        [10] = "Under Tolerance",
        [11] = "Feeding Gate Open Overtime",
        [12] = "Feeding Gate Not Closed",
        [13] = "Feeding Gate Close Overtime",
        [14] = "Discharge Gate Open Overtime",
        [15] = "Discharge Gate Close Overtime",
    };

    // Alarm Info 2 (40005) bit meanings (from your spreadsheet)
    private static readonly IReadOnlyDictionary<int, string> AlarmInfo2Bits = new Dictionary<int, string>
    {
        [0] = "Motor Parameter Error",
        [1] = "Calibration Fail, Unstable",
        [2] = "Calibration Fail, Loadcell Input High",
        [3] = "Calibration Fail, Loadcell Input Low",
        [4] = "Calibration Fail, Unstable",
        [5] = "Calibration Fail, Weight Over",
        [6] = "Calibration Fail, Weight Under",
        [7] = "Calibration Fail, Weight Value Error",
        [8] = "Calibration Fail, Over Resolution",
        [9] = "Calibration Fail, No Gain Voltage Record",
        [10] = "Over&Under Pause",
        [11] = "Fill Timeout",
        [12] = "Disc Timeout",
        // 13..15 reserved
    };

    public string FormatDisplay(
        string key,
        decimal? value,
        IReadOnlyList<LatestReadingSnapshot> allReadings)
    {
        // bad/empty
        if (value is null)
            return "—";

        // ✅ Strict rule: ONLY show ON/OFF if THIS point is Bool
        var thisPoint = allReadings.FirstOrDefault(x => x.Key == key);
        var dataType = thisPoint?.DataType ?? PointDataType.UInt16;
        if (dataType == PointDataType.Bool)
            return value.Value == 0m ? "OFF" : "ON";

        // Alarm decoding (bitfields)
        if (key == "Alarm.Info1")
            return DecodeAlarmBits((ushort)value.Value, AlarmInfo1Bits);

        if (key == "Alarm.Info2")
            return DecodeAlarmBits((ushort)value.Value, AlarmInfo2Bits);

        // Device-specific numeric formatting
        switch (key)
        {
            case "Flowrate.Actual":
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

            case "Weight.Display":
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

            default:
                return value.Value.ToString("0.###");
        }
    }

    private static string DecodeAlarmBits(ushort raw, IReadOnlyDictionary<int, string> map)
    {
        // No alarms set
        if (raw == 0)
            return "OK";

        var active = new List<string>();

        for (int bit = 0; bit < 16; bit++)
        {
            bool isSet = (raw & (1 << bit)) != 0;
            if (!isSet)
                continue;

            // If bit is reserved/unknown, still show something useful
            if (map.TryGetValue(bit, out var label))
                active.Add(label);
            else
                active.Add($"Bit {bit}");
        }

        return string.Join("; ", active);
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
}