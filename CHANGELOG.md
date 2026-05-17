# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-17

### Added

- WPF desktop client (.NET 8, Windows) with a two-pane layout: dashboard on
  the left, chat on the right, separated by a draggable splitter.
- Garmin Connect integration via `Unofficial.Garmin.Connect` 0.9.2. Pulls
  daily metrics (steps, RHR, min/max HR, sleep duration and score, sleep
  stages, body battery min/max plus charged/drained delta, stress, HRV
  overnight + weekly average + status, weight) and the full activity list
  for a configurable window (default 7 days).
- Anthropic Claude integration via `Anthropic.SDK` 5.10 with selectable model
  (Haiku 4.5, Sonnet 4.6, Opus 4.7) and a coach-context system prompt built
  from the live snapshot.
- `CoachContextBuilder` that renders a compact daily table plus per-activity
  detail and per-category aggregates (running, cycling, gym, climbing,
  mountain, swimming, walking, other) for the Claude system prompt.
- `ActivityAggregator` that groups activities by category, sums duration,
  distance, elevation, calories, and training load, and computes a duration-
  weighted average heart rate per category.
- DPAPI-encrypted `SettingsStore` persisting Garmin credentials, Anthropic
  API key, selected model, and fetch window at
  `%AppData%\GarminCoach\secrets.bin`.
- Dark-theme custom title bar, four headline cards, two LiveCharts2 plots,
  recent activities grid, and an optional generated written report panel.
- xUnit test suite (44 tests) covering activity classification, category
  aggregation, coach context rendering, and settings store round-trips.
- GitHub Actions CI (`tests.yml`) running restore, build, and `dotnet test`
  on push and pull request, with `.trx` results uploaded as an artifact.
- Auto-versioning workflow (`version.yml`) that parses conventional commits
  on `main`, bumps `<Version>` in the project file, commits
  `chore(release): vX.Y.Z`, tags, and creates a GitHub Release.
- Release workflow (`release.yml`) that publishes self-contained and
  framework-dependent `win-x64` zips on every `v*` tag.
- Issue and pull request templates.
- `SECURITY.md` describing the vulnerability disclosure process.

[Unreleased]: https://github.com/Jakub-Syrek/GarminCoach/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Jakub-Syrek/GarminCoach/releases/tag/v1.0.0
