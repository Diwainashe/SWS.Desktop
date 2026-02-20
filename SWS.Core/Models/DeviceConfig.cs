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
    /// Modbus Unit Identifier (Slave ID). For many TCP devices this is 1 (often ignored),
    /// but keep it configurable for gateways/bridges.
    /// </summary>
    public byte UnitId { get; set; } = 1;
    public int PollMs { get; set; } = 1000;

    public bool IsEnabled { get; set; } = true;
}
