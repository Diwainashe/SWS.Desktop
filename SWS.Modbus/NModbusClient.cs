using NModbus;
using SWS.Core.Abstractions;
using SWS.Core.Models;
using System.Net.Sockets;

namespace SWS.Modbus;

/// <summary>
/// NModbus implementation for Modbus TCP.
/// Converts manual-style addresses to 0-based offsets using ModbusAddressing.
/// </summary>
public sealed class NModbusClient : IModbusClient
{
    public async Task<ushort[]> ReadHoldingRegistersAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct)
    {
        ushort start = ModbusAddressing.HoldingToOffset(logicalAddress);
        return await ReadRegistersAsync(device, fc: RegisterFunction.Holding, start, length, ct);
    }

    public async Task<ushort[]> ReadInputRegistersAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct)
    {
        ushort start = ModbusAddressing.InputToOffset(logicalAddress);
        return await ReadRegistersAsync(device, fc: RegisterFunction.Input, start, length, ct);
    }

    public async Task<bool[]> ReadCoilsAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct)
    {
        ushort start = ModbusAddressing.CoilToOffset(logicalAddress);
        return await ReadBoolsAsync(device, boolType: BoolFunction.Coil, start, length, ct);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(
        DeviceConfig device,
        int logicalAddress,
        ushort length,
        CancellationToken ct)
    {
        ushort start = ModbusAddressing.DiscreteToOffset(logicalAddress);
        return await ReadBoolsAsync(device, boolType: BoolFunction.DiscreteInput, start, length, ct);
    }

    // ---------------- internal helpers ----------------

    private enum RegisterFunction { Holding, Input }
    private enum BoolFunction { Coil, DiscreteInput }

    private static async Task<ushort[]> ReadRegistersAsync(
        DeviceConfig device,
        RegisterFunction fc,
        ushort startAddress,
        ushort length,
        CancellationToken ct)
    {
        using var tcpClient = new TcpClient();

        // If cancellation triggers while connecting, dispose the socket
        using (ct.Register(() => tcpClient.Dispose()))
        {
            await tcpClient.ConnectAsync(device.IpAddress, device.Port);
        }

        var factory = new ModbusFactory();
        var master = factory.CreateMaster(tcpClient);

        // NModbus calls are sync; wrap in Task.Run to avoid blocking UI threads
        return await Task.Run(() =>
        {
            return fc switch
            {
                RegisterFunction.Holding => master.ReadHoldingRegisters(device.UnitId, startAddress, length),
                RegisterFunction.Input => master.ReadInputRegisters(device.UnitId, startAddress, length),
                _ => Array.Empty<ushort>()
            };
        }, ct);
    }

    private static async Task<bool[]> ReadBoolsAsync(
        DeviceConfig device,
        BoolFunction boolType,
        ushort startAddress,
        ushort length,
        CancellationToken ct)
    {
        using var tcpClient = new TcpClient();

        using (ct.Register(() => tcpClient.Dispose()))
        {
            await tcpClient.ConnectAsync(device.IpAddress, device.Port);
        }

        var factory = new ModbusFactory();
        var master = factory.CreateMaster(tcpClient);

        return await Task.Run(() =>
        {
            return boolType switch
            {
                BoolFunction.Coil => master.ReadCoils(device.UnitId, startAddress, length),
                BoolFunction.DiscreteInput => master.ReadInputs(device.UnitId, startAddress, length),
                _ => Array.Empty<bool>()
            };
        }, ct);
    }
}