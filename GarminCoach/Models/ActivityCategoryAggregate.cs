namespace GarminCoach.Models;

public sealed record ActivityCategoryAggregate(
    ActivityCategory Category,
    int SessionCount,
    TimeSpan TotalDuration,
    double TotalDistanceMeters,
    double TotalElevationGainMeters,
    double TotalCalories,
    double? AverageHr,
    double? TotalTrainingLoad);
