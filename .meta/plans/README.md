# Plans

Implementation plans for OneTap-Habits, written to be handed to Cursor (or any agent). Plan files only — no code changes live here.

| Plan | What it covers | Status |
|---|---|---|
| [free-tier-firebase.md](free-tier-firebase.md) | Keep the app on free tiers forever (Firebase Spark, GitHub Actions, Pages) for a single user; audit cloud calls, harden Firestore rules, add $0 budget alert and kill-switch | Planned |
| [fix-decrement-gesture.md](fix-decrement-gesture.md) | Fix the decrement gesture on the Today screen (issue #4) — long-press vs double-tap decision, gesture-layer investigation | Planned |
| connector-onetap-habits.md | Muse connector for OneTap-Habits (local-first, $0) | Not written yet |

## Conventions

- One plan per file, named by topic in kebab-case.
- Each plan states its goal, verified current state, work items with checkboxes, Done When criteria, and out-of-scope items.
- Plans reference real repo paths at the HEAD they were written against.
- Never commit secrets in plans (no API keys, no `google-services.json`, no keystores).
