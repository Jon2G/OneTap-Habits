#!/usr/bin/env python3
"""OneTap-Habits connector — local-first MCP server (v1).

Reads and writes the app's local guest data file (``guest_data.json``), the
same camelCase JSON that ``LocalGuestStore`` in the app persists
(``GuestDataSnapshot``: ``{habits, logs}``). No network, no Firebase, no
credentials, no paid dependency — Python 3 standard library only.

Protocol: MCP over stdio, newline-delimited JSON-RPC 2.0
(``initialize`` / ``tools/list`` / ``tools/call`` / ``ping``).

Data file resolution (first match wins):
  1. ``--data-file`` argument
  2. ``ONETAP_HABITS_DATA_FILE`` environment variable
  3. ``data/guest_data.json`` next to this script

The file is the app's own ``guest_data.json`` (see connector/README.md for how
to pull it from the phone). Writes are atomic (temp file + rename) and keep a
``.bak`` copy of the previous version. The app must be force-stopped before
pushing a modified file back to the phone, or the app may overwrite it.

Semantics mirror the app exactly (see ``.meta/plans/connector-onetap-habits.md``):
  * one log entry per habit+date (``{"habitId", "date": "yyyy-MM-dd",
    "isCompleted", "count"}``); entries with count 0 are removed, not stored
  * daily target = TimesPerDay (clamped 1..10) for SpecificDays habits, else 1
  * a day counts as completed when count >= daily target
  * SpecificDays habits are visible only on their TargetDays (.NET DayOfWeek:
    Sunday=0 .. Saturday=6); TimesPerWeek habits are always visible
"""

from __future__ import annotations

import argparse
import copy
import datetime as _dt
import json
import os
import sys

SERVER_NAME = "onetap-habits"
SERVER_VERSION = "0.1.0"
PROTOCOL_VERSION = "2024-11-05"

SCHEDULE_SPECIFIC_DAYS = 0
SCHEDULE_TIMES_PER_WEEK = 1
STREAK_LOOKBACK_DAYS = 400  # mirrors TodayViewModel.StreakLookbackDays


class ToolError(Exception):
    """An error that should be returned to the MCP client as a tool error."""


# ---------------------------------------------------------------------------
# Data file
# ---------------------------------------------------------------------------

def default_data_file() -> str:
    here = os.path.dirname(os.path.abspath(__file__))
    return os.path.join(here, "data", "guest_data.json")


def resolve_data_file(cli_path: str | None) -> str:
    if cli_path:
        return os.path.abspath(cli_path)
    env_path = os.environ.get("ONETAP_HABITS_DATA_FILE")
    if env_path:
        return os.path.abspath(os.path.expandvars(os.path.expanduser(env_path)))
    return default_data_file()


def load_snapshot(path: str) -> dict:
    if not os.path.exists(path):
        raise ToolError(
            f"Data file not found: {path}\n"
            "Pull guest_data.json from the phone first — see connector/README.md."
        )
    try:
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except (OSError, ValueError) as e:
        raise ToolError(f"Could not read data file {path}: {e}")
    if not isinstance(data, dict) or not isinstance(data.get("habits"), list) \
            or not isinstance(data.get("logs"), list):
        raise ToolError(
            f"Data file {path} is not a guest_data.json snapshot "
            "(expected {\"habits\": [...], \"logs\": [...]})."
        )
    return data


def save_snapshot(path: str, snapshot: dict) -> None:
    """Atomic save (temp file + rename) with a .bak of the previous version."""
    directory = os.path.dirname(path) or "."
    os.makedirs(directory, exist_ok=True)
    if os.path.exists(path):
        try:
            with open(path, "rb") as src, open(path + ".bak", "wb") as dst:
                dst.write(src.read())
        except OSError:
            pass  # backup is best-effort; the atomic write below still protects data
    tmp_path = path + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        json.dump(snapshot, f, ensure_ascii=False, separators=(",", ":"))
        f.write("\n")
    os.replace(tmp_path, path)


# ---------------------------------------------------------------------------
# App semantics (mirrors src/Services, src/OneTapHabits.Core)
# ---------------------------------------------------------------------------

def daily_target(habit: dict) -> int:
    if habit.get("scheduleMode", 0) == SCHEDULE_SPECIFIC_DAYS:
        return max(1, min(10, int(habit.get("timesPerDay") or 1)))
    return 1


def dotnet_day_of_week(date: _dt.date) -> int:
    """Python Mon=0..Sun=6 -> .NET DayOfWeek Sun=0..Sat=6."""
    return (date.weekday() + 1) % 7


def is_scheduled(habit: dict, date: _dt.date) -> bool:
    if habit.get("scheduleMode", 0) == SCHEDULE_TIMES_PER_WEEK:
        return True
    return dotnet_day_of_week(date) in (habit.get("targetDays") or [])


def parse_date(value: str | None, name: str = "date") -> _dt.date:
    if value is None:
        return _dt.date.today()
    try:
        return _dt.date.fromisoformat(value)
    except ValueError:
        raise ToolError(f"Invalid {name} '{value}': expected yyyy-MM-dd.")


def count_for(snapshot: dict, habit_id: str, date_key: str) -> int:
    for entry in snapshot["logs"]:
        if entry.get("habitId") == habit_id and entry.get("date") == date_key:
            return int(entry.get("count") or 0)
    return 0


def set_count(snapshot: dict, habit_id: str, date_key: str, count: int) -> None:
    """Mirror LocalGuestStore.AdjustCount: one entry per habit+date, removed at 0."""
    snapshot["logs"] = [
        e for e in snapshot["logs"]
        if not (e.get("habitId") == habit_id and e.get("date") == date_key)
    ]
    if count > 0:
        snapshot["logs"].append({
            "habitId": habit_id,
            "date": date_key,
            "isCompleted": True,
            "count": count,
        })


def find_habit(snapshot: dict, ref: str) -> dict:
    """Match by id (exact) or name (case-insensitive exact)."""
    ref = (ref or "").strip()
    if not ref:
        raise ToolError("Missing habit: pass an id or a name (see list_habits).")
    for habit in snapshot["habits"]:
        if habit.get("id") == ref:
            return habit
    matches = [h for h in snapshot["habits"]
               if str(h.get("name", "")).lower() == ref.lower()]
    if len(matches) == 1:
        return matches[0]
    if len(matches) > 1:
        raise ToolError(
            f"Ambiguous habit name '{ref}': matches "
            + ", ".join(f"{h.get('name')} ({h.get('id')})" for h in matches)
            + ". Use the id."
        )
    raise ToolError(
        f"Habit not found: '{ref}'. Use list_habits to see ids and names."
    )


def habit_view(habit: dict) -> dict:
    return {
        "id": habit.get("id"),
        "name": habit.get("name"),
        "colorHex": habit.get("colorHex"),
        "showInWidget": habit.get("showInWidget"),
        "targetDays": habit.get("targetDays"),
        "scheduleMode": habit.get("scheduleMode"),
        "timesPerWeek": habit.get("timesPerWeek"),
        "timesPerDay": habit.get("timesPerDay"),
        "dailyTarget": daily_target(habit),
        "sortOrder": habit.get("sortOrder"),
        "reminderEnabled": habit.get("reminderEnabled"),
        "reminderTime": habit.get("reminderTime"),
        "createdAt": habit.get("createdAt"),
        "isActive": habit.get("isActive"),
    }


def ordered_habits(snapshot: dict, include_inactive: bool = False) -> list[dict]:
    habits = snapshot["habits"]
    if not include_inactive:
        habits = [h for h in habits if h.get("isActive", True)]
    return sorted(
        habits,
        key=lambda h: (h.get("sortOrder") or 0,
                       str(h.get("name") or "").lower()),
    )


# ---------------------------------------------------------------------------
# Tools
# ---------------------------------------------------------------------------

def tool_list_habits(snapshot: dict, args: dict) -> dict:
    habits = ordered_habits(snapshot, bool(args.get("include_inactive", False)))
    return {"habits": [habit_view(h) for h in habits]}


def tool_get_today(snapshot: dict, args: dict) -> dict:
    date = parse_date(args.get("date"))
    date_key = date.isoformat()
    items = []
    for habit in ordered_habits(snapshot):
        if not is_scheduled(habit, date):
            continue
        count = count_for(snapshot, habit["id"], date_key)
        target = daily_target(habit)
        items.append({
            "id": habit.get("id"),
            "name": habit.get("name"),
            "count": count,
            "dailyTarget": target,
            "completed": count >= target,
            "scheduled": True,
        })
    return {"date": date_key, "habits": items}


def _adjust_count(snapshot: dict, args: dict, delta: int) -> dict:
    habit = find_habit(snapshot, str(args.get("habit", "")))
    date = parse_date(args.get("date"))
    date_key = date.isoformat()
    current = count_for(snapshot, habit["id"], date_key)
    # Mirror LogService.IncrementCountAsync / DecrementCountAsync: floor at 0.
    new_count = max(0, current + delta)
    set_count(snapshot, habit["id"], date_key, new_count)
    target = daily_target(habit)
    return {
        "id": habit.get("id"),
        "name": habit.get("name"),
        "date": date_key,
        "count": new_count,
        "dailyTarget": target,
        "completed": new_count >= target,
    }


def tool_log_completion(snapshot: dict, args: dict, data_file: str) -> dict:
    result = _adjust_count(snapshot, args, +1)
    save_snapshot(data_file, snapshot)
    return result


def tool_decrement_count(snapshot: dict, args: dict, data_file: str) -> dict:
    result = _adjust_count(snapshot, args, -1)
    save_snapshot(data_file, snapshot)
    return result


def tool_get_streak(snapshot: dict, args: dict) -> dict:
    habit = find_habit(snapshot, str(args.get("habit", "")))
    as_of = parse_date(args.get("as_of_date") or args.get("date"))
    target = daily_target(habit)
    streak = 0
    last_completed: str | None = None
    for offset in range(STREAK_LOOKBACK_DAYS):
        day = as_of - _dt.timedelta(days=offset)
        if not is_scheduled(habit, day):
            continue  # unscheduled days neither count nor break the streak
        count = count_for(snapshot, habit["id"], day.isoformat())
        if count >= target:
            streak += 1
            if last_completed is None:
                last_completed = day.isoformat()
        elif streak > 0:
            break  # first incomplete scheduled day ends the streak
        # else: days before the most recent completion don't end anything yet
    return {
        "id": habit.get("id"),
        "name": habit.get("name"),
        "asOf": as_of.isoformat(),
        "dailyTarget": target,
        "currentStreak": streak,
        "lastCompletedDate": last_completed,
    }


TOOLS = [
    {
        "name": "list_habits",
        "description": "List habits from the local OneTap-Habits data file, ordered by sort order then name.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "include_inactive": {
                    "type": "boolean",
                    "description": "Include habits with isActive=false (default false).",
                },
            },
        },
    },
    {
        "name": "get_today",
        "description": "Habits scheduled for a date (default today) with their count, daily target and completion state.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "date": {
                    "type": "string",
                    "description": "Date as yyyy-MM-dd (default today).",
                },
            },
        },
    },
    {
        "name": "log_completion",
        "description": "Increment a habit's count for a date by 1 (default today) and save the local data file. Mirrors tapping a habit card in the app.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "habit": {
                    "type": "string",
                    "description": "Habit id or exact name (see list_habits).",
                },
                "date": {
                    "type": "string",
                    "description": "Date as yyyy-MM-dd (default today).",
                },
            },
            "required": ["habit"],
        },
    },
    {
        "name": "decrement_count",
        "description": "Decrement a habit's count for a date by 1, floored at 0, and save the local data file. Mirrors the decrement gesture in the app.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "habit": {
                    "type": "string",
                    "description": "Habit id or exact name (see list_habits).",
                },
                "date": {
                    "type": "string",
                    "description": "Date as yyyy-MM-dd (default today).",
                },
            },
            "required": ["habit"],
        },
    },
    {
        "name": "get_streak",
        "description": "Current streak for a habit: consecutive scheduled days where the daily target was met, ending at the most recent completed scheduled day on or before the date (default today). Unscheduled days neither count nor break the streak.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "habit": {
                    "type": "string",
                    "description": "Habit id or exact name (see list_habits).",
                },
                "as_of_date": {
                    "type": "string",
                    "description": "Date as yyyy-MM-dd (default today).",
                },
            },
            "required": ["habit"],
        },
    },
]

READ_ONLY_TOOLS = {"list_habits", "get_today", "get_streak"}
WRITE_TOOLS = {"log_completion", "decrement_count"}


def dispatch(name: str, args: dict, data_file: str) -> dict:
    if name not in {t["name"] for t in TOOLS}:
        raise ToolError(f"Unknown tool: {name}")
    snapshot = load_snapshot(data_file)
    if name == "list_habits":
        return tool_list_habits(snapshot, args)
    if name == "get_today":
        return tool_get_today(snapshot, args)
    if name == "get_streak":
        return tool_get_streak(snapshot, args)
    if name == "log_completion":
        return tool_log_completion(snapshot, args, data_file)
    if name == "decrement_count":
        return tool_decrement_count(snapshot, args, data_file)
    raise ToolError(f"Unknown tool: {name}")


# ---------------------------------------------------------------------------
# MCP over stdio (newline-delimited JSON-RPC 2.0)
# ---------------------------------------------------------------------------

def handle_message(message: dict, data_file: str):
    """Return a response dict, or None for notifications (no reply)."""
    method = message.get("method")
    msg_id = message.get("id")
    params = message.get("params") or {}

    if method == "initialize":
        return {
            "jsonrpc": "2.0",
            "id": msg_id,
            "result": {
                "protocolVersion": PROTOCOL_VERSION,
                "capabilities": {"tools": {}},
                "serverInfo": {"name": SERVER_NAME, "version": SERVER_VERSION},
            },
        }
    if method == "ping":
        return {"jsonrpc": "2.0", "id": msg_id, "result": {}}
    if method == "tools/list":
        return {
            "jsonrpc": "2.0",
            "id": msg_id,
            "result": {"tools": copy.deepcopy(TOOLS)},
        }
    if method == "tools/call":
        name = params.get("name")
        args = params.get("arguments") or {}
        try:
            result = dispatch(name, args, data_file)
        except ToolError as e:
            return {
                "jsonrpc": "2.0",
                "id": msg_id,
                "result": {
                    "content": [{"type": "text", "text": str(e)}],
                    "isError": True,
                },
            }
        return {
            "jsonrpc": "2.0",
            "id": msg_id,
            "result": {
                "content": [{
                    "type": "text",
                    "text": json.dumps(result, ensure_ascii=False, indent=2),
                }],
            },
        }
    if method is not None and method.startswith("notifications/"):
        return None
    if msg_id is None:
        return None
    return {
        "jsonrpc": "2.0",
        "id": msg_id,
        "error": {"code": -32601, "message": f"Method not found: {method}"},
    }


def main() -> int:
    parser = argparse.ArgumentParser(
        description="OneTap-Habits connector — local-first MCP server (stdio)."
    )
    parser.add_argument(
        "--data-file",
        default=None,
        help="Path to guest_data.json (default: $ONETAP_HABITS_DATA_FILE or "
             "connector/data/guest_data.json).",
    )
    args = parser.parse_args()
    data_file = resolve_data_file(args.data_file)
    print(f"{SERVER_NAME} v{SERVER_VERSION} — data file: {data_file}",
          file=sys.stderr, flush=True)

    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            message = json.loads(line)
        except ValueError as e:
            print(f"Ignoring invalid JSON line: {e}", file=sys.stderr, flush=True)
            continue
        try:
            response = handle_message(message, data_file)
        except Exception as e:  # never kill the stdio loop on a bad request
            print(f"Error handling message: {e}", file=sys.stderr, flush=True)
            if message.get("id") is not None:
                response = {
                    "jsonrpc": "2.0",
                    "id": message.get("id"),
                    "error": {"code": -32603, "message": str(e)},
                }
            else:
                response = None
        if response is not None:
            sys.stdout.write(json.dumps(response, ensure_ascii=False) + "\n")
            sys.stdout.flush()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
