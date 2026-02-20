using SWS.Core.Models;

namespace SWS.Core.Abstractions;

/// <summary>
/// Abstraction around Modbus library calls.
/// Keeps NModbus isolated to SWS.Modbus.
/// </summary>
public interface IModbusClient
{
    Task<ushort[]> ReadHoldingRegistersAsync(
        DeviceConfig device,
        int logicalHoldingRegisterAddress,
        ushort length,
        CancellationToken ct);
}
