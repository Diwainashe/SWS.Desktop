using System;
using SWS.Core.Models;

namespace SWS.Modbus
{
    /// <summary>
    /// Converts Modbus 16-bit registers into typed values based on PointConfig.DataType.
    /// Word order differs between devices. We'll start with a default and make it configurable later.
    /// </summary>
    public static class ModbusDecoder
    {
        public static decimal? DecodeToDecimal(ushort[] regs, PointConfig point)
        {
            if (regs == null || regs.Length == 0)
                return null;

            // Apply scaling at the end so the raw decoding stays consistent
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

        public static string DecodeToText(ushort[] regs, PointConfig point)
        {
            if (regs == null || regs.Length == 0)
                return string.Empty;

            var numeric = DecodeToDecimal(regs, point);
            if (numeric is not null)
                return numeric.Value.ToString();

            // Fallback: show raw registers
            return string.Join(",", regs);
        }

        /// <summary>
        /// Combine two 16-bit registers into a 32-bit unsigned value.
        /// Default order here is Hi,Lo (regs[0] high word, regs[1] low word).
        /// If a device uses Lo,Hi, we will make that configurable later.
        /// </summary>
        private static uint CombineUInt32(ushort hi, ushort lo)
        {
            return ((uint)hi << 16) | lo;
        }

        /// <summary>
        /// Combine two 16-bit registers into a float (IEEE 754).
        /// Default order here is Hi,Lo.
        /// </summary>
        private static float CombineFloat32(ushort hi, ushort lo)
        {
            uint raw = CombineUInt32(hi, lo);
            var bytes = BitConverter.GetBytes(raw);

            // BitConverter expects system endianness; raw is already in correct word order
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
