using System;
using System.Collections.Generic;
using System.Linq;
using SWS.Core.Diagnostics;
using SWS.Core.Models;

namespace SWS.Desktop.Services;

/// <summary>
/// Takes the latest snapshot rows for ONE device and produces:
/// - Active alarms (from Alarm.Info1 + Alarm.Info2)
/// - Active states (weight/operating/condition)
/// This keeps “decoding” out of the ViewModel.
/// </summary>
public sealed class Gm9907L5DiagnosticsService
{
    public Gm9907L5DiagnosticsResult Build(IReadOnlyList<LatestReadingSnapshot> deviceRows)
    {
        // Safely extract 16-bit integers from the polled snapshots.
        ushort weightState = GetU16(deviceRows, Gm9907L5Bitfields.WeightStateKey);
        ushort opState = GetU16(deviceRows, Gm9907L5Bitfields.OperatingStateKey);
        ushort condState = GetU16(deviceRows, Gm9907L5Bitfields.ConditionStateKey);
        ushort alarm1 = GetU16(deviceRows, Gm9907L5Bitfields.AlarmInfo1Key);
        ushort alarm2 = GetU16(deviceRows, Gm9907L5Bitfields.AlarmInfo2Key);

        // Decode active bits to human-readable messages.
        var activeAlarms = new List<string>();
        activeAlarms.AddRange(Gm9907L5Bitfields.DecodeActiveBits(alarm1, Gm9907L5Bitfields.AlarmInfo1Bits));
        activeAlarms.AddRange(Gm9907L5Bitfields.DecodeActiveBits(alarm2, Gm9907L5Bitfields.AlarmInfo2Bits));

        var activeStates = new List<string>();
        activeStates.AddRange(Gm9907L5Bitfields.DecodeActiveBits(weightState, Gm9907L5Bitfields.WeightStateBits));
        activeStates.AddRange(Gm9907L5Bitfields.DecodeActiveBits(opState, Gm9907L5Bitfields.OperatingStateBits));
        activeStates.AddRange(Gm9907L5Bitfields.DecodeActiveBits(condState, Gm9907L5Bitfields.ConditionStateBits));

        return new Gm9907L5DiagnosticsResult(activeAlarms, activeStates);
    }

    private static ushort GetU16(IReadOnlyList<LatestReadingSnapshot> rows, string key)
    {
        var row = rows.FirstOrDefault(x => x.Key == key);

        // If missing or null, treat as 0 (no bits set).
        if (row?.ValueNumeric is null)
            return 0;

        // Clamp to UInt16 range safely
        var v = row.ValueNumeric.Value;
        if (v < 0) return 0;
        if (v > ushort.MaxValue) return ushort.MaxValue;

        return (ushort)v;
    }
}

/// <summary>
/// Result type returned to the UI.
/// </summary>
public sealed record Gm9907L5DiagnosticsResult(
    IReadOnlyList<string> ActiveAlarms,
    IReadOnlyList<string> ActiveStates);