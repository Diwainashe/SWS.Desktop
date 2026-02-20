namespace SWS.Modbus;

/// <summary>
/// Central place for converting "manual-style" PLC addresses (e.g. 40001)
/// into NModbus 0-based offsets.
/// 
/// Why: different tools/manuals sometimes write 40007 vs 400007.
/// This normalizer keeps your DB consistent and avoids off-by-base bugs.
/// </summary>
public static class ModbusAddressing
{
    /// <summary>
    /// Convert a holding-register address (4x) to a 0-based offset.
    /// Accepts either:
    /// - 40001 style (common manuals)
    /// - 400001 style (some SCADA tools show 6 digits)
    /// </summary>
    public static int HoldingRegisterToOffset(int address)
    {
        // Normalize "400007" -> "40007" if someone stored 6-digit style.
        // (i.e., anything >= 400001 is assumed to be 400001-based)
        if (address >= 400001)
            return address - 400001;

        if (address >= 40001)
            return address - 40001;

        throw new ArgumentOutOfRangeException(
            nameof(address),
            address,
            "Holding register address must be 40001+ (or 400001+).");
    }
}

