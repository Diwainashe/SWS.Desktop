namespace SWS.Core.Models;

/// <summary>
/// A "point" defines what to read from a Modbus device:
/// area (holding/input/coil/discrete), address, length, datatype, scaling, polling, and history policy.
/// </summary>
public sealed class PointConfig
{
    public int Id { get; set; }

    public int DeviceConfigId { get; set; }

    /// <summary>
    /// Unique key for the meaning of the point (e.g. "Weight.Display", "Flowrate.Actual").
    /// This is what lets you support multi-brand: the app reasons about keys, not addresses.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human label for UI (tiles, configuration screens).
    /// Example: "Weight", "Flowrate", "Alarm Code".
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Engineering unit shown in UI.
    /// Example: "kg", "t/h". Keep it empty for unitless values / flags.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Which Modbus area this point belongs to.
    /// </summary>
    public ModbusPointArea Area { get; set; } = ModbusPointArea.HoldingRegister;

    /// <summary>
    /// Store addresses in the same style as manuals (e.g. 400007).
    /// We'll convert to 0-based inside the Modbus wrapper.
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// Number of 16-bit registers to read (only used for register areas).
    /// 2 for 32-bit values (Int32/UInt32/Float32).
    /// </summary>
    public ushort Length { get; set; } = 1;

    public PointDataType DataType { get; set; } = PointDataType.UInt16;

    /// <summary>
    /// Scales raw -> engineering units.
    /// Example: 0.01 => raw * 0.01.
    /// </summary>
    public decimal Scale { get; set; } = 1m;

    /// <summary>
    /// Polling period for live reads.
    /// </summary>
    public int PollRateMs { get; set; } = 500;

    /// <summary>
    /// Marks points that should be used on the device tile on the main dashboard.
    /// </summary>
    public bool IsEssential { get; set; } = false;

    /// <summary>
    /// If true, this point may be written to ReadingHistory (append-only).
    /// </summary>
    public bool LogToHistory { get; set; } = false;

    /// <summary>
    /// Minimum interval between history records for this point.
    /// Example: 1000 = at most once per second, 60000 = once per minute.
    /// </summary>
    public int HistoryIntervalMs { get; set; } = 60000;
}

public enum ModbusPointArea
{
    HoldingRegister = 0,
    InputRegister = 1,
    Coil = 2,
    DiscreteInput = 3
}

public enum PointDataType
{
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float32,
    Bool
}