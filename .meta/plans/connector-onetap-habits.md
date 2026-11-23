# Connector for OneTap-Habits — plan

> Status: **Implemented in v1 — see `connector/` (PR: Add local-first connector for OneTap-Habits, closes #5).**
> Repo: `Jon2G/OneTap-Habits` · branch: `feat/connector` (from `main`)
> Issue: [#5 — Add a connector for OneTap-Habits](https://github.com/Jon2G/OneTap-Habits/issues/5)
> Owner constraints: **local-first design, $0 cost — no new cloud service, no hosted endpoint.**
> Reported by Jonathan, 2026-11-20.

## 1. Goal

Let Muse work with the OneTap-Habits app directly: list habits, see today's
progress, log completions, decrement counts, and check streaks — without adding
any cloud service, hosted endpoint, or paid dependency.

## 2. Current state (verified from the repo, 2026-11-22)

- **Stack:** .NET 9 MAUI (`src/OneTapHabits.csproj`), Android (+ iOS/Windows
  targets). Firebase Auth + Firestore on the **Spark (free) plan**, project
  `onetap-habits` (see `.meta/plans/free-tier-firebase.md` — the connector
  must also cost $0).
- **Local storage is the source of truth.** `HabitService` / `LogService`
  (`src/Services/`) read local stores first; Firestore sync is fire-and-forget
  in the background (`QueueCloudUpsert` swallows failures: "Local cache remains
  authoritative until next successful sync").
- **Guest mode persists as plain JSON.** `LocalGuestStore`
  (`src/Services/LocalGuestStore.cs`) stores a `GuestDataSnapshot`
  (`{habits: [Habit], logs: [GuestLogEntry]}`) as camelCase JSON in
  `guest_data.json` under `FileSystem.AppDataDirectory`, with static
  `LoadFromPath` / `SaveToPath` / `IncrementCount` / `DecrementCount` helpers.
- **Signed-in mode** uses a separate local cloud cache (`ILocalCloudStore` /
  `CloudCachePersistence`, `src/OneTapHabits.Core/Storage/`) — same habit/log
  shapes, different file.
- **No habit-data export exists in the app.** Settings only exports diagnostic
  logs (`DiagnosticLogService.ExportAndShareAsync`). The connector therefore
  operates on a local copy of `guest_data.json` pulled from the phone.
- **Data model** (from `HabitFirestoreDto` / `GuestDataSnapshot` /
  `HabitDailyTargetHelper` / `HabitScheduleHelper`):
  - `Habit`: `id`, `name`, `colorHex`, `showInWidget`, `targetDays`
    (List<int>, .NET DayOfWeek: Sunday=0..Saturday=6),
    `scheduleMode` (0 = SpecificDays, 1 = TimesPerWeek), `timesPerWeek`,
    `timesPerDay`, `sortOrder`, `reminderEnabled`, `reminderTime` ("HH:mm"),
    `createdAt` (ISO-8601), `isActive`.
  - `GuestLogEntry`: `habitId`, `date` ("yyyy-MM-dd"), `isCompleted`, `count`.
  - One log entry per habit+date; count floors at 0; an entry with count 0 is
    removed, not stored (`LocalGuestStore.AdjustCount`,
    `LogService.UpsertGuestCountAsync`).
  - Daily target = `TimesPerDay` (clamped 1..10) for SpecificDays habits,
    else 1. A day is complete when `count >= dailyTarget`.
  - SpecificDays habits are visible only on their `TargetDays`;
    TimesPerWeek habits are always visible.

## 3. Design (v1, implemented)

### 3.1 Data flow

```text
Phone (guest_data.json) --adb pull--> PC: connector/data/guest_data.json
                                            |
Muse <--stdio/MCP--> connector/server.py <---> local JSON file
                                            |
Phone <--adb push (app force-stopped)--- PC: modified guest_data.json
```

- The connector **never talks to the network**. It reads/writes one local file.
- The app must be force-stopped before pushing a modified file back, or the
  app may overwrite it on its next save. Pulling (read-only) is safe anytime.
- Writes are atomic (temp file + rename) and keep a `.bak` of the previous
  version next to the data file.
- The file path is configurable: `--data-file` argument, then
  `ONETAP_HABITS_DATA_FILE`, then `connector/data/guest_data.json`.

### 3.2 MCP server

- `connector/server.py` — Python 3, **standard library only** (no FastMCP,
  no pip installs, nothing to pay for or keep updated). MCP over stdio,
  newline-delimited JSON-RPC 2.0: `initialize`, `tools/list`, `tools/call`,
  `ping`. Verified against a live stdio session by `connector/test_smoke.py`.
- Habits are addressed by **id or exact name** (case-insensitive); ambiguous
  or unknown references return a tool error naming the problem, never a crash.

### 3.3 Tools

| Tool | What it does | App semantics mirrored |
|---|---|---|
| `list_habits` | Active habits (optionally incl. inactive), sort order then name | `HabitService.OrderHabits` |
| `get_today` | Habits scheduled for a date (default today) with count, daily target, completed | `HabitScheduleHelper.IsVisibleOnDate`, `LogService.GetCountMapForDateAsync` |
| `log_completion` | Count +1 for a habit+date, save | `LogService.IncrementCountAsync` |
| `decrement_count` | Count −1, floored at 0, save | `LogService.DecrementCountAsync` |
| `get_streak` | Consecutive scheduled days meeting the daily target, ending at the most recent completed scheduled day on/before the date | "Completed day = count >= daily target"; lookback 400 days (mirrors `TodayViewModel.StreakLookbackDays`); unscheduled days neither count nor break the streak |

### 3.4 Auth / secrets

- **None.** The connector holds no Firebase credentials, no tokens, no API
  keys. `google-services.json` stays gitignored; the connector never reads it.
- The data file contains only the user's own habit names and counts — it is
  local, never uploaded, and `connector/data/guest_data.json` (the live file)
  is gitignored; only the sample file is committed.

### 3.5 $0 verification

- No new service of any kind: no Cloud Function, no hosted endpoint, no
  Storage, no third-party API. The server makes zero network calls (stdlib
  only, no sockets in the code path).
- Firestore usage is unchanged: the connector does not touch Firebase at all,
  so the Spark budget from `.meta/plans/free-tier-firebase.md` is unaffected.
- Runtime cost: one Python process on the user's own machine, started by the
  MCP client.

## 4. Done When

- [x] Plan written (this file).
- [x] `connector/server.py` implements the 5 tools above with the app's exact
      semantics (daily target, schedule visibility, count floor at 0, one
      entry per habit+date).
- [x] `connector/test_smoke.py` passes (20 checks: initialize, tools/list,
      all 5 tools, persistence to disk, `.bak`, error paths, missing file).
- [x] `connector/README.md` documents setup, the adb pull/push flow, the
      Muse client config, and the $0/local-first constraints.
- [x] `connector/data/sample_guest_data.json` shows the exact file shape.
- [x] No secrets committed; no new dependency; no `src/` changes (Android
      build unaffected).
- [ ] Jonathan reviews the draft PR; the data file flow (adb pull/push) is
      confirmed usable from his rooted Pixel.

## 5. Risks / open questions

- **Signed-in mode is out of scope for v1.** The connector reads the guest
  file (`guest_data.json`). If Jonathan signs in, habits live in the cloud
  cache file instead; the connector would need a `--store cloud` mode (or the
  app's local cloud cache path) — flagged for v2, not built here.
- **File race with a running app.** The app loads the file on each operation
  and saves on change. The README's force-stop-before-push rule is the v1
  mitigation; a future in-app export/import would remove the manual step.
- **No in-app export yet.** v1 relies on `adb pull` (works on Jonathan's
  rooted Pixel). A Settings → "Export habit data" action in the app is the
  natural follow-up so non-rooted users can use the connector too.
- **Streak rule is the connector's definition**, documented on `get_streak`:
  consecutive scheduled days meeting the target, ending at the most recent
  completed scheduled day. It matches the app's "completed day" rule; the
  app's own streak label logic was not re-implemented, only the day rule.

## 6. Key file references

- `connector/server.py` — MCP server (stdlib only)
- `connector/test_smoke.py` — smoke test (run: `python3 connector/test_smoke.py`)
- `connector/README.md` — setup + data flow + Muse config
- `connector/data/sample_guest_data.json` — file shape example
- `connector/mcp-config.example.json` — MCP client config template
- App: `src/Services/LocalGuestStore.cs`, `src/Services/LogService.cs`,
  `src/Services/HabitService.cs`,
  `src/OneTapHabits.Core/Models/GuestDataSnapshot.cs`,
  `src/OneTapHabits.Core/Services/HabitDailyTargetHelper.cs`,
  `src/OneTapHabits.Core/Services/HabitScheduleHelper.cs`
