using System;
using SWS.Core.Models;

namespace SWS.Core.Modbus;

/// <summary>
/// Converts manual-style Modbus addresses (40001, 30001 etc)
/// into protocol offsets expected by NModbus (0-based).
///
/// We keep manual addresses in templates for readability,
/// and convert ONLY at read time.
/// </summary>
public static class ModbusAddress
{
    public static ushort ToOffset(ModbusPointArea area, int manualAddress)
    {
        if (manualAddress <= 0)
            throw new ArgumentOutOfRangeException(nameof(manualAddress));

        return area switch
        {
            ModbusPointArea.HoldingRegister => Convert40000(manualAddress),
            ModbusPointArea.InputRegister => Convert30000(manualAddress),
            ModbusPointArea.Coil => Convert00000(manualAddress),
            ModbusPointArea.DiscreteInput => Convert10000(manualAddress),
            _ => throw new ArgumentOutOfRangeException(nameof(area))
        };
    }

    private static ushort Convert40000(int a)
    {
        if (a >= 40001 && a <= 49999)
            return (ushort)(a - 40001);

        return (ushort)a; // already offset
    }

    private static ushort Convert30000(int a)
    {
        if (a >= 30001 && a <= 39999)
            return (ushort)(a - 30001);

        return (ushort)a;
    }

    private static ushort Convert00000(int a)
    {
        if (a >= 1 && a <= 9999)
            return (ushort)(a - 1);

        return (ushort)a;
    }

    private static ushort Convert10000(int a)
    {
        if (a >= 10001 && a <= 19999)
            return (ushort)(a - 10001);

        return (ushort)a;
    }
}