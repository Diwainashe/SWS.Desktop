namespace SWS.Core.Models;

/// <summary>
/// One decoded bit from a UInt16 bitfield register.
/// Used for alarms AND operating/condition/weight state flags.
/// </summary>
public sealed class BitFlagState
{
    /// <summary>Which snapshot key produced this flag (e.g. "Alarm.Info1", "State.Operating").</summary>
    public string SourceKey { get; init; } = "";

    /// <summary>Bit position 0..15.</summary>
    public int Bit { get; init; }

    /// <summary>Human label for the meaning of this bit.</summary>
    public string Label { get; init; } = "";

    /// <summary>True if this bit is set.</summary>
    public bool IsActive { get; init; }
}