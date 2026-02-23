namespace SWS.Modbus;

/// <summary>
/// Central place for converting "manual style" Modbus addresses into 0-based offsets.
/// Supports both 5-digit (40001) and 6-digit (400001) manual notations.
/// </summary>
public static class ModbusAddressing
{
    // Common manual bases used by different vendors/manuals
    private const int HrBase5 = 40001;
    private const int HrBase6 = 400001;

    private const int IrBase5 = 30001;
    private const int IrBase6 = 300001;

    private const int DiBase5 = 10001;
    private const int DiBase6 = 100001;

    /// <summary>
    /// Holding Registers (4x). Supports 40001.. or 400001..
    /// </summary>
    public static ushort HoldingToOffset(int logical)
        => ToOffsetSmart(logical, HrBase5, HrBase6);

    /// <summary>
    /// Input Registers (3x). Supports 30001.. or 300001..
    /// </summary>
    public static ushort InputToOffset(int logical)
        => ToOffsetSmart(logical, IrBase5, IrBase6);

    /// <summary>
    /// Coils (0x). Many manuals show 00001 or 1-based addressing.
    /// We treat:
    /// - 0 as already 0-based
    /// - >= 1 as 1-based manual address (subtract 1)
    /// </summary>
    public static ushort CoilToOffset(int logical)
        => ToOffsetOneBased(logical);

    /// <summary>
    /// Discrete Inputs (1x). Some manuals show 10001.. or 100001..; others show 1-based.
    /// This supports both 10001/100001 and plain 1-based.
    /// </summary>
    public static ushort DiscreteToOffset(int logical)
    {
        // If it looks like 10001/100001 style, treat it as register-style base mapping.
        if (logical >= DiBase5)
            return ToOffsetSmart(logical, DiBase5, DiBase6);

        // Otherwise treat as 1-based bit address.
        return ToOffsetOneBased(logical);
    }

    /// <summary>
    /// Converts logical address to offset, automatically choosing between 5-digit and 6-digit bases.
    /// Also allows already-0-based offsets for testing (0..65535).
    /// </summary>
    private static ushort ToOffsetSmart(int logical, int base5, int base6)
    {
        // Allow already 0-based offsets for testing
        if (logical >= 0 && logical <= ushort.MaxValue && logical < base5)
            return (ushort)logical;

        // Prefer matching the larger base if it fits that pattern
        if (logical >= base6)
            return ToOffsetFromBase(logical, base6);

        if (logical >= base5)
            return ToOffsetFromBase(logical, base5);

        throw new ArgumentOutOfRangeException(
            nameof(logical),
            $"Logical address must be >= {base5} (5-digit manual) or >= {base6} (6-digit manual), or be a valid 0-based offset (0..65535).");
    }

    private static ushort ToOffsetFromBase(int logical, int manualBase)
    {
        int zeroBased = logical - manualBase;

        if (zeroBased < 0 || zeroBased > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(logical), $"Address {logical} produced invalid offset {zeroBased} for base {manualBase}.");

        return (ushort)zeroBased;
    }

    private static ushort ToOffsetOneBased(int logical)
    {
        // 0-based allowed
        if (logical == 0)
            return 0;

        if (logical > 0 && logical <= ushort.MaxValue + 1)
            return (ushort)(logical - 1);

        throw new ArgumentOutOfRangeException(nameof(logical));
    }
}