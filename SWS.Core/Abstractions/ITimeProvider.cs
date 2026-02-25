namespace SWS.Core.Abstractions;

/// <summary>
/// Centralized time source so the whole app agrees on what "now" means.
/// In MVP we store South Africa local time (UTC+2) as "local now".
/// </summary>
public interface ITimeProvider
{
    DateTime NowLocal { get; }
}