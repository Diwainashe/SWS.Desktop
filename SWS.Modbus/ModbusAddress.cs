namespace SWS.Modbus
{
    /// <summary>
    /// Address conversion utilities.
    /// Your DB stores padded holding register references like 400001..400999.
    /// NModbus expects a 0-based startAddress.
    /// </summary>
    public static class ModbusAddress
    {
        private const int HoldingBasePadded = 400001;

        /// <summary>
        /// Converts padded holding register address (e.g. 400007) -> 0-based offset (6).
        /// </summary>
        public static int HoldingToOffset(int holdingAddress)
            => holdingAddress - HoldingBasePadded;
    }
}
