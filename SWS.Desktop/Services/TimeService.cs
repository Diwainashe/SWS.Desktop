using System;

namespace SWS.Desktop.Services;

public static class TimeService
{
    private static readonly TimeZoneInfo ZaTz =
        TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

    public static DateTime ToSouthAfricaTime(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), ZaTz);
}