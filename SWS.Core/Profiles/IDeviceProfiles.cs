using SWS.Core.Models;

namespace SWS.Core.Profiles;

/// <summary>
/// Device-specific behavior and templates.
/// Poller remains generic.
/// Profiles define decoding helpers and default points.
/// </summary>
public interface IDeviceProfile
{
    DeviceType DeviceType { get; }

    /// <summary>
    /// Returns default register template for this device.
    /// </summary>
    IReadOnlyList<PointConfig> GetDefaultPoints(int deviceId);

    /// <summary>
    /// Formats display value for tiles and UI.
    /// </summary>
    string FormatDisplay(PointConfig point, decimal? value);

    /// <summary>
    /// Optional status decoding (bitfields, codes, etc).
    /// </summary>
    string? DecodeState(PointConfig point, decimal? value);
}