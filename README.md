# OneTap Habits

Minimal habit tracker for Android with a one-tap home screen widget. Built with **.NET 9 MAUI**, **Firebase Auth + Firestore** (Spark plan), distributed via **GitHub Releases**.

## Features (v1)

- **Guest mode** — use the app immediately; habits stored locally on device
- **Google Sign-In** (optional, Settings) — sync local habits to Firebase when ready
- Today's habit list with tap-to-complete
- Offline-first Firestore sync
- Bilingual UI (English / Spanish) + light/dark theme (System / Light / Dark)
- Android home screen widget — square grid of today's incomplete habits; one tap to complete offline
- Marketing site at [GitHub Pages](https://jon2g.github.io/OneTap-Habits/) (EN/ES)

## Requirements

- Android 7.0+ (API 24), target API 24+
- .NET 9 SDK + `maui-android` workload
- JDK 17
- Firebase project `onetap-habits`

## Local setup

### 1. Firebase config

```bash
# Copy example and fill from Firebase Console or:
# firebase apps:sdkconfig android --project onetap-habits
cp src/google-services.json.example src/google-services.json
```

`google-services.json` is gitignored. Never commit API keys.

#### Google Sign-In setup

1. Firebase Console → **Authentication → Sign-in method → Google → Enable**
2. Register **debug and release SHA-1** for `com.jon2g.onetaphabits`:

```bash
chmod +x scripts/get-android-sha1.sh
./scripts/get-android-sha1.sh
```

3. Re-download **google-services.json** from Project settings → Your apps → Android
4. Verify the file includes an `oauth_client` entry with `"client_type": 3` (Web client). An empty `oauth_client` array means SHA-1 was not registered yet — Google Sign-In will fail with a configuration error in Settings.

For CI/release builds, update the **`GOOGLE_SERVICES_JSON_BASE64`** GitHub secret after each config change:

```bash
base64 -i src/google-services.json | pbcopy
```

### 2. Build

```bash
cd src
dotnet build OneTap-Habits.sln -f net9.0-android -c Debug
```

### Rider

Open **`src/OneTap-Habits.sln`** (not the repo root). After reload, run configurations appear in the toolbar dropdown:

| Configuration | When to use |
|---------------|-------------|
| **OneTap Habits Android** | Native deploy to emulator/device (needs Android SDK) |
| **Android (dotnet CLI)** | Fallback — builds and runs via `dotnet -t:Run` |
| **OneTap Habits iOS** | iPhone Simulator |

**If Android does not appear in the list:**

1. **Plugins:** Settings → Plugins → install/enable **Android** (and restart Rider).
2. **SDK:** Settings → Build, Execution, Deployment → **Android** → set Android SDK path, or use **Tools → Android → Android SDK setup**.
3. **Reload:** File → Reload All Projects (or restart Rider).
4. **Use fallback:** select **Android (dotnet CLI)** — works without the Xamarin Android deploy UI (emulator must already be running).

CLI alternative:

```bash
./scripts/run-android.sh
# or
cd src && dotnet build OneTapHabits.csproj -f net9.0-android -t:Run
```

### 3. Deploy Firestore rules

```bash
cd firebase
npx -y firebase-tools@latest deploy --only firestore --project onetap-habits
```

## Home screen widget

1. Open the app and add habits (guest mode — no sign-in required).
2. Optional: **Settings → Account → Google** to sync habits to the cloud.
2. Long-press the home screen → **Widgets** → **OneTap Habits**.
3. The widget shows a **square grid** of today's **incomplete** habits only (up to 6 cells; "+N more" opens the app if needed).
4. **Tap a cell** to mark that habit complete — it disappears from the widget immediately (works offline; syncs when online).
5. Open the app to see the same completion state on the Today screen.

**Manual test (offline):** enable airplane mode → tap widget → reopen app → habit should show completed.

## Installation (users)

1. Open [Releases](https://github.com/Jon2G/OneTap-Habits/releases)
2. Download `OneTapHabits-vX.Y.Z.apk`
3. Enable install from unknown sources and install

## CI/CD

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `ci.yml` | PR / push `main` | Android Debug build + unit tests |
| `android-release.yml` | tag `v*` | Signed APK → GitHub Release |
| `firebase-deploy.yml` | push `main` (firebase files) | Deploy Firestore rules |
| `web-pages.yml` | push `main` (`src/Web/**`) | GitHub Pages showcase |

### GitHub secrets

| Secret | Purpose |
|--------|---------|
| `ANDROID_KEYSTORE_BASE64` | Release keystore |
| `ANDROID_SIGNING_PASSWORD` | Keystore password |
| `GOOGLE_SERVICES_JSON_BASE64` | CI build Firebase config |
| `FIREBASE_TOKEN` | Deploy Firestore rules from CI |

Generate keystore (once):

```bash
keytool -genkey -v -keystore src/onetaphabits.keystore -alias onetaphabits -keyalg RSA -keysize 2048 -validity 10000
```

Register **debug and release SHA-1** in Firebase Console, enable **Google** under Authentication → Sign-in method, then re-download `google-services.json` (must include `oauth_client` entries).

## Project layout

```text
repos/OneTap-Habits/
├── src/
│   ├── OneTap-Habits.sln
│   ├── .idea/runConfigurations/   # Rider shared run configs
│   └── OneTapHabits.csproj
├── scripts/run-android.sh
├── firebase/               # firebase.json, rules, indexes
└── .github/workflows/
.cursor/projects/OneTap-Habits/  # Spec Kit specs (workspace)
```

## Specs

Product and technical specs live under `.cursor/projects/OneTap-Habits/specs/` (Spec Kit).

## Release

See [RELEASE.md](RELEASE.md) for v1.0.0 checklist (keystore, GitHub secrets, tagging).

## License

MIT — see [LICENSE](LICENSE).
