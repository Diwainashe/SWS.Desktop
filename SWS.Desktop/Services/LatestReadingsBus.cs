using SWS.Core.Services;

namespace SWS.Desktop.Services;

/// <summary>
/// Desktop implementation that raises an event the UI can subscribe to.
/// </summary>
public sealed class LatestReadingsBus : ILatestReadingsBus
{
    public event EventHandler<LatestReadingsUpdatedEventArgs>? LatestUpdated;

    public void Publish(int deviceId, DateTime timestampLocal)
    {
        LatestUpdated?.Invoke(this, new LatestReadingsUpdatedEventArgs(deviceId, timestampLocal));
    }
}

/// <summary>
/// Payload for UI refresh.
/// </summary>
public sealed class LatestReadingsUpdatedEventArgs : EventArgs
{
    public int DeviceId { get; }
    public DateTime TimestampLocal { get; }

    public LatestReadingsUpdatedEventArgs(int deviceId, DateTime timestampLocal)
    {
        DeviceId = deviceId;
        TimestampLocal = timestampLocal;
    }
}