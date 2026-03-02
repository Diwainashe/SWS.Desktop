using SWS.Core.Models;

namespace SWS.Core.Profiles;

public interface IDeviceProfile
{
    DeviceType DeviceType { get; }

    string FormatDisplay(
        string key,
        decimal? value,
        IReadOnlyList<LatestReadingSnapshot> allReadings);
}