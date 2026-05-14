namespace GarminCoach.Models;

public sealed record DailyMetrics(
    DateOnly Date,
    int? Steps,
    int? StepGoal,
    int? RestingHeartRate,
    int? MaxHeartRate,
    int? MinHeartRate,
    double? SleepHours,
    int? SleepScore,
    int? BodyBatteryMin,
    int? BodyBatteryMax,
    int? StressAvg,
    int? AverageRespiration,
    double? Weight)
{
    public bool HasAnyData =>
        Steps.HasValue || RestingHeartRate.HasValue || SleepHours.HasValue ||
        BodyBatteryMin.HasValue || StressAvg.HasValue || Weight.HasValue;
}
