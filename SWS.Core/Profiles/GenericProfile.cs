using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class GenericProfile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.Generic;

    public IReadOnlyList<PointConfig> GetDefaultPoints(int deviceId)
        => Array.Empty<PointConfig>();

    public string FormatDisplay(PointConfig point, decimal? value)
    {
        if (value is null) return "--";

        if (point.DataType == PointDataType.Bool)
            return value.Value == 1m ? "ON" : "OFF";

        if (string.IsNullOrWhiteSpace(point.Unit))
            return value.Value.ToString("0.###");

        return $"{value.Value:0.###} {point.Unit}";
    }

    public string? DecodeState(PointConfig point, decimal? value) => null;
}