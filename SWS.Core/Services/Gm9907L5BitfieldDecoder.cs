using System.Collections.Generic;
using System.Linq;
using SWS.Core.Models;

namespace SWS.Core.Services;

/// <summary>
/// GM9907_L5 bitfield decoder.
/// Correctly supports:
/// - Flag bits: only shown when active
/// - Two-state bits: always shown (0 meaning OR 1 meaning)
/// </summary>
public sealed class Gm9907L5BitfieldDecoder : IGm9907L5BitfieldDecoder
{
    // ---- Snapshot keys (stable contract) ----
    private const string KEY_WEIGHT_STATE = "State.Weight";      // 40001
    private const string KEY_OPERATING_STATE = "State.Operating";   // 40002
    private const string KEY_CONDITION_STATE = "State.Condition";   // 40003
    private const string KEY_ALARM_INFO1 = "Alarm.Info1";       // 40004
    private const string KEY_ALARM_INFO2 = "Alarm.Info2";       // 40005

    // ---- Bit definitions (from your sheet) ----

    private static readonly BitDefinition[] WeightState =
    {
        new BitDefinition.TwoState(0, "Unstable weight", "Stable weight"),
        new BitDefinition.TwoState(1, "Non-zero", "Zero"),
        new BitDefinition.TwoState(2, "Sign (+)", "Sign (-)"),
        new BitDefinition.Flag(3, "Weight overflow"),
        new BitDefinition.Flag(4, "Negative weight overflow"),
        new BitDefinition.Flag(5, "Millivolts overflow"),
        new BitDefinition.Flag(6, "Millivolts negative overflow"),
        new BitDefinition.TwoState(7, "Millivolts unstable", "Millivolts stable"),
        // 8..15 reserved
    };

    private static readonly BitDefinition[] OperatingState =
    {
        new BitDefinition.TwoState(0, "Stop", "Run"),
        new BitDefinition.Flag(1, "Before feed"),
        new BitDefinition.Flag(2, "CO-Fill"),
        new BitDefinition.Flag(3, "Fi-Fill"),
        new BitDefinition.Flag(4, "Result waiting"),
        new BitDefinition.Flag(5, "Over/Under test"),
        new BitDefinition.Flag(6, "DISC"),
        new BitDefinition.Flag(7, "NearZero"),
        new BitDefinition.Flag(8, "FILL"),
        new BitDefinition.Flag(9, "Supplement empty"),
        new BitDefinition.Flag(10, "Stock-in/out done"),
        new BitDefinition.Flag(11, "Last feed"),
        new BitDefinition.Flag(12, "OVER"),
        new BitDefinition.Flag(13, "UNDER"),
        // Bit14 in your sheet says "Stop" too, which clashes with bit0.
        // Treat it as a flag so it only appears when active.
        new BitDefinition.Flag(14, "Stop"),
        // 15 reserved
    };

    private static readonly BitDefinition[] ConditionState =
    {
        new BitDefinition.Flag(0, "Supplement FULL"),
        new BitDefinition.Flag(1, "Supplement OK"),
        new BitDefinition.Flag(2, "Supplement NotEmpty"),
        new BitDefinition.Flag(3, "DISC gate closed position"),
        new BitDefinition.Flag(4, "Fill permission"),
        new BitDefinition.Flag(5, "Cut material (feed signal)"),
        new BitDefinition.Flag(6, "Clogged (Out)"),
        // 7..15 reserved
    };

    private static readonly BitDefinition[] AlarmInfo1 =
    {
        new BitDefinition.Flag(0, "Delivery done alarm"),
        new BitDefinition.Flag(1, "Fail: Zero over range"),
        new BitDefinition.Flag(2, "Fail: Weight unstable"),
        new BitDefinition.Flag(3, "Fail: Process running"),
        new BitDefinition.Flag(4, "Target is 0: Unable to start"),
        new BitDefinition.Flag(5, "Over/Under alarm"),
        new BitDefinition.Flag(6, "Weight OFL: Unable to start"),
        new BitDefinition.Flag(7, "Continuous flowrate low"),
        new BitDefinition.Flag(8, "Stable judge overtime: scale unstable"),
        new BitDefinition.Flag(9, "Target error: Unable to start"),
        new BitDefinition.Flag(10, "Clear ACUM before next run"),
        new BitDefinition.Flag(11, "Discharge gate not closed"),
        new BitDefinition.Flag(12, "Feeding gate not closed"),
        new BitDefinition.Flag(13, "Feeding gate close overtime"),
        new BitDefinition.Flag(14, "Discharge gate open overtime"),
        new BitDefinition.Flag(15, "Discharge gate close overtime"),
    };

    private static readonly BitDefinition[] AlarmInfo2 =
    {
        new BitDefinition.Flag(0, "Motor parameter error"),
        new BitDefinition.Flag(1, "Calibration fail: Unstable"),
        new BitDefinition.Flag(2, "Calibration fail: Loadcell input high"),
        new BitDefinition.Flag(3, "Calibration fail: Loadcell input low"),
        new BitDefinition.Flag(4, "Calibration fail: Unstable"),
        new BitDefinition.Flag(5, "Calibration fail: Weight over"),
        new BitDefinition.Flag(6, "Calibration fail: Weight under"),
        new BitDefinition.Flag(7, "Calibration fail: Weight value error"),
        new BitDefinition.Flag(8, "Calibration fail: Over resolution"),
        new BitDefinition.Flag(9, "Calibration fail: No gain voltage record"),
        new BitDefinition.Flag(10, "Over & Under pause"),
        new BitDefinition.Flag(11, "Fill timeout"),
        new BitDefinition.Flag(12, "Disc timeout"),
        // 13..15 reserved
    };

    public IReadOnlyList<BitFlagState> DecodeWeightState(IReadOnlyList<LatestReadingSnapshot> readings)
        => Decode(KEY_WEIGHT_STATE, readings, WeightState, includeInactive: false);

    public IReadOnlyList<BitFlagState> DecodeOperatingState(IReadOnlyList<LatestReadingSnapshot> readings)
        => Decode(KEY_OPERATING_STATE, readings, OperatingState, includeInactive: false);

    public IReadOnlyList<BitFlagState> DecodeConditionState(IReadOnlyList<LatestReadingSnapshot> readings)
        => Decode(KEY_CONDITION_STATE, readings, ConditionState, includeInactive: false);

    public IReadOnlyList<BitFlagState> GetActiveAlarms(IReadOnlyList<LatestReadingSnapshot> readings)
    {
        var a1 = Decode(KEY_ALARM_INFO1, readings, AlarmInfo1, includeInactive: false);
        var a2 = Decode(KEY_ALARM_INFO2, readings, AlarmInfo2, includeInactive: false);
        return a1.Concat(a2).ToList();
    }

    private static IReadOnlyList<BitFlagState> Decode(
        string key,
        IReadOnlyList<LatestReadingSnapshot> readings,
        IReadOnlyList<BitDefinition> defs,
        bool includeInactive)
    {
        ushort raw = GetU16(readings, key);

        var result = new List<BitFlagState>(defs.Count);

        foreach (var def in defs)
        {
            bool isOn = IsBitSet(raw, def.Bit);

            switch (def)
            {
                case BitDefinition.TwoState ts:
                    {
                        // Always produce exactly one meaning
                        result.Add(new BitFlagState
                        {
                            SourceKey = key,
                            Bit = ts.Bit,
                            Label = isOn ? ts.WhenOne : ts.WhenZero,
                            IsActive = true // "active" here means "this is the current state"
                        });
                        break;
                    }

                case BitDefinition.Flag fl:
                    {
                        if (!includeInactive && !isOn)
                            break;

                        result.Add(new BitFlagState
                        {
                            SourceKey = key,
                            Bit = fl.Bit,
                            Label = fl.Label,
                            IsActive = isOn
                        });
                        break;
                    }
            }
        }

        // Optional: if includeInactive=false, remove any “inactive” flags but keep TwoState items
        if (!includeInactive)
            result = result.Where(x => x.IsActive).ToList();

        return result;
    }

    private static bool IsBitSet(ushort value, int bit)
        => bit is >= 0 and <= 15 && (value & (1 << bit)) != 0;

    private static ushort GetU16(IReadOnlyList<LatestReadingSnapshot> readings, string key)
    {
        var row = readings.FirstOrDefault(x => x.Key == key);

        if (row?.ValueNumeric is null)
            return 0;

        if (row.Quality != ReadingQuality.Good)
            return 0;

        int v = (int)row.ValueNumeric.Value;
        if (v < 0) v = 0;
        if (v > ushort.MaxValue) v = ushort.MaxValue;
        return (ushort)v;
    }
}