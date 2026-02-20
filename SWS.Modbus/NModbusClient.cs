using NModbus;
using System.Net.Sockets;
using SWS.Core.Abstractions;
using SWS.Core.Models;

namespace SWS.Modbus;

/// <summary>
/// NModbus implementation for Modbus TCP.
/// Converts "manual-style" holding register addresses (40001) to 0-based offsets.
/// </summary>
public sealed class NModbusClient : IModbusClient
{
    /// <summary>
    /// Reads holding registers using Modbus function code 03.
    /// </summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(
        DeviceConfig device,
        int logicalHoldingRegisterAddress,
        ushort length,
        CancellationToken ct)
    {
        // Convert manual-style holding register address (e.g. 40001) to 0-based address
        ushort startAddress = ToZeroBasedHoldingAddress(logicalHoldingRegisterAddress);

        using var tcpClient = new TcpClient();

        // Respect cancellation: dispose socket if cancellation triggers mid-connect
        using (ct.Register(() => tcpClient.Dispose()))
        {
            await tcpClient.ConnectAsync(device.IpAddress, device.Port);
        }

        var factory = new ModbusFactory();
        var master = factory.CreateMaster(tcpClient);

        // NModbus uses the "slaveId" parameter even on TCP. Use device.UnitId (default 1).
        ushort[] regs = master.ReadHoldingRegisters(device.UnitId, startAddress, length);

        return regs;
    }

    private static ushort ToZeroBasedHoldingAddress(int logicalAddress)
    {
        // We standardize the DB on manual addresses:
        // 40001 -> zero-based 0
        // 40002 -> zero-based 1
        // ...
        const int holdingBase = 40001;

        if (logicalAddress < holdingBase)
        {
            // Allow already-0-based addresses (useful for testing)
            if (logicalAddress >= 0 && logicalAddress <= ushort.MaxValue)
                return (ushort)logicalAddress;

            throw new ArgumentOutOfRangeException(
                nameof(logicalAddress),
                "Holding register logical address should be >= 40001 (manual style).");
        }

        int zeroBased = logicalAddress - holdingBase;

        if (zeroBased < 0 || zeroBased > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(logicalAddress));

        return (ushort)zeroBased;
    }
}
