using SWS.Core.Models;

namespace SWS.Core.Abstractions;

/// <summary>
/// Converts raw Modbus registers to a meaningful numeric value.
/// Word order handling will be added once we confirm the device's config.
/// </summary>
public interface IDecoder
{
    decimal? DecodeNumeric(PointConfig point, ushort[] registers);
}
