# OneTap-Habits connector

Local-first MCP server so Muse can work with the OneTap-Habits app directly.
**$0, no cloud, no hosted endpoint, no credentials** — it reads and writes the
app's own local data file, `guest_data.json`, over stdio with Python 3
(standard library only, no dependencies to install).

## How it works

The app's guest mode stores everything in one JSON file
(`GuestDataSnapshot`: `{habits, logs}`) — see `LocalGuestStore` in the app.
The connector operates on a local copy of that file:

```text
Phone (guest_data.json) --adb pull--> PC: connector/data/guest_data.json
                                            |
Muse <--stdio/MCP--> connector/server.py <---> local JSON file
                                            |
Phone <--adb push (app force-stopped)--- PC: modified guest_data.json
```

Writes are atomic (temp file + rename) and keep a `.bak` of the previous
version. The connector makes **zero network calls**.

## Setup

Requirements: Python 3.10+ (tested on 3.12). Nothing to install.

### 1. Get the data file from the phone

On a rooted phone (or a debuggable build via `run-as`):

```bash
# Pull the live file (read-only, safe anytime)
adb pull /data/data/com.jon2g.onetaphabits/files/guest_data.json \
  connector/data/guest_data.json
```

The live file `connector/data/guest_data.json` is gitignored — only the sample
is committed. A shape example lives at
`connector/data/sample_guest_data.json`.

### 2. Point Muse at the server

Copy `mcp-config.example.json` into your MCP client config and fix the two
paths (`server.py` and your `guest_data.json`):

```json
{
  "mcpServers": {
    "onetap-habits": {
      "command": "python3",
      "args": ["/path/to/OneTap-Habits/connector/server.py",
               "--data-file", "/path/to/guest_data.json"]
    }
  }
}
```

Or set `ONETAP_HABITS_DATA_FILE` instead of passing `--data-file`. Without
either, the server uses `connector/data/guest_data.json` next to `server.py`.

### 3. Push changes back (only needed after a write)

If Muse logged a completion or decremented a count, the change lives in the
local file. To see it in the app:

```bash
adb shell am force-stop com.jon2g.onetaphabits   # app must be closed first,
                                                  # or it may overwrite the file
adb push connector/data/guest_data.json \
  /data/data/com.jon2g.onetaphabits/files/guest_data.json
```

Then reopen the app. Reads (`list_habits`, `get_today`, `get_streak`) need no
push — they never modify the file.

## Tools

| Tool | Arguments | What it does |
|---|---|---|
| `list_habits` | `include_inactive?` | All active habits, sort order then name |
| `get_today` | `date?` (yyyy-MM-dd, default today) | Habits scheduled for the date, with count, daily target, completed |
| `log_completion` | `habit` (id or exact name), `date?` | Count +1 and save (like tapping a habit card) |
| `decrement_count` | `habit`, `date?` | Count −1, floored at 0, and save |
| `get_streak` | `habit`, `as_of_date?` | Current streak: consecutive scheduled days meeting the daily target |

Semantics mirror the app exactly: daily target = `TimesPerDay` (1–10) for
SpecificDays habits, else 1; a day is complete when count >= target;
SpecificDays habits show only on their `TargetDays` (Sunday=0..Saturday=6);
one log entry per habit+date, removed when the count reaches 0.

## Smoke test

```bash
python3 connector/test_smoke.py
# All 20 smoke checks passed.
```

It spawns the real server over stdio and exercises every tool against a
temporary data file (fixture dates are relative to today, so the streak
assertions are stable), including persistence to disk, the `.bak`, and the
error paths (unknown habit, invalid date, missing file).

## Constraints and scope (v1)

- **Local-first, $0.** No Firebase calls, no tokens, no new service, no paid
  dependency. Firestore usage is unchanged (the connector never touches it).
- **Guest file only.** v1 reads `guest_data.json` (guest mode). Signed-in
  users' habits live in the separate local cloud cache — that is a v2
  follow-up, not built here.
- **No in-app export yet.** v1 relies on `adb pull`. A Settings →
  "Export habit data" action in the app would make this usable without root.
- **Single user, single writer at a time.** Don't run the connector's writes
  while the app is open; force-stop the app before pushing a modified file
  back (see step 3 above).
