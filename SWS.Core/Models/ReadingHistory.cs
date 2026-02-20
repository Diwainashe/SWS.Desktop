namespace SWS.Core.Models;

/// <summary>
/// Append-only historian table for trending/reporting.
/// Numeric-only for good reads. ErrorText only for error rows.
/// </summary>
public sealed class ReadingHistory
{
    public long Id { get; set; }

    public int DeviceConfigId { get; set; }
    public int PointConfigId { get; set; }

    public DateTime TimestampUtc { get; set; }

    public decimal? ValueNumeric { get; set; }

    public string? ErrorText { get; set; }

    public ReadingQuality Quality { get; set; } = ReadingQuality.Good;
}