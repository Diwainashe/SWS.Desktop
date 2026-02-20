using SWS.Core.Abstractions;
using SWS.Core.Models;

namespace SWS.Acquisition;

/// <summary>
/// Minimal decoder:
/// - supports 16-bit and 32-bit numeric types and float32
/// - applies scale
/// Next upgrades:
/// - word order per profile/point
/// - bitfield decoding for status words
/// </summary>
public sealed class BasicDecoder : IDecoder
{
    public decimal? DecodeNumeric(PointConfig point, ushort[] registers)
    {
        if (registers is null || registers.Length == 0)
            return null;

        try
        {
            decimal raw = point.DataType switch
            {
                PointDataType.UInt16 => registers[0],
                PointDataType.Int16 => (short)registers[0],

                PointDataType.UInt32 => CombineU32BigEndian(registers),
                PointDataType.Int32 => unchecked((int)CombineU32BigEndian(registers)),

                PointDataType.Float32 => (decimal)CombineFloat32BigEndian(registers),

                _ => registers[0]
            };

            // Apply scaling (raw * Scale)
            return raw * point.Scale;
        }
        catch
        {
            return null;
        }
    }

    private static uint CombineU32BigEndian(ushort[] regs)
    {
        if (regs.Length < 2)
            throw new ArgumentException("Need 2 registers for 32-bit value.");

        // Big-endian word order: regs[0] high, regs[1] low
        return ((uint)regs[0] << 16) | regs[1];
    }

    private static float CombineFloat32BigEndian(ushort[] regs)
    {
        uint u = CombineU32BigEndian(regs);
        byte[] bytes = BitConverter.GetBytes(u);
        return BitConverter.ToSingle(bytes, 0);
    }
}
