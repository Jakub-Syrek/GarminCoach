# GarminCoach

WPF desktop app (.NET 8, Windows). Reads your Garmin Connect data and lets a Claude-powered AI coach analyze it. Two-pane layout: dashboard on the left (cards, charts, recent activities, generated written report), chat on the right (multi-turn conversation with the coach, fed the same data as context).

## About

A personal-use Windows tool for athletes who want their Garmin watch data interpreted by an actual coach instead of stared at as a wall of numbers. The watch already knows your resting HR, sleep stages, training load, and what you did this week. What it doesn't tell you is whether you're walking into an overreach, whether yesterday's run was too hard given your sleep, or what to do tomorrow. This app closes that loop: it pulls a fresh snapshot from Garmin Connect, hands it to Claude with a coach persona, and you get back grounded, numbers-cited feedback — either as a written report or as a back-and-forth chat.

Built for one user (you), not a SaaS — credentials live on your machine in DPAPI-encrypted form, you bring your own Anthropic API key, and there's no backend in the middle. Reads only; the app never writes anything back to Garmin.

## What it does

- Logs into Garmin Connect with email/password (community library `Unofficial.Garmin.Connect`)
- Pulls the last N days (default 7) of: steps, resting/min/max HR, sleep duration + score, body battery min-max, stress, weight, plus the full list of activities in that window
- Aggregates everything into a compact context block (`Services/CoachContextBuilder.cs`) and sends it as the **system prompt** to Claude on every chat turn or report request
- Stores Garmin credentials + Anthropic API key locally in `%AppData%\GarminCoach\secrets.bin`, encrypted with **Windows DPAPI** (current-user scope — readable only by your Windows account on this machine)

## What it does NOT do

- No streaming (responses arrive as a single block). LiveCharts2 handles charting; Anthropic.SDK `GetClaudeMessageAsync` is non-streaming. Streaming is a straightforward swap to `StreamClaudeMessageAsync` later.
- No sync to Garmin (read-only).
- No background fetch — you click "Odśwież dane" yourself.

## Build & run

Requires .NET 8 SDK or newer.

```powershell
dotnet build GarminCoach\GarminCoach.csproj
dotnet run --project GarminCoach\GarminCoach.csproj
```

On first launch the settings dialog opens. Provide:

1. **Garmin Connect email + hasło** — same as in the Garmin app
2. **Anthropic API key** — `sk-ant-...` from [console.anthropic.com](https://console.anthropic.com)
3. **Model** — Haiku 4.5 (cheap/fast), Sonnet 4.6 (default), or Opus 4.7 (most capable)
4. **Liczba dni** — how many days back to pull (7 is a good default)

Click "Odśwież dane" on the dashboard to fetch from Garmin. Then either "Generuj raport coacha" for a written summary, or just type in the chat panel on the right.

`Ctrl+Enter` in the chat box sends the message.

## Architecture

```
GarminCoach/
  Models/
    ActivitySummary.cs       Domain record for one activity
    DailyMetrics.cs          One day of biometrics
    CoachSnapshot.cs         Bundled view of last N days + activities
    ChatMessage.cs           Role + content
    AppSecrets.cs            Persisted user secrets

  Services/
    SettingsStore.cs         DPAPI-encrypted JSON at %AppData%\GarminCoach
    IGarminService.cs        Snapshot fetch contract
    GarminService.cs         Wraps Unofficial.Garmin.Connect; maps 0-valued
                             non-nullable Garmin fields to nullable domain
    IClaudeService.cs        Coach-context-aware chat contract
    ClaudeService.cs         Anthropic.SDK; system prompt = built context
    CoachContextBuilder.cs   Compact Polish coach prompt + tabular data

  ViewModels/
    MainViewModel.cs         Composes Dashboard + Chat + Settings;
                             wires SnapshotChanged → Chat.SetSnapshot
    DashboardViewModel.cs    Refresh, GenerateReport; chart series + cards
    ChatViewModel.cs         Messages, SendCommand, IsBusy
    SettingsViewModel.cs     Bound to the settings dialog

  Views/
    DashboardView.xaml       4 cards + 2 LiveCharts + activities grid +
                             optional report panel
    ChatView.xaml            Scrolling bubble list + input box
    SettingsDialog.xaml      First-run + edit form
  MainWindow.xaml            Header + GridSplitter (Dashboard | Chat)

  Converters/                BoolToVis (built-in), InvertBool, StringToVis
  App.xaml.cs                Microsoft.Extensions.Hosting bootstrap;
                             singleton DI for services + viewmodels
```

The **only** state shared between the dashboard and the chat is the `CoachSnapshot`. When the user clicks "Odśwież dane", `DashboardViewModel` fetches a fresh snapshot and raises `SnapshotChanged`; `MainViewModel` forwards it to `ChatViewModel.SetSnapshot()`, which in turn feeds it into every Claude call via `CoachContextBuilder.SystemPrompt(snap)`.

## Notes / caveats

- **Garmin auth is fragile.** Garmin changes their SSO/OAuth flow periodically. `Unofficial.Garmin.Connect` 0.9.2 works as of 2026-05-14 but may break. When it does, the workaround is either upgrade the NuGet or swap to a Python sidecar using `python-garminconnect` (mature, maintained). `IGarminService` is the only seam to change.
- **API keys on disk.** DPAPI ties the secret to your Windows user account on this specific machine. Re-imaging or copying the file to another user won't decrypt it. If you want roaming, swap `SettingsStore` to use Windows Credential Manager.
- **No retry/backoff** in the Garmin client. If Garmin returns a transient 5xx, just click Refresh again.
- **Token budget.** The system prompt scales with `DaysToFetch * (~12 fields)` + one row per activity. At 7 days and ~20 activities you're looking at ~1-2k input tokens per call — cheap. Don't bump days to 90 without thinking about cost.
- **MFA.** If your Garmin account has 2FA enabled, the current library path won't work — `GarminConnectContext` supports an `IMfaCodeProvider` but the wiring isn't hooked up yet.
