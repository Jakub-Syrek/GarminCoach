namespace GarminCoach.Models;

public sealed record CoachSnapshot(
    DateTime FetchedAt,
    string? UserDisplayName,
    IReadOnlyList<DailyMetrics> Days,
    IReadOnlyList<ActivitySummary> RecentActivities)
{
    public static CoachSnapshot Empty(string? user = null) =>
        new(DateTime.MinValue, user, Array.Empty<DailyMetrics>(), Array.Empty<ActivitySummary>());

    public bool IsEmpty => Days.Count == 0 && RecentActivities.Count == 0;
}
