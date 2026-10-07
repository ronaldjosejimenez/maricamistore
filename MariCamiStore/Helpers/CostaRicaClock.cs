namespace MariCamiStore.Helpers;

/// <summary>Single place for Costa Rica local time.</summary>
public static class CostaRicaClock
{
    private const string TimeZoneId = "Central America Standard Time";

    public static DateTime Now() =>
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, TimeZoneId);

    public static DateOnly Today() => DateOnly.FromDateTime(Now());
}
