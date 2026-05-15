using System.Globalization;
using System.Text;
using GarminCoach.Models;

namespace GarminCoach.Services;

public static class CoachContextBuilder
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string SystemPrompt(CoachSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Jesteś doświadczonym trenerem personalnym (siła, wytrzymałość, sporty górskie i wspinaczka) komentującym dane biometryczne klienta z zegarka Garmin.");
        sb.AppendLine("Odpowiadasz po polsku, krótko i konkretnie. Cytuj liczby tam gdzie pomagają. Nie wymyślaj danych których nie ma.");
        sb.AppendLine("Jeśli pole jest puste mów wprost że brak pomiaru zamiast spekulować.");
        sb.AppendLine("Uwzględniaj specyfikę dyscyplin: bieganie inaczej obciąża niż wspinaczka, siłownia inaczej niż alpinizm — komentarz dopasuj do typu aktywności w ostatnich dniach.");
        sb.AppendLine();
        sb.AppendLine("=== DANE KLIENTA ===");
        if (snapshot.UserDisplayName is { Length: > 0 } name)
        {
            sb.Append("Klient: ").AppendLine(name);
        }
        sb.Append("Snapshot pobrany: ").AppendLine(snapshot.FetchedAt.ToString("yyyy-MM-dd HH:mm", Inv));
        sb.AppendLine();

        sb.AppendLine("Dane dobowe (najnowsze pierwsze):");
        sb.AppendLine("Data | Kroki | RHR | Sen [h] (Deep/Light/REM/Awake) | Score | HRV (status) | BB (min-max, Δ) | Stres | Waga [kg]");
        foreach (var d in snapshot.Days.OrderByDescending(x => x.Date))
        {
            var sleepBreakdown = (d.SleepDeepHours.HasValue || d.SleepLightHours.HasValue ||
                                  d.SleepRemHours.HasValue || d.SleepAwakeHours.HasValue)
                ? $" ({Hr(d.SleepDeepHours)}/{Hr(d.SleepLightHours)}/{Hr(d.SleepRemHours)}/{Hr(d.SleepAwakeHours)})"
                : "";
            var hrvCell = d.HrvOvernight.HasValue
                ? $"{d.HrvOvernight} ({d.HrvStatus ?? "?"})"
                : "—";
            var bbCell = (d.BodyBatteryMin.HasValue && d.BodyBatteryMax.HasValue)
                ? $"{d.BodyBatteryMin}-{d.BodyBatteryMax}, +{d.BodyBatteryCharged ?? 0}/-{d.BodyBatteryDrained ?? 0}"
                : "—";

            sb.AppendLine(string.Join(" | ", new[]
            {
                d.Date.ToString("yyyy-MM-dd", Inv),
                Fmt(d.Steps),
                Fmt(d.RestingHeartRate),
                d.SleepHours.HasValue ? d.SleepHours.Value.ToString("F1", Inv) + sleepBreakdown : "—",
                Fmt(d.SleepScore),
                hrvCell,
                bbCell,
                Fmt(d.StressAvg),
                d.Weight.HasValue ? d.Weight.Value.ToString("F1", Inv) : "—"
            }));
        }

        sb.AppendLine();
        sb.AppendLine($"Aktywności w okresie ({snapshot.RecentActivities.Count}):");
        if (snapshot.RecentActivities.Count == 0)
        {
            sb.AppendLine("— brak —");
        }
        else
        {
            foreach (var a in snapshot.RecentActivities)
            {
                var category = ActivityCategoryExtensions.Classify(a.ActivityType);
                var dist = a.DistanceMeters.HasValue
                    ? (a.DistanceMeters.Value / 1000.0).ToString("F2", Inv) + " km"
                    : "—";
                var pace = a.AverageSpeedMps is > 0
                    ? FormatPace(a.AverageSpeedMps.Value)
                    : "—";
                var elev = a.ElevationGainMeters is > 0
                    ? $", D+ {a.ElevationGainMeters:F0} m"
                    : "";
                sb.Append($"- {a.StartTime:yyyy-MM-dd HH:mm} ");
                sb.Append($"[{category.ToPolish()}/{a.ActivityType}] {a.Name}; ");
                sb.Append($"czas {FormatDuration(a.Duration)}, dyst {dist}{elev}, ");
                sb.Append($"avgHR {Fmt(a.AverageHr)}, maxHR {Fmt(a.MaxHr)}, ");
                sb.Append($"kcal {Fmt(a.Calories)}, pace {pace}");
                if (a.TrainingLoad is > 0) sb.Append($", TSS {a.TrainingLoad.Value:F0}");
                sb.AppendLine();
            }
        }

        if (snapshot.CategoryAggregates.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Suma wg kategorii (w pobranym okresie):");
            sb.AppendLine("Kategoria | Sesji | Czas | Dystans | D+ [m] | avgHR | Kalorie | TSS");
            foreach (var c in snapshot.CategoryAggregates)
            {
                sb.AppendLine(string.Join(" | ", new[]
                {
                    c.Category.ToPolish(),
                    c.SessionCount.ToString(Inv),
                    FormatDuration(c.TotalDuration),
                    c.TotalDistanceMeters > 0
                        ? (c.TotalDistanceMeters / 1000.0).ToString("F1", Inv) + " km"
                        : "—",
                    c.TotalElevationGainMeters > 0
                        ? c.TotalElevationGainMeters.ToString("F0", Inv)
                        : "—",
                    c.AverageHr.HasValue ? c.AverageHr.Value.ToString("F0", Inv) : "—",
                    c.TotalCalories > 0 ? c.TotalCalories.ToString("F0", Inv) : "—",
                    c.TotalTrainingLoad.HasValue ? c.TotalTrainingLoad.Value.ToString("F0", Inv) : "—"
                }));
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== KONIEC DANYCH ===");
        return sb.ToString();
    }

    public static string DefaultReportPrompt() =>
        "Daj mi krótki raport coacha z ostatnich dni: 1) co działa dobrze, 2) co poprawić (uwzględnij HRV, sen i body battery), 3) konkretna sugestia na jutro dopasowana do dyscyplin które trenuję. Max 8 zdań.";

    private static string Fmt(int? v) => v.HasValue ? v.Value.ToString(Inv) : "—";
    private static string Fmt(double? v) => v.HasValue ? v.Value.ToString("F0", Inv) : "—";
    private static string Hr(double? v) => v.HasValue ? v.Value.ToString("F1", Inv) : "—";

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
        return $"{ts.Minutes}m {ts.Seconds:D2}s";
    }

    private static string FormatPace(double speedMps)
    {
        var secPerKm = 1000.0 / speedMps;
        var m = (int)(secPerKm / 60);
        var s = (int)(secPerKm % 60);
        return $"{m}:{s:D2}/km";
    }
}
