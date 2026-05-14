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
    double? SleepDeepHours,
    double? SleepLightHours,
    double? SleepRemHours,
    double? SleepAwakeHours,
    int? AvgSleepStress,
    int? BodyBatteryMin,
    int? BodyBatteryMax,
    int? BodyBatteryCharged,
    int? BodyBatteryDrained,
    int? StressAvg,
    int? AverageRespiration,
    int? HrvOvernight,
    int? HrvWeeklyAvg,
    string? HrvStatus,
    double? Weight)
{
    public bool HasAnyData =>
        Steps.HasValue || RestingHeartRate.HasValue || SleepHours.HasValue ||
        BodyBatteryMin.HasValue || StressAvg.HasValue || Weight.HasValue ||
        HrvOvernight.HasValue;
}
