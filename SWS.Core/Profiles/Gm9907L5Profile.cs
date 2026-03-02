using System;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class Gm9907L5Profile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.GM9907_L5;

    public string FormatDisplay(
        string key,
        decimal? value,
        IReadOnlyList<LatestReadingSnapshot> allReadings)
    {
        // if bad/empty, show dash
        if (value == null)
            return "—";

        // Find this point's data type (so we only show ON/OFF for true boolean points)
        var thisPoint = allReadings.FirstOrDefault(x => x.Key == key);
        var dataType = thisPoint?.DataType ?? PointDataType.UInt16;

        // ✅ Only render ON/OFF if the point itself is Bool
        if (dataType == PointDataType.Bool)
            return value.Value == 0m ? "OFF" : "ON";

        // Device-specific formatting
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
                // generic numeric formatting for this device when no special rule exists
                return value.Value.ToString("0.###");
        }
    }

    private static string FormatDecimal(decimal value, int decimals, string unit)
    {
        // guard: manual could send weird decimals
        if (decimals < 0) decimals = 0;
        if (decimals > 6) decimals = 6;

        string num = Math.Round(value, decimals).ToString($"F{decimals}");
        return string.IsNullOrWhiteSpace(unit) ? num : $"{num} {unit}";
    }

    private static int GetInt(IReadOnlyList<LatestReadingSnapshot> list, string key)
    {
        var item = list.FirstOrDefault(x => x.Key == key);

        // If missing, default to 0
        if (item?.ValueNumeric is null)
            return 0;

        return (int)item.ValueNumeric.Value;
    }
}