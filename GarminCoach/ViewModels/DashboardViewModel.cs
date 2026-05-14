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

    public ObservableCollection<ISeries> HrSeries { get; } = new();
    public ObservableCollection<ISeries> SleepSeries { get; } = new();
    public ObservableCollection<Axis> XAxes { get; } = new();
    public ObservableCollection<Axis> YAxesHr { get; } = new();
    public ObservableCollection<Axis> YAxesSleep { get; } = new();
    public ObservableCollection<ActivitySummary> Activities { get; } = new();

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
            foreach (var a in snap.RecentActivities.Take(20)) Activities.Add(a);
            StatusText = $"Pobrano {snap.Days.Count} dni, {snap.RecentActivities.Count} aktywności.";
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

        var labels = ordered.Select(d => d.Date.ToString("MM-dd")).ToArray();

        HrSeries.Clear();
        HrSeries.Add(new LineSeries<double?>
        {
            Name = "Resting HR",
            Values = ordered.Select(d => (double?)d.RestingHeartRate).ToArray(),
            Stroke = new SolidColorPaint(SKColor.Parse("E57373")) { StrokeThickness = 2 },
            GeometryStroke = new SolidColorPaint(SKColor.Parse("E57373")),
            GeometryFill = new SolidColorPaint(SKColors.White),
            Fill = null
        });

        SleepSeries.Clear();
        SleepSeries.Add(new ColumnSeries<double?>
        {
            Name = "Sen (h)",
            Values = ordered.Select(d => d.SleepHours).ToArray(),
            Fill = new SolidColorPaint(SKColor.Parse("64B5F6"))
        });

        XAxes.Clear();
        XAxes.Add(new Axis
        {
            Labels = labels,
            LabelsRotation = 0,
            TextSize = 11
        });

        YAxesHr.Clear();
        YAxesHr.Add(new Axis { MinLimit = 40, MaxLimit = 80, TextSize = 11 });

        YAxesSleep.Clear();
        YAxesSleep.Add(new Axis { MinLimit = 0, MaxLimit = 10, TextSize = 11 });
    }
}
