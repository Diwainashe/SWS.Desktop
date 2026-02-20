using System;

namespace SWS.Modbus;

using SWS.Core.Models;

public static class ModbusDecoder
{
    public static decimal? DecodeToNumeric(ushort[] regs, PointConfig point)
    {
        if (regs is null || regs.Length == 0)
            return null;

        decimal? raw = point.DataType switch
        {
            PointDataType.UInt16 => regs[0],
            PointDataType.Int16 => unchecked((short)regs[0]),

            PointDataType.UInt32 => regs.Length >= 2 ? CombineUInt32(regs[0], regs[1]) : null,
            PointDataType.Int32 => regs.Length >= 2 ? unchecked((int)CombineUInt32(regs[0], regs[1])) : null,

            PointDataType.Float32 => regs.Length >= 2 ? (decimal?)CombineFloat32(regs[0], regs[1]) : null,

            _ => null
        };

        return raw is null ? null : raw.Value * point.Scale;
    }

    private static uint CombineUInt32(ushort hi, ushort lo)
        => ((uint)hi << 16) | lo;

    private static float CombineFloat32(ushort hi, ushort lo)
    {
        uint raw = CombineUInt32(hi, lo);
        var bytes = BitConverter.GetBytes(raw);
        return BitConverter.ToSingle(bytes, 0);
    }
}