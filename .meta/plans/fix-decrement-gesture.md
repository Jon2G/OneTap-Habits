# Fix decrement gesture — OneTap-Habits

> Status: **Plan only — no code changes in this commit.**
> Meant to be handed to Cursor (or any agent) for implementation.
> Repo: `Jon2G/OneTap-Habits` · branch: `meta/plans` · HEAD at time of writing: `5c92ed66`
> Issue: [#4 — Double tap to decrease counter no longer works](https://github.com/Jon2G/OneTap-Habits/issues/4)
> Reported by Jonathan, 2026-11-20. Also tracked as a todo/goal ("Fix double tap to decrease counter in OneTap-Habits").

## 1. Goal

Make the decrement gesture on the Today screen work again: a habit card’s counter goes down by 1 (floored at 0) when the user performs the intended gesture, without breaking tap-to-increment.

## 2. Current state (verified from the repo, 2026-11-20)

- **The decrement gesture today is a long-press, not a double-tap.** `src/Views/TodayPage.xaml` wires the habit card (`Border`) with:
  - `TapGestureRecognizer` → `IncrementCommand` (single tap increases the counter)
  - `behaviors:LongPressBehavior` → `DecrementCommand` (long-press decreases it)
- `src/Behaviors/LongPressBehavior.cs` implements long-press with a `PointerGestureRecognizer` (`PointerPressed` / `PointerReleased` / `PointerExited`) + a 500 ms `Task.Delay` before firing the command.
- **No double-tap gesture exists anywhere in the code** — code search for `DoubleTap` and `NumberOfTapsRequired` returns 0 hits.
- Decrement command chain: `TodayViewModel.DecrementHabitAsync` (`src/ViewModels/TodayViewModel.cs`) → `LogService.DecrementCountAsync` (`src/Services/LogService.cs`) → `ILocalCloudStore.DecrementCountAsync` / guest store. Counter floors at 0 (`CloudCachePersistence.DecrementCount`).
- The Android widget has its own decrement path (tap the `x/n` progress → `ActionDecrementHabit` → `WidgetCompletionService.DecrementHabitAsync`) — **not** part of this bug unless the investigation shows otherwise.
- Decrement logic is already unit-tested (`tests/OneTapHabits.Tests/CloudCachePersistenceTests.DecrementCount_FloorsAtZero`), so the bug is most likely in the **gesture layer**, not the counter logic.

## 3. First decision (do this before writing code)

Decide the intended gesture — the report says “double tap”, the code says “long-press”:

- **Option A — restore long-press:** keep the current UX (long-press = decrement), fix whatever broke `LongPressBehavior`.
- **Option B — implement real double-tap:** add a `TapGestureRecognizer` with `NumberOfTapsRequired="2"` bound to `DecrementCommand`, and remove or keep long-press as a secondary gesture.
- Ask Jonathan if unsure; default to **Option A** (restore what the code documents) unless he confirms double-tap is what he wants. Record the decision in the issue.

## 4. Investigation

- [ ] Reproduce on a real Android device / emulator: build debug, tap a habit to increment, then long-press (and double-tap) the card. Note exactly which gesture fails.
- [ ] Check whether `PointerGestureRecognizer` events fire at all for touch on the current MAUI version — pointer gestures are primarily mouse/pen and are known to be unreliable for touch on Android. Add temporary debug logging in `LongPressBehavior.OnPointerPressed/OnPointerReleased` to confirm.
- [ ] Check for gesture conflicts: the same `Border` has both a tap recognizer (increment) and the pointer recognizer. Verify a long-press does not also fire the tap increment on release (or vice versa).
- [ ] Check `git log` for recent changes to `TodayPage.xaml`, `LongPressBehavior.cs`, and the MAUI / `Plugin.Firebase` package versions — identify what changed when the gesture last worked.
- [ ] Confirm the decrement path below the gesture still works (call `DecrementCommand` from a temporary debug button, or run the existing unit tests).

## 5. Work items

- [ ] Implement the gesture decided in §3:
  - **Option A:** fix `LongPressBehavior` (e.g. replace the `PointerGestureRecognizer` approach with a platform-reliable long-press, or fix the event wiring / cancellation). Keep the 500 ms threshold unless the repro shows it should change.
  - **Option B:** add double-tap recognizer; make sure single-tap increment and double-tap decrement do not both fire (MAUI fires the single-tap recognizer on double-tap unless handled — test this explicitly).
- [ ] Make sure the gesture works in both guest mode and signed-in mode (the command chain differs: `LocalGuestStore` vs cloud store).
- [ ] Make sure the widget decrement still works (regression check only — do not change widget code unless the investigation implicates it).
- [ ] Add a UI/integration test if the repo has a harness for it; at minimum add a unit test that `DecrementHabitAsync` decreases the count and floors at 0 (check whether one already exists — do not duplicate `DecrementCount_FloorsAtZero`).
- [ ] Update the in-app hint / docs if the gesture changes (e.g. if double-tap replaces long-press, update any text that says “long-press to decrease”).
- [ ] Verify the fix on a real device, not just the emulator, in both light and dark theme.

## 6. Done When

- [ ] On the Today screen, the intended gesture decreases the counter by 1 per gesture, floored at 0.
- [ ] Single tap still increments by exactly 1 and does not also decrement.
- [ ] Decrement works in guest mode and signed-in mode; the Today screen and the widget show the same count afterwards.
- [ ] Existing tests pass (`dotnet test`), debug build succeeds (`dotnet build OneTap-Habits.sln -f net9.0-android -c Debug`).
- [ ] Issue #4 closed with a comment stating which gesture was chosen (long-press vs double-tap) and the device it was verified on.

## 7. Key file references

- `src/Views/TodayPage.xaml` — gesture wiring on the habit card
- `src/Behaviors/LongPressBehavior.cs` — long-press implementation (500 ms, `PointerGestureRecognizer`)
- `src/ViewModels/TodayViewModel.cs` — `IncrementHabitAsync` / `DecrementHabitAsync`
- `src/Services/LogService.cs` — `DecrementCountAsync`
- `src/Services/ILogService.cs`, `src/Services/ILocalCloudStore.cs`, `src/Services/LocalCloudStore.cs`, `src/Services/LocalGuestStore.cs`
- `src/OneTapHabits.Core/Storage/CloudCachePersistence.cs` — `DecrementCount` (floors at 0)
- `tests/OneTapHabits.Tests/CloudCachePersistenceTests.cs`
- Widget (regression check only): `src/Platforms/Android/Widget/WidgetTapReceiver.cs`, `src/Platforms/Android/Services/WidgetCompletionService.cs`

## 8. Out of scope

- Any other Today screen behavior (swipe edit/delete, reorder, refresh).
- The free-tier Firebase work (`.meta/plans/free-tier-firebase.md`).
- The Muse connector (`.meta/plans/connector-onetap-habits.md`, not written yet).
- Redesigning the habit card UI.
