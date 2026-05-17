# About GarminCoach

GarminCoach is a single-user Windows desktop app (.NET 8, WPF) that turns the
raw biometric data from your Garmin Connect account into actionable coaching
feedback. It pulls your last week of steps, resting and max heart rate,
sleep stages, body battery, stress, HRV, weight, and activities, then hands
that snapshot to an Anthropic Claude model acting as a personal trainer.

Two surfaces share the same data context: a dashboard with cards, charts,
and a recent-activities grid; and a chat panel where you can have a
multi-turn conversation with the coach. The same `CoachSnapshot` is fed to
Claude as a system prompt on every turn, so every answer is grounded in your
actual numbers and never invented.

Built for personal use, not as a service. Credentials are stored locally
with Windows DPAPI, no backend sits in the middle, and the app only ever
reads from Garmin — nothing is written back.
