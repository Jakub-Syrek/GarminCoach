using System.Net.Http;
using Garmin.Connect;
using Garmin.Connect.Auth;
using GarminCoach.Models;
using Microsoft.Extensions.Logging;

namespace GarminCoach.Services;

public sealed class GarminService : IGarminService, IDisposable
{
    private readonly SettingsStore _settings;
    private readonly ILogger<GarminService> _logger;
    private readonly HttpClient _http;
    private GarminConnectClient? _client;

    public GarminService(SettingsStore settings, ILogger<GarminService> logger)
    {
        _settings = settings;
        _logger = logger;
        _http = new HttpClient();
    }

    private GarminConnectClient GetClient()
    {
        if (_client != null) return _client;

        var s = _settings.Load();
        if (string.IsNullOrWhiteSpace(s.GarminEmail) || string.IsNullOrWhiteSpace(s.GarminPassword))
        {
            throw new InvalidOperationException("Garmin credentials are not configured.");
        }

        var auth = new BasicAuthParameters(s.GarminEmail, s.GarminPassword);
        var ctx = new GarminConnectContext(_http, auth);
        _client = new GarminConnectClient(ctx);
        return _client;
    }

    public async Task<CoachSnapshot> FetchSnapshotAsync(int days, CancellationToken ct = default)
    {
        var client = GetClient();

        string? displayName = null;
        try
        {
            var profile = await client.GetSocialProfile(ct);
            displayName = profile?.DisplayName ?? profile?.FullName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Garmin profile");
        }

        var today = DateTime.Today;
        var startDate = today.AddDays(-(days - 1));

        var daily = new List<DailyMetrics>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            daily.Add(await FetchDayAsync(client, date, ct));
        }

        var activities = await FetchActivitiesAsync(client, startDate, today, ct);

        return new CoachSnapshot(
            FetchedAt: DateTime.Now,
            UserDisplayName: displayName,
            Days: daily,
            RecentActivities: activities);
    }

    private async Task<DailyMetrics> FetchDayAsync(GarminConnectClient client, DateTime date, CancellationToken ct)
    {
        int? steps = null, stepGoal = null;
        int? rhr = null, maxHr = null, minHr = null;
        double? sleepHours = null;
        int? sleepScore = null;
        int? bbMin = null, bbMax = null;
        int? stress = null, resp = null;
        double? weight = null;

        try
        {
            var summary = await client.GetUserSummary(date, ct);
            if (summary != null)
            {
                steps = NullIfZero((int)summary.TotalSteps);
                stepGoal = NullIfZero((int)summary.DailyStepGoal);
                rhr = NullIfZero((int)summary.RestingHeartRate);
                maxHr = NullIfZero((int)summary.MaxHeartRate);
                minHr = NullIfZero((int)summary.MinHeartRate);
                stress = summary.AverageStressLevel > 0 ? (int)summary.AverageStressLevel : null;
                resp = summary.AvgWakingRespirationValue > 0 ? (int)Math.Round(summary.AvgWakingRespirationValue) : null;
                bbMin = NullIfZero((int)summary.BodyBatteryLowestValue);
                bbMax = NullIfZero((int)summary.BodyBatteryHighestValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UserSummary failed for {Date}", date);
        }

        try
        {
            var sleep = await client.GetWellnessSleepData(date, ct);
            var dto = sleep?.DailySleepDto;
            if (dto != null && dto.SleepTimeSeconds > 0)
            {
                sleepHours = dto.SleepTimeSeconds / 3600.0;
            }
            var overall = dto?.SleepScores?.Overall?.Value ?? 0;
            sleepScore = overall > 0 ? (int)overall : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Sleep failed for {Date}", date);
        }

        try
        {
            var bc = await client.GetBodyComposition(date, date, ct);
            var first = bc?.DateWeightList?.FirstOrDefault();
            if (first != null && first.Weight > 0)
            {
                weight = first.Weight / 1000.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "BodyComposition failed for {Date}", date);
        }

        return new DailyMetrics(
            Date: DateOnly.FromDateTime(date),
            Steps: steps,
            StepGoal: stepGoal,
            RestingHeartRate: rhr,
            MaxHeartRate: maxHr,
            MinHeartRate: minHr,
            SleepHours: sleepHours,
            SleepScore: sleepScore,
            BodyBatteryMin: bbMin,
            BodyBatteryMax: bbMax,
            StressAvg: stress,
            AverageRespiration: resp,
            Weight: weight);
    }

    private async Task<IReadOnlyList<ActivitySummary>> FetchActivitiesAsync(
        GarminConnectClient client, DateTime from, DateTime to, CancellationToken ct)
    {
        try
        {
            var acts = await client.GetActivitiesByDate(from, to, string.Empty, ct);
            if (acts == null) return Array.Empty<ActivitySummary>();

            return acts
                .OrderByDescending(a => a.StartTimeLocal)
                .Select(a => new ActivitySummary(
                    Id: a.ActivityId,
                    Name: a.ActivityName ?? "(no name)",
                    ActivityType: a.ActivityType?.TypeKey ?? "unknown",
                    StartTime: a.StartTimeLocal,
                    Duration: TimeSpan.FromSeconds(a.Duration),
                    DistanceMeters: a.Distance > 0 ? a.Distance : null,
                    AverageHr: a.AverageHr > 0 ? a.AverageHr : null,
                    MaxHr: a.MaxHr > 0 ? a.MaxHr : null,
                    Calories: a.Calories > 0 ? a.Calories : null,
                    AverageSpeedMps: a.AverageSpeed > 0 ? a.AverageSpeed : null,
                    ElevationGainMeters: a.ElevationGain > 0 ? a.ElevationGain : null,
                    TrainingLoad: a.TrainingStressScore > 0 ? a.TrainingStressScore : null))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch activities");
            return Array.Empty<ActivitySummary>();
        }
    }

    private static int? NullIfZero(int v) => v == 0 ? null : v;

    public void Dispose() => _http.Dispose();
}
