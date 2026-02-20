namespace SWS.Core.Models;

/// <summary>
/// The most recent reading per device+point.
/// UI reads this table for fast updates.
/// </summary>
public sealed class LatestReading
{
    public int Id { get; set; }
    public int DeviceConfigId { get; set; }
    public int PointConfigId { get; set; }

    public DateTime TimestampUtc { get; set; }

    public decimal? ValueNumeric { get; set; }
    public string? ValueText { get; set; }

    public ReadingQuality Quality { get; set; } = ReadingQuality.Good;

    public DateTime UpdatedUtc { get; set; }
}

public enum ReadingQuality
{
    Good,
    Timeout,
    Exception,
    BadData
}
