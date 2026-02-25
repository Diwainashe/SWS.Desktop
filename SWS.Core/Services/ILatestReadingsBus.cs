using System;

namespace SWS.Desktop.Services;

/// <summary>
/// Simple in-process event bus: poller publishes latest changes,
/// UI subscribes and updates immediately.
/// </summary>
public interface ILatestReadingsBus
{
    event EventHandler<LatestReadingsUpdatedEventArgs>? Updated;

    void Publish(LatestReadingsUpdatedEventArgs args);
}

/// <summary>
/// Payload sent from the poller to the UI.
/// Keep it small and UI-friendly.
/// </summary>
public sealed class LatestReadingsUpdatedEventArgs : EventArgs
{
    public LatestReadingsUpdatedEventArgs(int deviceId, DateTime timestampUtc)
    {
        DeviceId = deviceId;
        TimestampUtc = timestampUtc;
    }

    public int DeviceId { get; }
    public DateTime TimestampUtc { get; }
}