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
        sb.AppendLine("Jesteś doświadczonym trenerem personalnym (siła + wytrzymałość) komentującym dane biometryczne klienta z zegarka Garmin.");
        sb.AppendLine("Odpowiadasz po polsku, krótko i konkretnie. Cytuj liczby tam gdzie pomagają. Nie wymyślaj danych, których nie ma.");
        sb.AppendLine("Jeśli pole jest puste, mów wprost że brak pomiaru, zamiast spekulować.");
        sb.AppendLine();
        sb.AppendLine("=== DANE KLIENTA ===");
        if (snapshot.UserDisplayName is { Length: > 0 } name)
        {
            sb.Append("Klient: ").AppendLine(name);
        }
        sb.Append("Snapshot pobrany: ").AppendLine(snapshot.FetchedAt.ToString("yyyy-MM-dd HH:mm", Inv));
        sb.AppendLine();
        sb.AppendLine("Dane dobowe (najnowsze ostatnie):");
        sb.AppendLine("Data | Kroki | RHR | Max HR | Sen [h] | Sen score | BB min-max | Stres | Waga [kg]");
        foreach (var d in snapshot.Days.OrderByDescending(x => x.Date))
        {
            sb.AppendLine(string.Join(" | ", new[]
            {
                d.Date.ToString("yyyy-MM-dd", Inv),
                Fmt(d.Steps),
                Fmt(d.RestingHeartRate),
                Fmt(d.MaxHeartRate),
                d.SleepHours.HasValue ? d.SleepHours.Value.ToString("F1", Inv) : "—",
                Fmt(d.SleepScore),
                d.BodyBatteryMin.HasValue && d.BodyBatteryMax.HasValue
                    ? $"{d.BodyBatteryMin}-{d.BodyBatteryMax}" : "—",
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
                var dist = a.DistanceMeters.HasValue
                    ? (a.DistanceMeters.Value / 1000.0).ToString("F2", Inv) + " km"
                    : "—";
                var pace = a.AverageSpeedMps is > 0
                    ? FormatPace(a.AverageSpeedMps.Value)
                    : "—";
                sb.Append($"- {a.StartTime:yyyy-MM-dd HH:mm} ");
                sb.Append($"[{a.ActivityType}] {a.Name}; ");
                sb.Append($"czas {FormatDuration(a.Duration)}, dyst {dist}, ");
                sb.Append($"avgHR {Fmt(a.AverageHr)}, maxHR {Fmt(a.MaxHr)}, ");
                sb.Append($"kcal {Fmt(a.Calories)}, pace {pace}");
                if (a.TrainingLoad is > 0) sb.Append($", TSS {a.TrainingLoad.Value:F0}");
                sb.AppendLine();
            }
        }

        sb.AppendLine();
        sb.AppendLine("=== KONIEC DANYCH ===");
        return sb.ToString();
    }

    public static string DefaultReportPrompt() =>
        "Daj mi krótki raport coacha z ostatnich dni: 1) co działa dobrze, 2) co poprawić, 3) konkretna sugestia na jutro. Max 6 zdań.";

    private static string Fmt(int? v) => v.HasValue ? v.Value.ToString(Inv) : "—";
    private static string Fmt(double? v) => v.HasValue ? v.Value.ToString("F0", Inv) : "—";

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
