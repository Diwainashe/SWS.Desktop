using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class GenericProfile : IDeviceProfile
{
    public DeviceType DeviceType => DeviceType.Generic;

    public string FormatDisplay(string key, decimal? value, IReadOnlyList<LatestReadingSnapshot> allReadings)
    {
        if (value is null)
            return "—";

        // Determine datatype of THIS key from the device context
        var dt = allReadings.FirstOrDefault(x => x.Key == key)?.DataType ?? PointDataType.UInt16;

        // Only render ON/OFF if this point is Bool
        if (dt == PointDataType.Bool)
            return value.Value == 0m ? "OFF" : "ON";

        // Otherwise keep numeric
        return value.Value.ToString("0.###");
    }
}