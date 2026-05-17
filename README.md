# GarminCoach

> A Windows desktop coach that turns your Garmin Connect data into grounded, numbers-cited training advice from Claude.

[![Tests](https://github.com/Jakub-Syrek/GarminCoach/actions/workflows/tests.yml/badge.svg)](https://github.com/Jakub-Syrek/GarminCoach/actions/workflows/tests.yml)
[![Release](https://img.shields.io/github/v/release/Jakub-Syrek/GarminCoach)](https://github.com/Jakub-Syrek/GarminCoach/releases)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/github/license/Jakub-Syrek/GarminCoach)
![Last commit](https://img.shields.io/github/last-commit/Jakub-Syrek/GarminCoach)
![Tests](https://img.shields.io/badge/tests-44-success)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
[![Garmin Connect](https://img.shields.io/badge/data-Garmin%20Connect-007CC3?logo=garmin&logoColor=white)](https://connect.garmin.com)
[![Anthropic Claude](https://img.shields.io/badge/AI-Anthropic%20Claude-CC785C)](https://www.anthropic.com)

## Overview

GarminCoach is a personal-use Windows tool for athletes who want their Garmin
watch data interpreted by an actual coach instead of stared at as a wall of
numbers. The watch already knows your resting heart rate, sleep stages,
training load, and what you did this week. What it does not tell you is
whether you are walking into an overreach, whether yesterday's run was too
hard given your sleep, or what to do tomorrow. This app closes that loop: it
pulls a fresh snapshot from Garmin Connect, hands it to Claude with a coach
persona, and you get back grounded, numbers-cited feedback — either as a
written report or as a back-and-forth chat.

Built for one user, not as a SaaS. Credentials live on your machine in
DPAPI-encrypted form, you bring your own Anthropic API key, and there is no
backend in the middle. Reads only; the app never writes anything back to
Garmin.

## Features

- Logs into Garmin Connect with email and password through the community
  library `Unofficial.Garmin.Connect`.
- Pulls a configurable window (default 7 days) of: steps, resting / min /
  max heart rate, sleep duration and score, sleep stages, body battery min
  and max plus charged / drained delta, stress, HRV overnight and weekly
  average with status, weight, and the full list of activities in that
  window.
- Aggregates everything into a compact context block (Polish coaching
  persona, English data labels) and sends it as the system prompt to
  Claude on every chat turn or report request.
- Categorises activities (running, cycling, gym, climbing, mountain,
  swimming, walking, other) and renders per-category sums with duration,
  distance, elevation, calories, training load, and duration-weighted
  average heart rate.
- Two surfaces sharing one snapshot: dashboard with cards and charts on
  the left, multi-turn chat on the right.
- Stores Garmin credentials and Anthropic API key locally in
  `%AppData%\GarminCoach\secrets.bin`, encrypted with Windows DPAPI under
  the current user account.

## Tech stack

- **Language / runtime:** C# 12 on .NET 8 (`net8.0-windows`).
- **UI:** WPF with a dark theme and a custom title bar.
- **MVVM:** CommunityToolkit.Mvvm 8.4.
- **Charts:** LiveChartsCore.SkiaSharpView.WPF 2.0.
- **AI client:** Anthropic.SDK 5.10 (non-streaming `GetClaudeMessageAsync`).
- **Garmin client:** Unofficial.Garmin.Connect 0.9.2.
- **Hosting / DI:** Microsoft.Extensions.Hosting 8.0.
- **Tests:** xUnit 2.5 with coverlet collector.

## Architecture

```
GarminCoach/
  Models/
    ActivityCategory.cs              Enum + Classify(typeKey) + ToPolish()
    ActivityCategoryAggregate.cs     Per-category roll-up record
    ActivitySummary.cs               Domain record for one activity
    DailyMetrics.cs                  One day of biometrics
    CoachSnapshot.cs                 Bundled view of last N days + activities
    ChatMessage.cs                   Role + content
    AppSecrets.cs                    Persisted user secrets

  Services/
    ActivityAggregator.cs            Groups activities by category, sums and
                                      computes duration-weighted average HR
    SettingsStore.cs                 DPAPI-encrypted JSON at %AppData%\GarminCoach
    IGarminService.cs                Snapshot fetch contract
    GarminService.cs                 Wraps Unofficial.Garmin.Connect; maps
                                      0-valued non-nullable Garmin fields to
                                      nullable domain values
    IClaudeService.cs                Coach-context-aware chat contract
    ClaudeService.cs                 Anthropic.SDK; system prompt = built context
    CoachContextBuilder.cs           Compact coach prompt + tabular data

  ViewModels/
    MainViewModel.cs                 Composes Dashboard + Chat + Settings;
                                      wires SnapshotChanged -> Chat.SetSnapshot
    DashboardViewModel.cs            Refresh, GenerateReport; chart series + cards
    ChatViewModel.cs                 Messages, SendCommand, IsBusy
    SettingsViewModel.cs             Bound to the settings dialog

  Views/
    DashboardView.xaml               4 cards + 2 LiveCharts + activities grid +
                                      optional report panel
    ChatView.xaml                    Scrolling bubble list + input box
    SettingsDialog.xaml              First-run + edit form
  MainWindow.xaml                    Header + GridSplitter (Dashboard | Chat)

  Converters/                        BoolToVis (built-in), InvertBool, StringToVis
  App.xaml.cs                        Microsoft.Extensions.Hosting bootstrap;
                                      singleton DI for services + view models
```

The **only** state shared between the dashboard and the chat is the
`CoachSnapshot`. When the user clicks the refresh button,
`DashboardViewModel` fetches a fresh snapshot and raises `SnapshotChanged`;
`MainViewModel` forwards it to `ChatViewModel.SetSnapshot()`, which in turn
feeds it into every Claude call via `CoachContextBuilder.SystemPrompt(snap)`.

## Build and run

Requires the .NET 8 SDK on Windows.

```powershell
dotnet build GarminCoach\GarminCoach.csproj
dotnet run --project GarminCoach\GarminCoach.csproj
```

On first launch the settings dialog opens. Provide:

1. Garmin Connect email and password (same as in the Garmin app).
2. Anthropic API key (`sk-ant-...`) from
   [console.anthropic.com](https://console.anthropic.com).
3. Claude model: Haiku 4.5 (cheap and fast), Sonnet 4.6 (default), or
   Opus 4.7 (most capable).
4. Number of days back to pull (7 is a good default).

Click the refresh action on the dashboard to fetch from Garmin. Then either
generate a written report, or just type in the chat panel on the right.
`Ctrl+Enter` in the chat box sends the message.

## Testing

```powershell
dotnet test
```

The xUnit suite covers activity classification, per-category aggregation
(including duration-weighted average HR), coach-context rendering (day
ordering, missing-field placeholders, pace formatting, HRV / sleep stage
rendering, category aggregate tables), and the DPAPI settings store
round-trip. All 44 tests must pass before merging to `main`.

## Versioning

Releases follow [Semantic Versioning](https://semver.org/). Commit messages
follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` -> minor bump
- `fix:` / `chore:` / `docs:` / `test:` / `refactor:` / `ci:` -> patch bump
- `BREAKING CHANGE:` in the body or `!` after the type -> major bump

The `version.yml` workflow runs on every push to `main`, computes the next
version from commits since the last tag, updates `<Version>` in the main
project, commits `chore(release): vX.Y.Z`, and pushes a matching tag. The
`release.yml` workflow then takes over: it builds self-contained and
framework-dependent `win-x64` artifacts and attaches them to an
auto-generated GitHub Release.

See [CHANGELOG.md](./CHANGELOG.md) for the human-readable history.

## License

[MIT](./LICENSE) (c) 2026 Jakub Syrek.

## Contact

- Author: Jakub Syrek
- Email: <jakubvonsyrek@gmail.com>
- Security reports: see [SECURITY.md](./SECURITY.md).
