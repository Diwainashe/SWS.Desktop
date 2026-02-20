namespace SWS.Core.Models
{
    public sealed class PointTemplate
    {
        public int Id { get; set; }

        public string Key { get; set; } = string.Empty;  // e.g., "Weight.Display", "Flow.TPH"
        public string DisplayName { get; set; } = string.Empty;  // e.g., "Weight (kg)", "Flow Rate (t/h)"

        public ModbusPointArea Area { get; set; } = ModbusPointArea.HoldingRegister;  // Holding, Input, Coil
        public int Address { get; set; }  // e.g., 400007
        public PointDataType DataType { get; set; } = PointDataType.UInt16;  // Default data type
        public decimal Scale { get; set; } = 1m;  // Default scale factor (e.g., 1m, 0.01)
        public bool IsEssential { get; set; } = true;  // Marks if the point is essential for display
    }
}