using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Core.Profiles;

/// <summary>
/// GM9907-L5 display formatting rules:
/// - ON/OFF is ONLY for Bool points (DataType == Bool)
/// - Weight/Flowrate: use unit + decimals from helper registers
/// - State + Alarm registers (UInt16) are decoded as bitfields (active-only)
/// </summary>
public sealed class Gm9907L5Profile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.GM9907_L5;

    public string FormatDisplay(
        string key,
        decimal? value,
        IReadOnlyList<LatestReadingSnapshot> allReadings)
    {
        // If missing or bad, show dash.
        if (value == null)
            return "—";

        // Find this point's DataType so we NEVER guess.
        var thisPoint = allReadings.FirstOrDefault(x => x.Key == key);
        var dataType = thisPoint?.DataType ?? PointDataType.UInt16;

        // ✅ 1) Only Bool points get ON/OFF.
        if (dataType == PointDataType.Bool)
            return value.Value == 0m ? "OFF" : "ON";

        // ✅ 2) Decode known GM bitfield registers (UInt16) as "active-only".
        // These are not "boolean points"; they are status/alarm bitfields.
        if (dataType == PointDataType.UInt16 || dataType == PointDataType.Int16)
        {
            // We stored these as numeric decimals, but they represent a 16-bit word.
            // Cast safely.
            var word = ToUInt16(value.Value);

            if (key == "Alarm.Info1")
                return DecodeAlarmInfo1(word);

            if (key == "Alarm.Info2")
                return DecodeAlarmInfo2(word);

            if (key == "State.Weight")
                return DecodeWeightState(word);

            if (key == "State.Operating")
                return DecodeOperatingState(word);

            if (key == "State.Condition")
                return DecodeConditionState(word);
        }

        // ✅ 3) Device-specific numeric formatting (weight/flowrate helpers)
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

                    // PLC stores flowrate as integer with implied decimal point.
                    // e.g. raw=125, decimals=2 -> actual value = 1.25
                    decimal scaledFlow = ScaleByDecimal(value.Value, decimals);
                    return FormatDecimal(scaledFlow, decimals, unit);
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

                    // OFL sentinel: 0xFFFFFFFF read as signed Int32 = -1
                    if (value.Value == -1m)
                        return "OFL";

                    // PLC stores weight as integer with implied decimal point.
                    // e.g. raw=31100, decimals=2 -> actual value = 311.00
                    decimal scaledWeight = ScaleByDecimal(value.Value, decimals);
                    return FormatDecimal(scaledWeight, decimals, unit);
                }

            default:
                // Generic numeric formatting for everything else.
                return value.Value.ToString("0.###");
        }
    }

    // =========================
    // Bitfield decoding helpers
    // =========================

    private static string DecodeWeightState(ushort w)
    {
        // Register 40001 (Weight state)
        // .0 0: Unstable, 1: Stable
        // .1 0: Non-zero, 1: Zero
        // .2 0: Positive sign, 1: Minus sign
        // .3 Weight overflow
        // .4 Negative weight overflow
        // .5 Millivolts overflow
        // .6 Millivolts negative overflow
        // .7 Millivolts stable: 1, unstable: 0
        // .8-.15 reserved

        var parts = new List<string>();

        // Binary meaning bits: always show the resolved meaning.
        parts.Add(Bit(w, 0) ? "Stable" : "Unstable");
        parts.Add(Bit(w, 1) ? "Zero" : "Non-zero");
        parts.Add(Bit(w, 2) ? "Minus" : "Plus");

        // Active-only flags:
        AddIf(parts, Bit(w, 3), "Weight overflow");
        AddIf(parts, Bit(w, 4), "Negative weight overflow");
        AddIf(parts, Bit(w, 5), "mV overflow");
        AddIf(parts, Bit(w, 6), "mV negative overflow");

        // Another binary meaning bit:
        parts.Add(Bit(w, 7) ? "mV stable" : "mV unstable");

        return string.Join(", ", parts);
    }

    private static string DecodeOperatingState(ushort w)
    {
        // Register 40002 (Operating state)
        // .0 0: Stop, 1: Run
        // .1 Before feed
        // .2 CO-Fill
        // .3 Fi-Fill
        // .4 Result Waiting
        // .5 Over/Under test
        // .6 DISC
        // .7 NearZero
        // .8 FILL
        // .9 Supplement Empty
        // .10 Stock-in/out Done
        // .11 Last Feed
        // .12 OVER
        // .13 UNDER
        // .14 Stop (yes, doc repeats "Stop" as a flag bit)
        // .15 reserved

        var parts = new List<string>();

        // Binary meaning bit: always show
        parts.Add(Bit(w, 0) ? "Run" : "Stop");

        // Active-only flags:
        AddIf(parts, Bit(w, 1), "Before feed");
        AddIf(parts, Bit(w, 2), "CO-Fill");
        AddIf(parts, Bit(w, 3), "Fi-Fill");
        AddIf(parts, Bit(w, 4), "Result waiting");
        AddIf(parts, Bit(w, 5), "Over/Under test");
        AddIf(parts, Bit(w, 6), "DISC");
        AddIf(parts, Bit(w, 7), "NearZero");
        AddIf(parts, Bit(w, 8), "FILL");
        AddIf(parts, Bit(w, 9), "Supplement empty");
        AddIf(parts, Bit(w, 10), "Stock-in/out done");
        AddIf(parts, Bit(w, 11), "Last feed");
        AddIf(parts, Bit(w, 12), "OVER");
        AddIf(parts, Bit(w, 13), "UNDER");
        AddIf(parts, Bit(w, 14), "Stop flag");

        return string.Join(", ", parts);
    }

    private static string DecodeConditionState(ushort w)
    {
        // Register 40003 (Condition state)
        // .0 Supplement FULL
        // .1 Supplement OK
        // .2 Supplement NotEmpty
        // .3 DISC Gate Closed Pos
        // .4 Fill Permission
        // .5 Cut Material (feed signal)
        // .6 Clogged(Out)
        // .7-.15 reserved

        var parts = new List<string>();

        // Active-only bits:
        AddIf(parts, Bit(w, 0), "Supplement FULL");
        AddIf(parts, Bit(w, 1), "Supplement OK");
        AddIf(parts, Bit(w, 2), "Supplement not empty");
        AddIf(parts, Bit(w, 3), "DISC gate closed");
        AddIf(parts, Bit(w, 4), "Fill permission");
        AddIf(parts, Bit(w, 5), "Cut material (feed)");
        AddIf(parts, Bit(w, 6), "Clogged (out)");

        return parts.Count == 0 ? "OK" : string.Join(", ", parts);
    }

    private static string DecodeAlarmInfo1(ushort w)
    {
        // Register 40004 (Alarm Info 1) - active-only
        var parts = new List<string>();

        AddIf(parts, Bit(w, 0), "Delivery done alarm");
        AddIf(parts, Bit(w, 1), "Fail: zero over range");
        AddIf(parts, Bit(w, 2), "Fail: weight unstable");
        AddIf(parts, Bit(w, 3), "Fail: process running");
        AddIf(parts, Bit(w, 4), "Target is 0: unable to start");
        AddIf(parts, Bit(w, 5), "Over/Under alarm");
        AddIf(parts, Bit(w, 6), "Weight OFL: unable to start");
        AddIf(parts, Bit(w, 7), "Continuous flowrate low");
        AddIf(parts, Bit(w, 8), "Stable judge overtime (scale unstable)");
        AddIf(parts, Bit(w, 9), "Target error: unable to start");
        AddIf(parts, Bit(w, 10), "Clear ACUM before next run");
        AddIf(parts, Bit(w, 11), "Discharge gate not closed");
        AddIf(parts, Bit(w, 12), "Feeding gate not closed");
        AddIf(parts, Bit(w, 13), "Feeding gate close overtime");
        AddIf(parts, Bit(w, 14), "Discharge gate open overtime");
        AddIf(parts, Bit(w, 15), "Discharge gate close overtime");

        return parts.Count == 0 ? "OK" : string.Join(", ", parts);
    }

    private static string DecodeAlarmInfo2(ushort w)
    {
        // Register 40005 (Alarm Info 2) - active-only
        var parts = new List<string>();

        AddIf(parts, Bit(w, 0), "Motor parameter error");
        AddIf(parts, Bit(w, 1), "Calibration fail: unstable");
        AddIf(parts, Bit(w, 2), "Calibration fail: loadcell input high");
        AddIf(parts, Bit(w, 3), "Calibration fail: loadcell input low");
        AddIf(parts, Bit(w, 4), "Calibration fail: unstable (dup)");
        AddIf(parts, Bit(w, 5), "Calibration fail: weight over");
        AddIf(parts, Bit(w, 6), "Calibration fail: weight under");
        AddIf(parts, Bit(w, 7), "Calibration fail: weight value error");
        AddIf(parts, Bit(w, 8), "Calibration fail: over resolution");
        AddIf(parts, Bit(w, 9), "Calibration fail: no gain voltage record");
        AddIf(parts, Bit(w, 10), "Over & Under pause");
        AddIf(parts, Bit(w, 11), "Fill timeout");
        AddIf(parts, Bit(w, 12), "Disc timeout");
        // .13-.15 reserved

        return parts.Count == 0 ? "OK" : string.Join(", ", parts);
    }

    private static bool Bit(ushort word, int bitIndex)
        => (word & (1 << bitIndex)) != 0;

    private static void AddIf(List<string> list, bool condition, string label)
    {
        if (condition) list.Add(label);
    }

    private static ushort ToUInt16(decimal d)
    {
        // Defensive conversion: your DB stores decimal, but this represents a 16-bit register.
        // Clamp safely.
        if (d < 0) return 0;
        if (d > ushort.MaxValue) return ushort.MaxValue;
        return (ushort)d;
    }

    // =========================
    // Existing helpers
    // =========================

    private static string FormatDecimal(decimal value, int decimals, string unit)
    {
        if (decimals < 0) decimals = 0;
        if (decimals > 6) decimals = 6;

        string num = Math.Round(value, decimals).ToString($"F{decimals}");
        return string.IsNullOrWhiteSpace(unit) ? num : $"{num} {unit}";
    }

    /// <summary>
    /// Converts a PLC integer-encoded value to its real decimal value.
    /// The PLC stores e.g. 31100 to represent 311.00 when decimals=2.
    /// Divide by 10^decimals to get the actual engineering value.
    /// </summary>
    private static decimal ScaleByDecimal(decimal raw, int decimals)
    {
        if (decimals <= 0) return raw;
        if (decimals > 6) decimals = 6;
        return raw / (decimal)Math.Pow(10, decimals);
    }

    private static int GetInt(IReadOnlyList<LatestReadingSnapshot> list, string key)
    {
        var item = list.FirstOrDefault(x => x.Key == key);
        if (item?.ValueNumeric is null) return 0;
        return (int)item.ValueNumeric.Value;
    }
}