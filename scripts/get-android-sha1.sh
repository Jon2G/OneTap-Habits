#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE_ROOT="$(cd "$ROOT/../.." && pwd)"
SECRETS_ENV="$WORKSPACE_ROOT/.cursor/projects/OneTap-Habits/secrets/android-signing.env"
RELEASE_KEYSTORE="$ROOT/src/onetaphabits.keystore"
DEBUG_KEYSTORE="${HOME}/.android/debug.keystore"

echo "OneTap Habits — Android SHA-1 fingerprints for Firebase Console"
echo "Project: onetap-habits | Package: com.jon2g.onetaphabits"
echo ""
echo "Add both SHA-1 values under Firebase → Project settings → Your apps → Android."
echo "Then enable Google under Authentication → Sign-in method and re-download google-services.json."
echo ""

print_sha1() {
  local label="$1"
  shift
  echo "=== $label ==="
  if "$@" 2>/dev/null | awk '/SHA1:/ { print $2; exit }'; then
    :
  else
    echo "(could not read — check keystore path and password)"
  fi
  echo ""
}

print_sha1 "Debug (androiddebugkey)" \
  keytool -list -v -alias androiddebugkey -keystore "$DEBUG_KEYSTORE" -storepass android -keypass android

if [[ -f "$RELEASE_KEYSTORE" ]]; then
  if [[ -f "$SECRETS_ENV" ]]; then
    # shellcheck disable=SC1090
    source "$SECRETS_ENV"
  fi
  if [[ -n "${ANDROID_SIGNING_PASSWORD:-}" ]]; then
    print_sha1 "Release (onetaphabits)" \
      keytool -list -v -alias onetaphabits -keystore "$RELEASE_KEYSTORE" \
        -storepass "$ANDROID_SIGNING_PASSWORD" -keypass "$ANDROID_SIGNING_PASSWORD"
  else
    echo "=== Release (onetaphabits) ==="
    echo "Keystore: $RELEASE_KEYSTORE"
    echo "Set ANDROID_SIGNING_PASSWORD in:"
    echo "  $SECRETS_ENV"
    echo "Or run: keytool -list -v -alias onetaphabits -keystore \"$RELEASE_KEYSTORE\""
    echo ""
  fi
else
  echo "=== Release keystore not found ==="
  echo "Expected: $RELEASE_KEYSTORE"
  echo "Generate with README / RELEASE.md instructions, then re-run this script."
  echo ""
fi

if [[ -f "$ROOT/src/google-services.json" ]]; then
  if grep -q '"client_type": 3' "$ROOT/src/google-services.json" 2>/dev/null; then
    echo "google-services.json: oauth_client type 3 (Web client) present — OK for Google Sign-In."
  else
    echo "google-services.json: missing oauth_client type 3 — re-download after registering SHA-1."
  fi
else
  echo "google-services.json not found — copy from google-services.json.example after Firebase setup."
fi
