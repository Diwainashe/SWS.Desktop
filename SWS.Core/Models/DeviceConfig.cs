namespace SWS.Core.Models;

/// <summary>
/// Connection and enable/disable state for a Modbus TCP device.
/// Stored in DB so you can add devices without code changes.
/// </summary>
public sealed class DeviceConfig
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; } = 502;

    /// <summary>
    /// Modbus Unit Identifier (Slave ID).
    /// </summary>
    public byte UnitId { get; set; } = 1;

    public int PollMs { get; set; } = 1000;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Controls which default template pack to offer (e.g., GM9907_L5).
    /// Stored as int in DB by EF.
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Generic;
}

public enum DeviceType
{
    Generic = 0,
    GM9907_L5 = 1
}