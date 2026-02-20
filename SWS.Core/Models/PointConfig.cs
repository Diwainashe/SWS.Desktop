namespace SWS.Core.Models;

/// <summary>
/// A "point" defines what to read from a Modbus device:
/// address, length, datatype, scaling, polling rate.
/// </summary>
public sealed class PointConfig
{
    public int Id { get; set; }

    public int DeviceConfigId { get; set; }

    public string Key { get; set; } = string.Empty; // e.g. "Weight.Display"

    /// <summary>
    /// Store addresses in the same style as manuals (e.g. 40001).
    /// We'll convert to 0-based inside the Modbus client wrapper.
    /// </summary>
    public int Address { get; set; }  // e.g. 40001

    /// <summary>
    /// Number of 16-bit registers to read. 2 for 32-bit values.
    /// </summary>
    public ushort Length { get; set; } = 1;

    public PointDataType DataType { get; set; } = PointDataType.UInt16;

    /// <summary>
    /// Scales raw -> engineering units. Example: 0.01 => divide by 100.
    /// </summary>
    public decimal Scale { get; set; } = 1m;

    /// <summary>
    /// Polling period. We'll use this later to separate fast/slow polling groups.
    /// </summary>
    public int PollRateMs { get; set; } = 500;

    /// <summary>
    /// Marks points that should be used on the device tile on the main dashboard.
    /// </summary>
    public bool IsEssential { get; set; } = false;
}

public enum PointDataType
{
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float32
}
