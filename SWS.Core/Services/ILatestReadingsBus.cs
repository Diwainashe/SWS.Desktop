namespace SWS.Core.Services;

/// <summary>
/// Cross-layer signal: Acquisition publishes “latest readings changed”.
/// UI can listen and refresh.
/// Keep interface in Core so Acquisition can depend on it.
/// </summary>
public interface ILatestReadingsBus
{
    void Publish(int deviceId, DateTime timestampLocal);
}