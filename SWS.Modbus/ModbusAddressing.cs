namespace SWS.Modbus;

/// <summary>
/// Converts manual-style Modbus addresses to 0-based offsets used by libraries.
/// Supports both 40,000-style (e.g. 40007) and 400,000-style (e.g. 400007).
/// </summary>
public static class ModbusAddressing
{
    public static int ToZeroBasedOffset(int manualAddress)
    {
        // Common manual formats:
        // - 40001..49999 (older style)
        // - 400001..499999 (6-digit style)
        // We detect which base to subtract.
        if (manualAddress >= 400001)
            return manualAddress - 400001;

        if (manualAddress >= 40001)
            return manualAddress - 40001;

        // If someone stored 1-based raw register index (1..),
        // treat it as 1-based and convert to 0-based.
        if (manualAddress >= 1)
            return manualAddress - 1;

        return -1;
    }
}