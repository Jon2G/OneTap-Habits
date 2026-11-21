# Free-tier Firebase plan — OneTap-Habits

> Status: **Plan only — no code changes in this commit.**
> Meant to be handed to Cursor (or any agent) for implementation.
> Repo: `Jon2G/OneTap-Habits` · branch: `main` · HEAD at time of writing: `d0cc15f`
> Owner constraint: **single user (Jonathan). Everything must stay on free tiers. $0/month target.**

## 1. Goal

Make OneTap-Habits provably free to run forever for one user:

1. Confirm the Firebase project `onetap-habits` is on the **Spark (free) plan** and stays there.
2. Audit every cloud call the app makes and prove it fits inside Spark quotas with huge headroom.
3. Add guardrails (rules, budgets, alerts, kill-switch) so a bug can never silently push the project into billable usage.
4. Keep CI/CD and hosting on free tiers too (GitHub Actions on a public repo, GitHub Pages).
5. Design the future Muse connector so it costs $0 (local-first, no new cloud service).

## 2. Current state (verified from the repo, 2026-11-20)

- **Stack:** .NET 9 MAUI (`src/OneTapHabits.csproj`), Android (+ iOS/Windows targets), distributed via GitHub Releases.
- **Firebase already in use:** `Plugin.Firebase.Auth`, `Plugin.Firebase.Core`, `Plugin.Firebase.Crashlytics`, `Plugin.Firebase.Firestore` — all v4.0.0. README already says “Firebase Auth + Firestore (Spark plan)”.
- **Project:** `onetap-habits` (`firebase/.firebaserc`, `src/google-services.json.example`). Secrets via `GOOGLE_SERVICES_JSON_BASE64` GitHub secret; real `google-services.json` is gitignored — **never commit it**.
- **Data model:** per-user subtree `users/{userId}/{document=**}` (`firebase/firestore.rules`); offline-first sync; guest mode works with local-only storage; Google Sign-In is optional (Settings → Account).
- **No Cloud Functions** in the repo (code search: 0 hits). No paid extensions. Analytics is disabled in the iOS example plist (`IS_ANALYTICS_ENABLED = false`).
- **CI:** `ci.yml`, `android-release.yml`, `windows-release.yml`, `firebase-deploy.yml` (deploys Firestore rules on push to `main`), `web-pages.yml` (GitHub Pages showcase in `src/Web/`).
- **Local services of interest:** `src/Services/Firestore/` (`HabitFirestoreDto`, `LogFirestoreDto`, `FirestoreCloudRepair`, `CloudDocumentSanitizer` in `src/OneTapHabits.Core/Firestore/`), `src/Platforms/Android/Services/AndroidGoogleSignInService.cs`.
- **No `.meta/` folder exists yet** — this file creates it.

So this is **not a migration to Firebase**. It is a *lock-in and prove-it* task: the app is already on Spark; the work is to verify, trim, and guard it.

## 3. Free-tier budget (verify in the Firebase console — numbers below are the documented Spark quotas)

| Service | Spark (free) quota | Expected single-user usage | Headroom |
|---|---|---|---|
| Firestore reads | 50,000 / day | A few hundred / day (today screen + widget) | ~100× |
| Firestore writes | 20,000 / day | < 100 / day (habit taps) | ~200× |
| Firestore deletes | 20,000 / day | ~0 / day | — |
| Firestore storage | 1 GiB | A few MB (habits + logs) | ~100× |
| Firebase Auth (Google provider) | Free | 1 user | — |
| Crashlytics | Free | 1 device | — |
| Cloud Messaging (FCM) | Free | Not used today | — |
| Cloud Functions | None used | **Must stay 0** — do not add | — |
| Cloud Storage | Not used for data (only referenced in iOS plist) | **Must stay 0** | — |
| GitHub Actions (public repo) | Free for public repos | CI + release builds | — |
| GitHub Pages | Free | Showcase site | — |

**Rule for every future change:** if a feature needs a service that is not free on Spark, it is out of scope unless it can be done locally on-device instead.

## 4. Work items

### P0 — Confirm and lock the plan

- [ ] Open Firebase Console → Project settings → Usage and billing: confirm plan is **Spark**. Screenshot/note the result in `firebase/README.md` (new file).
- [ ] In Google Cloud Console → Billing → Budgets & alerts: create a **$0 budget alert** for project `onetap-habits` (alert at 50% / 90% / 100% of $0 — i.e. any billable usage emails Jonathan). Document the steps in `firebase/README.md`.
- [ ] Confirm **no billing account is attached** to the project (Spark needs none). If one is attached, detach it — do not just trust the plan label.
- [ ] Record the Firebase console usage page as the source of truth; add a monthly manual check (first of month) to the README.

### P0 — Audit every cloud call

- [ ] Grep the whole `src/` tree for Firebase/HTTP usage (`Plugin.Firebase.*`, `HttpClient`, `RestSharp`, `FirebaseAdmin`, `Storage`, `Functions`, `RemoteConfig`, `Analytics`). Produce a table: call site → service → trigger → estimated calls/day for 1 user.
- [ ] Verify **zero** Cloud Storage writes/reads and **zero** Cloud Functions invocations. The iOS `GoogleService-Info.plist.example` references `onetap-habits.firebasestorage.app` — confirm it is only a config placeholder and no code touches Storage; if it is dead config, note it.
- [ ] Verify Crashlytics is the only telemetry and that Analytics stays off on all platforms (check Android `google-services.json.example` equivalents, not just iOS).
- [ ] Verify the widget (`src/OneTapHabits.WidgetHost/`) performs **no network calls** — it must read local state only.

### P1 — Make sync cheap by design (single-user math)

- [ ] **Debounce + batch:** habit taps already write locally first; ensure cloud sync batches pending changes (one write per habit/day, not one per tap burst) and retries on a backoff, not a tight loop. Cap retries (e.g. 5 attempts, then stop and surface “sync paused”).
- [ ] **No polling listeners:** use a single snapshot listener only while the Today screen is open, or prefer explicit pull-on-open. No background listener while the app is closed.
- [ ] **Minimal reads:** Today screen reads only today’s habit docs (query by date, not the whole collection). Widget reads local DB only.
- [ ] **Minimal indexes:** audit `firebase/firestore.indexes.json` — delete any composite index no query uses (each index costs writes). Keep the file minimal and document which query needs each index.
- [ ] **Debug usage counter (debug builds only):** log Firestore reads/writes per session to the debug log so a regression is visible during development. Never ship the counter to production analytics.
- [ ] **Size caps:** enforce a max habits-per-user (e.g. 100) and max log entries per habit/day in the sync layer so a bug cannot create unbounded documents.

### P1 — Rules hardening

- [ ] Keep `firebase/firestore.rules` as `users/{userId}/{document=**}` with `request.auth.uid == userId`. No public reads.
- [ ] Add **schema validation** to the rules: allow only known fields (`HabitFirestoreDto` / `LogFirestoreDto` shape), correct types, and string/number size caps — a bad client build must not be able to write garbage that inflates storage.
- [ ] Test rules with the Firebase Emulator Suite (`firebase emulators:start --only firestore`) — add a small rules test script under `firebase/` and document it. Deploy only after tests pass; CI (`firebase-deploy.yml`) already deploys rules on push to `main`.
- [ ] Confirm `FirestoreCloudRepair` and `CloudDocumentSanitizer` cannot trigger write storms (bounded batch sizes, single run per sign-in, not per app start).

### P1 — Auth stays free

- [ ] Google provider only. **Do not enable phone auth** (SMS is billable beyond 10 free verifications/day) or email-link auth that needs extra services.
- [ ] Keep guest mode fully working with zero cloud calls — signing in must remain optional.
- [ ] Document the SHA-1 setup (already in README) so a rebuild never silently breaks sign-in and falls back to repeated auth retries.

### P2 — CI/CD stays free

- [ ] Keep the repo **public** (Actions free for public repos). If it ever goes private, re-check the 2,000 min/month Actions budget.
- [ ] Prefer Linux/Windows runners; **avoid macOS runners** (10× minute multiplier) unless an iOS build is genuinely needed.
- [ ] Keep NuGet caching on in `ci.yml` to cut build minutes. Keep the `firebase-deploy.yml` trigger scoped to `firebase/**` changes only (already the case — verify).

### P2 — Muse connector (follow-up, design only in this plan)

Context: Jonathan wants Muse to talk to OneTap-Habits (see the “Add a connector for OneTap-Habits” goal). The connector must also cost $0:

- [ ] **Local-first design:** a small read-only MCP/HTTP bridge that runs on Jonathan’s own machine and reads the app’s local data (local SQLite/export file) — no new cloud service, no hosted endpoint, no Firebase Admin SDK in the cloud.
- [ ] Define the data contract first: which local file/table holds habits and completions, and a JSON export format. Document it in `.meta/plans/connector-onetap-habits.md` (separate plan file).
- [ ] **Do not** put a service account key or any credential in the repo. Any credential lives in Muse’s secure connection flow, never in code.
- [ ] Explicit non-goal for this phase: no cloud-hosted API, no new Firebase project, no Functions.

### P3 — Kill-switch and fallback

- [ ] App must keep working fully offline if Firebase is unreachable or the project is disabled (guest/local mode already exists — verify the fallback path has no retry loop that hammers the network).
- [ ] Add a Settings toggle “Cloud sync on/off” if not present; off = zero Firebase calls.
- [ ] Document the manual fallback: export local data → re-import. If export does not exist yet, file it as a separate idea (do not build it in this task).

## 5. Done When (acceptance criteria)

- [ ] Firebase console confirms Spark plan, no billing account attached, $0 budget alert created.
- [ ] Audit table of all cloud calls exists in `firebase/README.md`; zero Functions / zero Storage / zero Analytics calls.
- [ ] Firestore rules validate schema + size caps and pass emulator tests.
- [ ] `firestore.indexes.json` contains only indexes a real query uses.
- [ ] Sync path is batched/debounced with capped retries; no background listeners; widget is offline-only.
- [ ] `dotnet build OneTap-Habits.sln -f net9.0-android -c Debug` passes; existing unit tests pass; `firebase deploy --only firestore --project onetap-habits` succeeds (rules only).
- [ ] README updated: “Free tier” section stating Spark, the $0 target, and where the budget alert lives.
- [ ] No secrets committed (`google-services.json`, keystores, tokens stay gitignored / in GitHub secrets).

## 6. Risks / decisions

- **Single-user assumption is load-bearing.** All headroom math assumes 1 user. If the app ever gets public users, revisit quotas before enabling sign-up for others (Auth + Firestore scale, but the $0 guarantee does not).
- **Spark quotas reset daily** — a bug that writes in a loop can burn a day’s quota in minutes; the retry caps and rules validation are the mitigation, not the budget alert (alerts are not real-time).
- **No SLA on Spark.** Acceptable for a personal app.
- **Plugin.Firebase 4.0.0** is the current dependency — do not upgrade Firebase packages in this task; that is a separate change.

## 7. Key file references

- `src/OneTapHabits.csproj` — Firebase packages (Auth/Core/Crashlytics/Firestore 4.0.0)
- `src/Services/Firestore/` — `HabitFirestoreDto.cs`, `LogFirestoreDto.cs`, `FirestoreCloudRepair.cs`
- `src/OneTapHabits.Core/Firestore/CloudDocumentSanitizer.cs`
- `src/Platforms/Android/Services/AndroidGoogleSignInService.cs`
- `src/OneTapHabits.WidgetHost/` — widget (must stay offline-only)
- `firebase/firebase.json`, `firebase/firestore.rules`, `firebase/firestore.indexes.json`, `firebase/.firebaserc`
- `.github/workflows/firebase-deploy.yml`, `ci.yml`, `android-release.yml`
- `src/google-services.json.example` (config template — real file is gitignored)

## 8. Out of scope

- Fixing the double-tap-to-decrease counter bug (separate todo).
- Any UI redesign, new habit features, or new platforms.
- Upgrading .NET / MAUI / Plugin.Firebase versions.
- Building the Muse connector (separate plan: `.meta/plans/connector-onetap-habits.md`).
- Migrating to another backend (Supabase, Appwrite, etc.) — Firebase Spark already satisfies the $0 requirement; do not propose a migration without new evidence.
