using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class Gm9907L5Profile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.GM9907_L5;

    public IReadOnlyList<PointConfig> GetDefaultPoints(int deviceId)
    {
        // Minimal “tile starter pack”
        // Replace addresses/length/datatype with the confirmed manual values
        return new List<PointConfig>
        {
            new PointConfig
            {
                DeviceConfigId = deviceId,
                Key = "Weight.Display",
                Label = "Weight",
                Unit = "kg",
                Area = ModbusPointArea.HoldingRegister,
                Address = 400007,
                Length = 2,
                DataType = PointDataType.Float32,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true,
                LogToHistory = true,
                HistoryIntervalMs = 1000
            },
            new PointConfig
            {
                DeviceConfigId = deviceId,
                Key = "Flowrate.Actual",
                Label = "Flowrate",
                Unit = "t/h",
                Area = ModbusPointArea.HoldingRegister,
                Address = 400010,
                Length = 2,
                DataType = PointDataType.Float32,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true
            },
            new PointConfig
            {
                DeviceConfigId = deviceId,
                Key = "Status.Word",
                Label = "Status",
                Unit = "",
                Area = ModbusPointArea.HoldingRegister,
                Address = 400020,
                Length = 1,
                DataType = PointDataType.UInt16,
                Scale = 1m,
                PollRateMs = 500,
                IsEssential = true
            }
        };
    }

    public string FormatDisplay(PointConfig point, decimal? value)
    {
        if (value is null) return "--";

        if (point.DataType == PointDataType.Bool)
            return value.Value == 1m ? "ON" : "OFF";

        // GM9907 nice formatting – keep clean, tile-friendly
        if (point.Key.StartsWith("Flowrate"))
            return $"{value.Value:0.###} {point.Unit}";

        if (point.Key.StartsWith("Weight"))
            return $"{value.Value:0.##} {point.Unit}";

        if (string.IsNullOrWhiteSpace(point.Unit))
            return value.Value.ToString("0.###");

        return $"{value.Value:0.###} {point.Unit}";
    }

    public string? DecodeState(PointConfig point, decimal? value)
    {
        if (value is null) return null;

        // Example placeholder: later we decode the manual’s “Description -> decode table”
        if (point.Key == "Status.Word")
        {
            var v = (ushort)value.Value;
            // e.g. bit 0 = Running, bit 1 = Alarm... (we’ll fill with confirmed manual mapping)
            bool running = (v & 0b1) != 0;
            return running ? "Running" : "Stopped";
        }

        return null;
    }
}