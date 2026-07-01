# Release v1.0.0 checklist

Complete these steps before tagging `v1.0.0`:

## GitHub secrets

Configure in **Settings → Secrets and variables → Actions**:

| Secret | Purpose |
|--------|---------|
| `ANDROID_KEYSTORE_BASE64` | Base64-encoded release keystore |
| `ANDROID_SIGNING_PASSWORD` | Keystore + key password |
| `GOOGLE_SERVICES_JSON_BASE64` | CI Firebase Android config |
| `FIREBASE_TOKEN` or `FIREBASE_SERVICE_ACCOUNT` | Deploy Firestore rules |

## Keystore (one-time, local)

Canonical copy: **`.cursor/projects/OneTap-Habits/secrets/`** (workspace, gitignored via `secrets/.gitignore`):

- `onetaphabits.keystore`
- `android-signing.env` (`ANDROID_SIGNING_PASSWORD=...`)

Copy keystore to `src/onetaphabits.keystore` for local builds. GitHub Actions secrets **`ANDROID_KEYSTORE_BASE64`** and **`ANDROID_SIGNING_PASSWORD`** must match this keystore.

```bash
# Regenerate (only if rotating keys — updates Firebase SHA-1 requirement)
keytool -genkey -v -keystore src/onetaphabits.keystore -alias onetaphabits -keyalg RSA -keysize 2048 -validity 10000
base64 -i src/onetaphabits.keystore | pbcopy   # → ANDROID_KEYSTORE_BASE64
```

Local release build:

```bash
./scripts/build-release-apk.sh
```

Register **debug and release SHA-1** in Firebase Console for Google Sign-In (`./scripts/get-android-sha1.sh`).

## GitHub Pages

1. Repo **Settings → Pages → Build and deployment → GitHub Actions**
2. Push to `main` with changes under `src/Web/` to deploy showcase

## Tag release

```bash
git tag v1.0.0
git push origin v1.0.0
```

`android-release.yml` publishes `OneTapHabits-v1.0.0.apk` to GitHub Releases.

## Post-release

- Verify Crashlytics in Firebase Console (optional test crash)
- Update marketing site CTA if needed
