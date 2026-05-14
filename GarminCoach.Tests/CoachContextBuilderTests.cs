using GarminCoach.Models;
using GarminCoach.Services;

namespace GarminCoach.Tests;

public sealed class CoachContextBuilderTests
{
    [Fact]
    public void SystemPrompt_IncludesAllProvidedDays_InDescendingOrder()
    {
        var snap = new CoachSnapshot(
            FetchedAt: new DateTime(2026, 5, 14, 9, 0, 0),
            UserDisplayName: "Jakub",
            Days: new[]
            {
                MakeDay(new DateOnly(2026, 5, 12), steps: 8000),
                MakeDay(new DateOnly(2026, 5, 13), steps: 11000),
                MakeDay(new DateOnly(2026, 5, 14), steps: 4500)
            },
            RecentActivities: Array.Empty<ActivitySummary>());

        var prompt = CoachContextBuilder.SystemPrompt(snap);

        var i14 = prompt.IndexOf("2026-05-14", StringComparison.Ordinal);
        var i13 = prompt.IndexOf("2026-05-13", StringComparison.Ordinal);
        var i12 = prompt.IndexOf("2026-05-12", StringComparison.Ordinal);

        Assert.True(i14 >= 0 && i13 > i14 && i12 > i13,
            $"Days should be ordered newest-first; got 14@{i14}, 13@{i13}, 12@{i12}");
    }

    [Fact]
    public void SystemPrompt_RendersDashForMissingFields()
    {
        var snap = new CoachSnapshot(
            FetchedAt: DateTime.Now,
            UserDisplayName: null,
            Days: new[] { MakeDay(new DateOnly(2026, 5, 14), steps: null) },
            RecentActivities: Array.Empty<ActivitySummary>());

        var prompt = CoachContextBuilder.SystemPrompt(snap);

        Assert.Contains("—", prompt);
        Assert.DoesNotContain("Klient:", prompt);
    }

    [Fact]
    public void SystemPrompt_IncludesActivityBlock_WhenActivitiesPresent()
    {
        var activity = new ActivitySummary(
            Id: 42,
            Name: "Morning run",
            ActivityType: "running",
            StartTime: new DateTime(2026, 5, 14, 7, 30, 0),
            Duration: TimeSpan.FromMinutes(45),
            DistanceMeters: 8500,
            AverageHr: 152,
            MaxHr: 178,
            Calories: 540,
            AverageSpeedMps: 3.15,
            ElevationGainMeters: 42,
            TrainingLoad: 95);

        var snap = new CoachSnapshot(
            FetchedAt: DateTime.Now,
            UserDisplayName: "Jakub",
            Days: new[] { MakeDay(new DateOnly(2026, 5, 14)) },
            RecentActivities: new[] { activity });

        var prompt = CoachContextBuilder.SystemPrompt(snap);

        Assert.Contains("Morning run", prompt);
        Assert.Contains("running", prompt);
        Assert.Contains("8.50 km", prompt);
        Assert.Contains("avgHR 152", prompt);
    }

    [Fact]
    public void SystemPrompt_FormatsPace_AsMinSecPerKm()
    {
        // 3.0 m/s = 5:33/km
        var activity = new ActivitySummary(
            Id: 1, Name: "test", ActivityType: "running",
            StartTime: DateTime.Now, Duration: TimeSpan.FromMinutes(30),
            DistanceMeters: 5000, AverageHr: 140, MaxHr: 160,
            Calories: 300, AverageSpeedMps: 3.0, ElevationGainMeters: 0,
            TrainingLoad: null);

        var snap = new CoachSnapshot(DateTime.Now, null,
            new[] { MakeDay(DateOnly.FromDateTime(DateTime.Today)) },
            new[] { activity });

        var prompt = CoachContextBuilder.SystemPrompt(snap);

        Assert.Contains("5:33/km", prompt);
    }

    [Fact]
    public void DefaultReportPrompt_IsNonEmpty()
    {
        Assert.False(string.IsNullOrWhiteSpace(CoachContextBuilder.DefaultReportPrompt()));
    }

    private static DailyMetrics MakeDay(
        DateOnly date,
        int? steps = 10000,
        int? rhr = 55,
        double? sleep = 7.5)
        => new(
            Date: date,
            Steps: steps,
            StepGoal: 10000,
            RestingHeartRate: rhr,
            MaxHeartRate: null,
            MinHeartRate: null,
            SleepHours: sleep,
            SleepScore: null,
            BodyBatteryMin: null,
            BodyBatteryMax: null,
            StressAvg: null,
            AverageRespiration: null,
            Weight: null);
}
