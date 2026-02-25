namespace SWS.Core.Models;

/// <summary>
/// The most recent reading per device+point (fast UI refresh).
/// Numeric-only for good reads. ErrorText only used when Quality != Good.
/// </summary>
public sealed class LatestReading
{
    public int Id { get; set; }

    public int DeviceConfigId { get; set; }
    public int PointConfigId { get; set; }

    public DateTime TimestampLocal { get; set; }

    public decimal? ValueNumeric { get; set; }

    /// <summary>
    /// Only populated when an error occurs (Timeout/Exception/BadData).
    /// Keep normal readings numeric-only.
    /// </summary>
    public string? ErrorText { get; set; }

    public ReadingQuality Quality { get; set; } = ReadingQuality.Good;

    public DateTime UpdatedLocal { get; set; }
}

public enum ReadingQuality
{
    Good,
    Timeout,
    Exception,
    BadData
}