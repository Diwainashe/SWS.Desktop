using SWS.Core.Models;

namespace SWS.Core.Profiles;

public sealed class DeviceProfileRegistry
{
    private readonly Dictionary<DeviceType, IDeviceProfile> _map;

    public DeviceProfileRegistry(IEnumerable<IDeviceProfile> profiles)
    {
        _map = profiles.ToDictionary(p => p.DeviceType, p => p);
    }

    public IDeviceProfile Get(DeviceType type)
    {
        if (_map.TryGetValue(type, out var profile))
            return profile;

        // fallback is required so Generic devices still work
        return _map[DeviceType.Generic];
    }
}