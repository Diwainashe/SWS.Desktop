namespace SWS.Core.Abstractions;

/// <summary>
/// South Africa time is UTC+2 and does not use DST.
/// This makes it safe for storing as "local time" for your deployment.
/// </summary>
public sealed class SouthAfricaTimeProvider : ITimeProvider
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(2);

    public DateTime NowLocal => DateTime.UtcNow + Offset;
}