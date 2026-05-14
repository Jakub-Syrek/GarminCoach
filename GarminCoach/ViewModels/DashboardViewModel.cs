using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminCoach.Models;
using GarminCoach.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace GarminCoach.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IGarminService _garmin;
    private readonly IClaudeService _claude;
    private readonly SettingsStore _settings;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Gotowy.";
    [ObservableProperty] private string _reportText = string.Empty;
    [ObservableProperty] private CoachSnapshot _snapshot = CoachSnapshot.Empty();

    [ObservableProperty] private string _stepsToday = "—";
    [ObservableProperty] private string _restingHr = "—";
    [ObservableProperty] private string _sleepLast = "—";
    [ObservableProperty] private string _bodyBattery = "—";
    [ObservableProperty] private string _hrvLast = "—";
    [ObservableProperty] private string _stressLast = "—";

    public ObservableCollection<ISeries> HrSeries { get; } = new();
    public ObservableCollection<ISeries> SleepStagesSeries { get; } = new();
    public ObservableCollection<ISeries> HrvSeries { get; } = new();
    public ObservableCollection<ISeries> BodyBatterySeries { get; } = new();
    public ObservableCollection<ISeries> ActivityVolumeSeries { get; } = new();
    public ObservableCollection<ISeries> StressSeries { get; } = new();

    public ObservableCollection<Axis> XAxes { get; } = new();
    public ObservableCollection<Axis> YAxesHr { get; } = new();
    public ObservableCollection<Axis> YAxesSleep { get; } = new();
    public ObservableCollection<Axis> YAxesHrv { get; } = new();
    public ObservableCollection<Axis> YAxesBB { get; } = new();
    public ObservableCollection<Axis> YAxesVolume { get; } = new();
    public ObservableCollection<Axis> YAxesStress { get; } = new();

    public ObservableCollection<ActivitySummary> Activities { get; } = new();
    public ObservableCollection<ActivityCategoryAggregate> CategoryAggregates { get; } = new();

    public event EventHandler<CoachSnapshot>? SnapshotChanged;

    public DashboardViewModel(
        IGarminService garmin,
        IClaudeService claude,
        SettingsStore settings,
        ILogger<DashboardViewModel> logger)
    {
        _garmin = garmin;
        _claude = claude;
        _settings = settings;
        _logger = logger;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsBusy) return;
        var secrets = _settings.Load();
        if (!_settings.IsConfigured(secrets))
        {
            StatusText = "Brak konfiguracji. Otwórz Ustawienia.";
            return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Pobieram dane z Garmin Connect…";
            var days = secrets.DaysToFetch <= 0 ? 7 : secrets.DaysToFetch;
            var snap = await _garmin.FetchSnapshotAsync(days);
            Snapshot = snap;
            UpdateChartsAndCards(snap);
            Activities.Clear();
            foreach (var a in snap.RecentActivities.Take(30)) Activities.Add(a);
            CategoryAggregates.Clear();
            foreach (var c in snap.CategoryAggregates) CategoryAggregates.Add(c);
            StatusText = $"Pobrano {snap.Days.Count} dni, {snap.RecentActivities.Count} aktywności w {snap.CategoryAggregates.Count} kategoriach.";
            SnapshotChanged?.Invoke(this, snap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Garmin fetch failed");
            StatusText = "Błąd: " + ex.Message;
            MessageBox.Show(ex.ToString(), "Garmin error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        if (IsBusy) return;
        if (Snapshot.IsEmpty)
        {
            await RefreshAsync();
            if (Snapshot.IsEmpty) return;
        }

        try
        {
            IsBusy = true;
            StatusText = "Claude analizuje dane…";
            var report = await _claude.AskAsync(
                Snapshot,
                Array.Empty<ChatMessage>(),
                CoachContextBuilder.DefaultReportPrompt());
            ReportText = report;
            StatusText = "Raport gotowy.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude report failed");
            ReportText = "Błąd: " + ex.Message;
            StatusText = "Błąd raportu.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateChartsAndCards(CoachSnapshot snap)
    {
        var ordered = snap.Days.OrderBy(d => d.Date).ToList();
        var latest = ordered.LastOrDefault();

        StepsToday = latest?.Steps.HasValue == true
            ? latest.Steps!.Value.ToString("N0")
            : "—";
        RestingHr = latest?.RestingHeartRate.HasValue == true
            ? $"{latest.RestingHeartRate} bpm"
            : "—";
        SleepLast = latest?.SleepHours.HasValue == true
            ? $"{latest.SleepHours:F1} h"
            : "—";
        BodyBattery = (latest?.BodyBatteryMin.HasValue == true && latest?.BodyBatteryMax.HasValue == true)
            ? $"{latest.BodyBatteryMin}–{latest.BodyBatteryMax}"
            : "—";
        HrvLast = latest?.HrvOvernight.HasValue == true
            ? $"{latest.HrvOvernight} ({latest.HrvStatus ?? "—"})"
            : "—";
        StressLast = latest?.StressAvg.HasValue == true
            ? latest.StressAvg!.Value.ToString()
            : "—";

        var labels = ordered.Select(d => d.Date.ToString("MM-dd")).ToArray();

        // Resting HR line
        HrSeries.Clear();
        HrSeries.Add(new LineSeries<double?>
        {
            Name = "Resting HR",
            Values = ordered.Select(d => (double?)d.RestingHeartRate).ToArray(),
            Stroke = new SolidColorPaint(SKColor.Parse("E57373")) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(SKColor.Parse("E57373")),
            GeometryFill = new SolidColorPaint(SKColors.White),
            Fill = null
        });

        // Sleep stages stacked
        SleepStagesSeries.Clear();
        SleepStagesSeries.Add(MakeStackedColumn("Głęboki", ordered.Select(d => d.SleepDeepHours).ToArray(), "1A237E"));
        SleepStagesSeries.Add(MakeStackedColumn("Lekki",   ordered.Select(d => d.SleepLightHours).ToArray(), "5C6BC0"));
        SleepStagesSeries.Add(MakeStackedColumn("REM",     ordered.Select(d => d.SleepRemHours).ToArray(),   "9575CD"));
        SleepStagesSeries.Add(MakeStackedColumn("Awake",   ordered.Select(d => d.SleepAwakeHours).ToArray(), "EF9A9A"));

        // HRV line
        HrvSeries.Clear();
        HrvSeries.Add(new LineSeries<double?>
        {
            Name = "HRV (ms)",
            Values = ordered.Select(d => (double?)d.HrvOvernight).ToArray(),
            Stroke = new SolidColorPaint(SKColor.Parse("66BB6A")) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(SKColor.Parse("66BB6A")),
            GeometryFill = new SolidColorPaint(SKColors.White),
            Fill = null
        });

        // Body battery (charged vs drained as paired columns)
        BodyBatterySeries.Clear();
        BodyBatterySeries.Add(new ColumnSeries<double?>
        {
            Name = "Naładowane",
            Values = ordered.Select(d => (double?)d.BodyBatteryCharged).ToArray(),
            Fill = new SolidColorPaint(SKColor.Parse("81C784"))
        });
        BodyBatterySeries.Add(new ColumnSeries<double?>
        {
            Name = "Zużyte",
            Values = ordered.Select(d => d.BodyBatteryDrained.HasValue ? (double?)-d.BodyBatteryDrained.Value : null).ToArray(),
            Fill = new SolidColorPaint(SKColor.Parse("E57373"))
        });

        // Activity volume per day by category (stacked column)
        ActivityVolumeSeries.Clear();
        var byDayCategory = BuildVolumeMatrix(snap.RecentActivities, ordered.Select(d => d.Date).ToArray());
        foreach (var (cat, values, color) in byDayCategory)
        {
            ActivityVolumeSeries.Add(new StackedColumnSeries<double?>
            {
                Name = cat.ToPolish(),
                Values = values,
                Fill = new SolidColorPaint(SKColor.Parse(color)),
                Stroke = null
            });
        }

        // Stress line
        StressSeries.Clear();
        StressSeries.Add(new LineSeries<double?>
        {
            Name = "Stres",
            Values = ordered.Select(d => (double?)d.StressAvg).ToArray(),
            Stroke = new SolidColorPaint(SKColor.Parse("FFA726")) { StrokeThickness = 2.5f },
            GeometryStroke = new SolidColorPaint(SKColor.Parse("FFA726")),
            GeometryFill = new SolidColorPaint(SKColors.White),
            Fill = null
        });

        XAxes.Clear();
        XAxes.Add(new Axis { Labels = labels, TextSize = 11 });

        YAxesHr.Clear();
        YAxesHr.Add(new Axis { MinLimit = 40, MaxLimit = 80, TextSize = 11 });

        YAxesSleep.Clear();
        YAxesSleep.Add(new Axis { MinLimit = 0, MaxLimit = 10, TextSize = 11 });

        YAxesHrv.Clear();
        YAxesHrv.Add(new Axis { MinLimit = 20, MaxLimit = 90, TextSize = 11 });

        YAxesBB.Clear();
        YAxesBB.Add(new Axis { MinLimit = -100, MaxLimit = 100, TextSize = 11 });

        YAxesVolume.Clear();
        YAxesVolume.Add(new Axis { MinLimit = 0, TextSize = 11, Name = "min" });

        YAxesStress.Clear();
        YAxesStress.Add(new Axis { MinLimit = 0, MaxLimit = 100, TextSize = 11 });
    }

    private static StackedColumnSeries<double?> MakeStackedColumn(
        string name, double?[] values, string hexColor)
        => new()
        {
            Name = name,
            Values = values,
            Fill = new SolidColorPaint(SKColor.Parse(hexColor)),
            Stroke = null
        };

    private static List<(ActivityCategory Category, double?[] Values, string Color)> BuildVolumeMatrix(
        IReadOnlyList<ActivitySummary> activities,
        DateOnly[] days)
    {
        var palette = new Dictionary<ActivityCategory, string>
        {
            { ActivityCategory.Running,  "F44336" },
            { ActivityCategory.Cycling,  "2196F3" },
            { ActivityCategory.Gym,      "FF9800" },
            { ActivityCategory.Climbing, "9C27B0" },
            { ActivityCategory.Mountain, "4CAF50" },
            { ActivityCategory.Swimming, "00BCD4" },
            { ActivityCategory.Walking,  "9E9E9E" },
            { ActivityCategory.Other,    "607D8B" }
        };

        var byCategory = activities
            .GroupBy(a => ActivityCategoryExtensions.Classify(a.ActivityType))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<(ActivityCategory, double?[], string)>();
        foreach (var cat in palette.Keys)
        {
            if (!byCategory.TryGetValue(cat, out var list)) continue;
            var arr = days.Select(d =>
            {
                var minutes = list
                    .Where(a => DateOnly.FromDateTime(a.StartTime) == d)
                    .Sum(a => a.Duration.TotalMinutes);
                return minutes > 0 ? (double?)minutes : null;
            }).ToArray();
            result.Add((cat, arr, palette[cat]));
        }
        return result;
    }
}
