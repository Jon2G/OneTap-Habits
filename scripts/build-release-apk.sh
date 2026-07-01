#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE_ROOT="$(cd "$ROOT/../.." && pwd)"
SECRETS_ENV="$WORKSPACE_ROOT/.cursor/projects/OneTap-Habits/secrets/android-signing.env"
OUT_DIR="$ROOT/src/bin/Release/net9.0-android"

if [[ ! -f "$SECRETS_ENV" ]]; then
  echo "Missing signing env: $SECRETS_ENV" >&2
  echo "Create it with ANDROID_SIGNING_PASSWORD and copy onetaphabits.keystore to src/." >&2
  exit 1
fi

# shellcheck disable=SC1090
source "$SECRETS_ENV"
export ANDROID_SIGNING_PASSWORD

if [[ ! -f "$ROOT/src/onetaphabits.keystore" ]]; then
  echo "Missing keystore: $ROOT/src/onetaphabits.keystore" >&2
  exit 1
fi

dotnet publish "$ROOT/src/OneTapHabits.csproj" \
  -f net9.0-android \
  -c Release \
  -o "$OUT_DIR" \
  "$@"

APK="$OUT_DIR/com.jon2g.onetaphabits-Signed.apk"
if [[ -f "$APK" ]]; then
  echo "Release APK: $APK"
else
  echo "Signed APK not found under $OUT_DIR" >&2
  exit 1
fi
