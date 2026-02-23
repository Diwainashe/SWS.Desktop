namespace SWS.Core.Models;

/// <summary>
/// A PointTemplate is a reusable definition of a value the app knows about,
/// without hardcoding device addresses.
/// 
/// Example: "Flowrate.Actual" means "current flowrate", and we set Unit="t/h",
/// but we do NOT set a Modbus address here.
/// 
/// The user maps templates -> PointConfig per device.
/// </summary>
public sealed class PointTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Template key used by the app (stable across brands/models).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human label for UI selection.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Unit for display (e.g. "kg", "t/h").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Optional: which area is typical for this point.
    /// A user can override when creating PointConfig.
    /// </summary>
    public ModbusPointArea DefaultArea { get; set; } = ModbusPointArea.HoldingRegister;

    /// <summary>
    /// Typical data type (user can override).
    /// </summary>
    public PointDataType DefaultDataType { get; set; } = PointDataType.UInt16;

    /// <summary>
    /// Typical register length for this datatype (1 for 16-bit, 2 for 32-bit/float).
    /// </summary>
    public ushort DefaultLength { get; set; } = 1;

    /// <summary>
    /// Typical engineering scale (user can override).
    /// </summary>
    public decimal Scale { get; set; } = 1m;

    /// <summary>
    /// Suggested polling rate.
    /// </summary>
    public int PollRateMs { get; set; } = 500;

    /// <summary>
    /// Whether this point is a good default for tile display.
    /// </summary>
    public bool IsEssential { get; set; } = false;

    /// <summary>
    /// Default historian behavior.
    /// </summary>
    public bool LogToHistory { get; set; } = false;

    public int HistoryIntervalMs { get; set; } = 60000;

    /// <summary>
    /// Optional: helps you group templates per device family.
    /// Example: "GM9907-L5"
    /// </summary>
    public string DeviceType { get; set; } = "Generic";
}