using GarminCoach.Models;

namespace GarminCoach.Services;

public static class ActivityAggregator
{
    public static IReadOnlyList<ActivityCategoryAggregate> AggregateByCategory(
        IReadOnlyList<ActivitySummary> activities)
    {
        return activities
            .GroupBy(a => ActivityCategoryExtensions.Classify(a.ActivityType))
            .Select(g =>
            {
                var hrSamples = g.Where(a => a.AverageHr.HasValue).ToList();
                double? avgHr = null;
                if (hrSamples.Count > 0)
                {
                    var totalSec = hrSamples.Sum(a => a.Duration.TotalSeconds);
                    if (totalSec > 0)
                    {
                        avgHr = hrSamples.Sum(a => a.AverageHr!.Value * a.Duration.TotalSeconds) / totalSec;
                    }
                }

                var loadSamples = g.Where(a => a.TrainingLoad.HasValue).ToList();
                double? totalLoad = loadSamples.Count > 0
                    ? loadSamples.Sum(a => a.TrainingLoad!.Value)
                    : null;

                return new ActivityCategoryAggregate(
                    Category: g.Key,
                    SessionCount: g.Count(),
                    TotalDuration: TimeSpan.FromSeconds(g.Sum(a => a.Duration.TotalSeconds)),
                    TotalDistanceMeters: g.Sum(a => a.DistanceMeters ?? 0),
                    TotalElevationGainMeters: g.Sum(a => a.ElevationGainMeters ?? 0),
                    TotalCalories: g.Sum(a => a.Calories ?? 0),
                    AverageHr: avgHr,
                    TotalTrainingLoad: totalLoad);
            })
            .OrderByDescending(a => a.TotalDuration)
            .ToList();
    }
}
