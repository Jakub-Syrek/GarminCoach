using GarminCoach.Models;
using GarminCoach.Services;

namespace GarminCoach.Tests;

public sealed class ActivityAggregatorTests
{
    [Fact]
    public void AggregateByCategory_GroupsAndSumsCorrectly()
    {
        var activities = new[]
        {
            Make("running", 30, 5000, 150, 350, 50),
            Make("trail_running", 60, 10000, 145, 700, 250),
            Make("indoor_climbing", 90, 0, 110, 400, 0),
            Make("rock_climbing", 120, 0, 130, 600, 100),
            Make("strength_training", 45, 0, 105, 250, 0),
        };

        var aggs = ActivityAggregator.AggregateByCategory(activities);

        Assert.Equal(3, aggs.Count);

        var run = aggs.Single(a => a.Category == ActivityCategory.Running);
        Assert.Equal(2, run.SessionCount);
        Assert.Equal(90, run.TotalDuration.TotalMinutes);
        Assert.Equal(15000, run.TotalDistanceMeters);
        Assert.Equal(1050, run.TotalCalories);

        var climb = aggs.Single(a => a.Category == ActivityCategory.Climbing);
        Assert.Equal(2, climb.SessionCount);
        Assert.Equal(210, climb.TotalDuration.TotalMinutes);

        var gym = aggs.Single(a => a.Category == ActivityCategory.Gym);
        Assert.Equal(1, gym.SessionCount);
        Assert.Equal(45, gym.TotalDuration.TotalMinutes);
    }

    [Fact]
    public void AggregateByCategory_AvgHr_IsDurationWeighted()
    {
        // 30 min @ 140 + 90 min @ 160 → expected 155
        var activities = new[]
        {
            Make("running", 30, 5000, 140, 300, 0),
            Make("running", 90, 15000, 160, 900, 0),
        };

        var aggs = ActivityAggregator.AggregateByCategory(activities);
        var run = Assert.Single(aggs);

        Assert.NotNull(run.AverageHr);
        Assert.Equal(155.0, run.AverageHr!.Value, precision: 1);
    }

    [Fact]
    public void AggregateByCategory_OrdersByDurationDescending()
    {
        var activities = new[]
        {
            Make("running", 30, 5000, 150, 300, 0),
            Make("strength_training", 60, 0, 110, 300, 0),
            Make("indoor_climbing", 120, 0, 120, 500, 0),
        };

        var aggs = ActivityAggregator.AggregateByCategory(activities);

        Assert.Equal(ActivityCategory.Climbing, aggs[0].Category);
        Assert.Equal(ActivityCategory.Gym, aggs[1].Category);
        Assert.Equal(ActivityCategory.Running, aggs[2].Category);
    }

    [Fact]
    public void AggregateByCategory_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(ActivityAggregator.AggregateByCategory(Array.Empty<ActivitySummary>()));
    }

    private static ActivitySummary Make(
        string type, int minutes, double meters, double avgHr, double kcal, double elevation)
        => new(
            Id: Random.Shared.NextInt64(),
            Name: type,
            ActivityType: type,
            StartTime: DateTime.Now.AddMinutes(-Random.Shared.Next(1, 10000)),
            Duration: TimeSpan.FromMinutes(minutes),
            DistanceMeters: meters > 0 ? meters : null,
            AverageHr: avgHr,
            MaxHr: avgHr + 20,
            Calories: kcal,
            AverageSpeedMps: meters > 0 ? meters / (minutes * 60.0) : null,
            ElevationGainMeters: elevation > 0 ? elevation : null,
            TrainingLoad: null);
}
