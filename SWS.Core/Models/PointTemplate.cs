namespace SWS.Core.Models;

/// <summary>
/// A reusable template that describes a point for a specific device type.
/// These are copied into PointConfigs when you press "Load Defaults".
/// </summary>
public sealed class PointTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Device type string (matches enum name usually e.g. "GM9907_L5").
    /// Using string keeps DB flexible.
    /// </summary>
    public string DeviceType { get; set; } = "Generic";

    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Unit { get; set; } = "";

    public ModbusPointArea Area { get; set; } = ModbusPointArea.HoldingRegister;

    /// <summary>
    /// Manual-style address (e.g. 400007).
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// SQL Server has no ushort. We store int in DB and cast to ushort when copying.
    /// </summary>
    public ushort DefaultLength { get; set; } = 1;

    public PointDataType DataType { get; set; } = PointDataType.UInt16;
    public decimal Scale { get; set; } = 1m;

    public int PollRateMs { get; set; } = 5000;

    public bool IsEssential { get; set; } = false;
    public bool LogToHistory { get; set; } = false;
    public int HistoryIntervalMs { get; set; } = 60000;
}