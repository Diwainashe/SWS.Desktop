using NModbus;
using SWS.Core.Abstractions;
using SWS.Core.Models;
using System.Net.Sockets;
using SWS.Core.Modbus;

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
        try
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
                try
                {
                    return fc switch
                    {
                        RegisterFunction.Holding => master.ReadHoldingRegisters(device.UnitId, startAddress, length),
                        RegisterFunction.Input => master.ReadInputRegisters(device.UnitId, startAddress, length),
                        _ => Array.Empty<ushort>()
                    };
                }
                catch (NModbus.SlaveException)
                {
                    // Illegal address / illegal function / etc.
                    return Array.Empty<ushort>();
                }
            }, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            // Socket errors, timeouts, etc.
            return Array.Empty<ushort>();
        }
    }

    private static async Task<bool[]> ReadBoolsAsync(
    DeviceConfig device,
    BoolFunction boolType,
    ushort startAddress,
    ushort length,
    CancellationToken ct)
    {
        try
        {
            if (length == 0) length = 1;

            using var tcpClient = new TcpClient();

            using (ct.Register(() => tcpClient.Dispose()))
            {
                await tcpClient.ConnectAsync(device.IpAddress, device.Port);
            }

            var factory = new ModbusFactory();
            var master = factory.CreateMaster(tcpClient);

            return await Task.Run(() =>
            {
                try
                {
                    return boolType switch
                    {
                        BoolFunction.Coil => master.ReadCoils(device.UnitId, startAddress, length),
                        BoolFunction.DiscreteInput => master.ReadInputs(device.UnitId, startAddress, length),
                        _ => Array.Empty<bool>()
                    };
                }
                catch (NModbus.SlaveException)
                {
                    // Illegal address/function/etc.
                    return Array.Empty<bool>();
                }
                catch
                {
                    return Array.Empty<bool>();
                }
            }, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return Array.Empty<bool>();
        }
    }
}