namespace GarminCoach.Models;

public sealed record ActivitySummary(
    long Id,
    string Name,
    string ActivityType,
    DateTime StartTime,
    TimeSpan Duration,
    double? DistanceMeters,
    double? AverageHr,
    double? MaxHr,
    double? Calories,
    double? AverageSpeedMps,
    double? ElevationGainMeters,
    double? TrainingLoad);
