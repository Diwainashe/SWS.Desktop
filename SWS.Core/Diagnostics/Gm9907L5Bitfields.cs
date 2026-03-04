using System;
using System.Collections.Generic;

namespace SWS.Core.Diagnostics;

/// <summary>
/// GM9907-L5 bitfield definitions (manual style).
/// These are NOT "points" - they are interpretations of registers.
/// We keep them in Core so Desktop + future apps can reuse them.
/// </summary>
public static class Gm9907L5Bitfields
{
    // ---------------------------
    // Register keys (match your PointConfig keys)
    // ---------------------------
    public const string WeightStateKey = "State.Weight";     // map this to 40001
    public const string OperatingStateKey = "State.Operating";  // map this to 40002
    public const string ConditionStateKey = "State.Condition";  // map this to 40003
    public const string AlarmInfo1Key = "Alarm.Info1";      // 40004
    public const string AlarmInfo2Key = "Alarm.Info2";      // 40005

    /// <summary>Bit descriptions for 40001 Weight state.</summary>
    public static readonly IReadOnlyDictionary<int, string> WeightStateBits =
        new Dictionary<int, string>
        {
            { 0, "Weight stable (0=unstable, 1=stable)" },
            { 1, "Zero flag (0=non-zero, 1=zero)" },
            { 2, "Sign (0=positive, 1=minus)" },
            { 3, "Weight overflow" },
            { 4, "Negative weight overflow" },
            { 5, "Millivolts overflow" },
            { 6, "Millivolts negative overflow" },
            { 7, "Millivolts stable (1=stable, 0=unstable)" },
            // 8..15 reserved
        };

    /// <summary>Bit descriptions for 40002 Operating state.</summary>
    public static readonly IReadOnlyDictionary<int, string> OperatingStateBits =
        new Dictionary<int, string>
        {
            { 0,  "Run flag (0=stop, 1=run)" },
            { 1,  "Before feed" },
            { 2,  "CO-Fill" },
            { 3,  "Fi-Fill" },
            { 4,  "Result waiting" },
            { 5,  "Over/Under test" },
            { 6,  "DISC" },
            { 7,  "NearZero" },
            { 8,  "FILL" },
            { 9,  "Supplement empty" },
            { 10, "Stock-in/out done" },
            { 11, "Last feed" },
            { 12, "OVER" },
            { 13, "UNDER" },
            { 14, "Stop" },
            // 15 reserved
        };

    /// <summary>Bit descriptions for 40003 Condition state.</summary>
    public static readonly IReadOnlyDictionary<int, string> ConditionStateBits =
        new Dictionary<int, string>
        {
            { 0, "Supplement FULL" },
            { 1, "Supplement OK" },
            { 2, "Supplement NotEmpty" },
            { 3, "DISC gate closed position" },
            { 4, "Fill permission" },
            { 5, "Cut material / feed signal" },
            { 6, "Clogged (Out)" },
            // 7..15 reserved
        };

    /// <summary>Bit descriptions for 40004 Alarm Info 1.</summary>
    public static readonly IReadOnlyDictionary<int, string> AlarmInfo1Bits =
        new Dictionary<int, string>
        {
            { 0, "Delivery done alarm" },
            { 1, "Fail: zero over range" },
            { 2, "Fail: weight unstable" },
            { 3, "Fail: process running" },
            { 4, "Target is 0, unable to start" },
            { 5, "Over/Under alarm" },
            { 6, "Weight OFL, unable to start" },
            { 7, "Continuous flowrate low" },
            { 8, "Stable judge overtime (scale unstable)" },
            { 9, "Target error, unable to start" },
            { 10, "Clear ACUM before next run" },
            { 11, "Discharge gate not closed" },
            { 12, "Feeding gate not closed" },
            { 13, "Feeding gate close overtime" },
            { 14, "Discharge gate open overtime" },
            { 15, "Discharge gate close overtime" }
        };

    /// <summary>Bit descriptions for 40005 Alarm Info 2.</summary>
    public static readonly IReadOnlyDictionary<int, string> AlarmInfo2Bits =
        new Dictionary<int, string>
        {
            { 0, "Motor parameter error" },
            { 1, "Calibration fail: unstable" },
            { 2, "Calibration fail: loadcell input high" },
            { 3, "Calibration fail: loadcell input low" },
            { 4, "Calibration fail: unstable" },
            { 5, "Calibration fail: weight over" },
            { 6, "Calibration fail: weight under" },
            { 7, "Calibration fail: weight value error" },
            { 8, "Calibration fail: over resolution" },
            { 9, "Calibration fail: no gain voltage record" },
            { 10, "Over & Under pause" },
            { 11, "Fill timeout" },
            { 12, "Disc timeout" }
            // 13..15 reserved
        };

    /// <summary>
    /// Returns the active bit messages for a 16-bit value given a bit dictionary.
    /// </summary>
    public static List<string> DecodeActiveBits(ushort value, IReadOnlyDictionary<int, string> bitMap)
    {
        var active = new List<string>();

        foreach (var kv in bitMap)
        {
            int bit = kv.Key;
            bool isSet = (value & (1 << bit)) != 0;
            if (isSet)
                active.Add(kv.Value);
        }

        return active;
    }
}