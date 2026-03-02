using SWS.Core.Models;

namespace SWS.Core.Models;

/// <summary>
/// A lightweight, UI-ready snapshot of the latest value for a point.
/// Lives in Core because device profiles (also in Core) use it for formatting.
/// </summary>
public sealed class LatestReadingSnapshot
{
    public int DeviceId { get; set; }
    public string DeviceName { get; set; } = "";

    public DeviceType DeviceType { get; set; }

    public int PointId { get; set; }
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Unit { get; set; } = "";

    /// <summary>
    /// Critical: lets us format booleans correctly and avoid ON/OFF for numeric 0/1 values.
    /// </summary>
    public PointDataType DataType { get; set; }

    public decimal? ValueNumeric { get; set; }
    public ReadingQuality Quality { get; set; }
    public DateTime TimestampLocal { get; set; }
}