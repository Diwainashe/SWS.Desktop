using SWS.Core.Models;

namespace SWS.Core.Abstractions;

/// <summary>
/// Abstraction over Modbus reads so the rest of the app doesn't care
/// which library/transport is used (NModbus, RTU, etc).
/// </summary>
public interface IModbusClient
{
    /// <summary>
    /// Reads Holding Registers (Function Code 03).
    /// </summary>
    Task<ushort[]> ReadHoldingRegistersAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct);

    /// <summary>
    /// Reads Input Registers (Function Code 04).
    /// </summary>
    Task<ushort[]> ReadInputRegistersAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct);

    /// <summary>
    /// Reads Coils (Function Code 01). Returns one bool per coil.
    /// </summary>
    Task<bool[]> ReadCoilsAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct);

    /// <summary>
    /// Reads Discrete Inputs (Function Code 02). Returns one bool per input.
    /// </summary>
    Task<bool[]> ReadDiscreteInputsAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct);
}