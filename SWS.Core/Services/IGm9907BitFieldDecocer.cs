using SWS.Core.Models;

namespace SWS.Core.Services;

/// <summary>
/// Decodes GM9907_L5 bitfield registers into named flags.
/// This includes:
/// - Weight state (40001)
/// - Operating state (40002)
/// - Condition state (40003)
/// - Alarm info 1 (40004)
/// - Alarm info 2 (40005)
/// </summary>
public interface IGm9907L5BitfieldDecoder
{
    IReadOnlyList<BitFlagState> DecodeWeightState(IReadOnlyList<LatestReadingSnapshot> readings);
    IReadOnlyList<BitFlagState> DecodeOperatingState(IReadOnlyList<LatestReadingSnapshot> readings);
    IReadOnlyList<BitFlagState> DecodeConditionState(IReadOnlyList<LatestReadingSnapshot> readings);

    /// <summary>Returns only ACTIVE alarms (Info1 + Info2).</summary>
    IReadOnlyList<BitFlagState> GetActiveAlarms(IReadOnlyList<LatestReadingSnapshot> readings);
}