#!/usr/bin/env python3
"""Smoke test for the OneTap-Habits connector (connector/server.py).

Spawns the MCP server over stdio and exercises every tool against a
temporary data file built from the repo's sample habits, with log dates
relative to today so the streak assertions are stable.

Run:  python3 connector/test_smoke.py
Exit code 0 = all checks passed.
"""

from __future__ import annotations

import datetime as _dt
import json
import os
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
SERVER = os.path.join(HERE, "server.py")
SAMPLE = os.path.join(HERE, "data", "sample_guest_data.json")

CHECKS = 0


def check(condition: bool, label: str, detail: str = "") -> None:
    global CHECKS
    CHECKS += 1
    status = "ok  " if condition else "FAIL"
    print(f"[{status}] {label}" + (f" — {detail}" if detail else ""))
    if not condition:
        raise SystemExit(f"smoke test failed at: {label}")


def build_fixture(path: str) -> None:
    """Sample habits + logs with dates relative to today."""
    with open(SAMPLE, "r", encoding="utf-8") as f:
        snapshot = json.load(f)
    today = _dt.date.today()

    def d(days_ago: int) -> str:
        return (today - _dt.timedelta(days=days_ago)).isoformat()

    # Water: target 8/day. Today 5 (incomplete), previous 3 days complete,
    # 4 days ago missing -> expected streak 3.
    # Read: target 1/day. Today + yesterday complete, 2 days ago missing
    # -> expected streak 2.
    # Gym: Mon/Wed/Fri, target 1. Complete on the last 3 scheduled days
    # before today -> expected streak 3.
    logs = [
        {"habitId": "habit-water", "date": d(0), "isCompleted": True, "count": 5},
        {"habitId": "habit-water", "date": d(1), "isCompleted": True, "count": 8},
        {"habitId": "habit-water", "date": d(2), "isCompleted": True, "count": 8},
        {"habitId": "habit-water", "date": d(3), "isCompleted": True, "count": 8},
        {"habitId": "habit-read", "date": d(0), "isCompleted": True, "count": 1},
        {"habitId": "habit-read", "date": d(1), "isCompleted": True, "count": 1},
    ]
    scheduled_back = []
    probe = today - _dt.timedelta(days=1)
    while len(scheduled_back) < 3:
        if (probe.weekday() + 1) % 7 in (1, 3, 5):  # Mon/Wed/Fri (.NET DayOfWeek)
            scheduled_back.append(probe)
        probe -= _dt.timedelta(days=1)
    for day in scheduled_back:
        logs.append({"habitId": "habit-gym", "date": day.isoformat(),
                     "isCompleted": True, "count": 1})
    snapshot["logs"] = logs
    with open(path, "w", encoding="utf-8") as f:
        json.dump(snapshot, f, ensure_ascii=False)


class McpClient:
    def __init__(self, data_file: str):
        self.proc = subprocess.Popen(
            [sys.executable, SERVER, "--data-file", data_file],
            stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL, text=True, bufsize=1,
        )
        self._next_id = 1

    def request(self, method: str, params: dict | None = None) -> dict:
        msg_id = self._next_id
        self._next_id += 1
        self.proc.stdin.write(json.dumps(
            {"jsonrpc": "2.0", "id": msg_id, "method": method,
             "params": params or {}}) + "\n")
        self.proc.stdin.flush()
        line = self.proc.stdout.readline()
        if not line:
            raise SystemExit("server closed stdout unexpectedly")
        return json.loads(line)

    def notify(self, method: str) -> None:
        self.proc.stdin.write(json.dumps(
            {"jsonrpc": "2.0", "method": method}) + "\n")
        self.proc.stdin.flush()

    def call(self, name: str, arguments: dict | None = None) -> dict:
        resp = self.request("tools/call", {
            "name": name, "arguments": arguments or {}})
        return resp.get("result", {})

    def call_ok(self, name: str, arguments: dict | None = None) -> dict:
        result = self.call(name, arguments)
        if result.get("isError"):
            text = result["content"][0]["text"]
            raise SystemExit(f"tool {name} errored: {text}")
        return json.loads(result["content"][0]["text"])

    def close(self) -> None:
        self.proc.stdin.close()
        self.proc.wait(timeout=10)


def main() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        data_file = os.path.join(tmp, "guest_data.json")
        build_fixture(data_file)

        client = McpClient(data_file)
        try:
            init = client.request("initialize", {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "smoke-test", "version": "0.1.0"},
            })
            check(init.get("result", {}).get("serverInfo", {}).get("name")
                  == "onetap-habits", "initialize returns server info")
            client.notify("notifications/initialized")

            tools = client.request("tools/list").get("result", {}).get("tools", [])
            names = {t["name"] for t in tools}
            check(names == {"list_habits", "get_today", "log_completion",
                            "decrement_count", "get_streak"},
                  "tools/list exposes the 5 tools", f"got {sorted(names)}")

            habits = client.call_ok("list_habits")["habits"]
            check([h["name"] for h in habits]
                  == ["Drink water", "Read 20 pages", "Gym"],
                  "list_habits returns 3 habits in sort order")
            check(habits[0]["dailyTarget"] == 8
                  and habits[1]["dailyTarget"] == 1,
                  "dailyTarget mirrors the app (SpecificDays -> TimesPerDay)")

            today = client.call_ok("get_today")
            by_name = {h["name"]: h for h in today["habits"]}
            check(by_name["Drink water"]["count"] == 5
                  and by_name["Drink water"]["completed"] is False,
                  "get_today: water 5/8, not completed")
            check(by_name["Read 20 pages"]["count"] == 1
                  and by_name["Read 20 pages"]["completed"] is True,
                  "get_today: read 1/1, completed")
            gym_today = _dt.date.today().weekday() in (0, 2, 4)  # Mon/Wed/Fri
            check(("Gym" in by_name) == gym_today,
                  "get_today: gym shown only on Mon/Wed/Fri",
                  f"today is {_dt.date.today().strftime('%A')}")

            water = client.call_ok("log_completion", {"habit": "Drink water"})
            check(water["count"] == 6 and water["completed"] is False,
                  "log_completion: water 5 -> 6")
            water = client.call_ok("log_completion", {"habit": "habit-water"})
            check(water["count"] == 7,
                  "log_completion: works by id too")
            client.call_ok("log_completion", {"habit": "Drink water"})
            water = client.call_ok("get_today")["habits"][0]
            check(water["count"] == 8 and water["completed"] is True,
                  "log_completion: water reaches target 8/8")

            water = client.call_ok("decrement_count", {"habit": "Drink water"})
            check(water["count"] == 7 and water["completed"] is False,
                  "decrement_count: water 8 -> 7")
            for _ in range(8):
                water = client.call_ok("decrement_count",
                                       {"habit": "Drink water"})
            check(water["count"] == 0,
                  "decrement_count: floors at 0")

            # Persistence: the file on disk must reflect the final counts.
            with open(data_file, "r", encoding="utf-8") as f:
                on_disk = json.load(f)
            water_today = [e for e in on_disk["logs"]
                           if e["habitId"] == "habit-water"
                           and e["date"] == _dt.date.today().isoformat()]
            check(water_today == [],
                  "count 0 removes the log entry (mirrors the app)")
            check(os.path.exists(data_file + ".bak"),
                  "writes keep a .bak of the previous version")

            streak = client.call_ok("get_streak", {"habit": "Read 20 pages"})
            check(streak["currentStreak"] == 2,
                  "get_streak: read = 2 (today + yesterday)",
                  f"got {streak['currentStreak']}")
            streak = client.call_ok("get_streak", {"habit": "Drink water"})
            check(streak["currentStreak"] == 3,
                  "get_streak: water = 3 (today now 0, prev 3 days complete)",
                  f"got {streak['currentStreak']}")
            streak = client.call_ok("get_streak", {"habit": "Gym"})
            check(streak["currentStreak"] == 3,
                  "get_streak: gym = 3 scheduled days",
                  f"got {streak['currentStreak']}")

            err = client.call("log_completion", {"habit": "No such habit"})
            check(err.get("isError") is True,
                  "unknown habit returns a tool error, not a crash")
            err = client.call("get_today", {"date": "not-a-date"})
            check(err.get("isError") is True,
                  "invalid date returns a tool error")
        finally:
            client.close()

        # Missing data file -> clear error, not an empty result.
        missing = McpClient(os.path.join(tmp, "does-not-exist.json"))
        try:
            missing.request("initialize", {})
            missing.notify("notifications/initialized")
            err = missing.call("list_habits")
            check(err.get("isError") is True,
                  "missing data file returns a tool error")
        finally:
            missing.close()

    print(f"\nAll {CHECKS} smoke checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
