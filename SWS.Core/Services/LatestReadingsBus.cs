using System;

namespace SWS.Desktop.Services;

/// <summary>
/// Thread-safe enough for MVP: just raises an event.
/// Subscribers must marshal to UI thread if needed.
/// </summary>
public sealed class LatestReadingsBus : ILatestReadingsBus
{
    public event EventHandler<LatestReadingsUpdatedEventArgs>? Updated;

    public void Publish(LatestReadingsUpdatedEventArgs args)
        => Updated?.Invoke(this, args);
}