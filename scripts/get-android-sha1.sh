#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE_ROOT="$(cd "$ROOT/../.." && pwd)"
SECRETS_ENV="$WORKSPACE_ROOT/.cursor/projects/OneTap-Habits/secrets/android-signing.env"
RELEASE_KEYSTORE="$ROOT/src/onetaphabits.keystore"
ANDROID_SDK_DEBUG_KEYSTORE="${HOME}/.android/debug.keystore"
MAUI_DEBUG_KEYSTORE="${HOME}/Library/Application Support/Xamarin/Mono for Android/debug.keystore"
GOOGLE_SERVICES_JSON="$ROOT/src/google-services.json"

normalize_sha1() {
  echo "$1" | tr '[:lower:]' '[:upper:]' | tr -d ':'
}

read_sha1() {
  if "$@" 2>/dev/null | awk '/SHA1:/ { print $2; exit }'; then
    :
  else
    echo ""
  fi
}

echo "OneTap Habits — Android SHA-1 fingerprints for Firebase Console"
echo "Project: onetap-habits | Package: com.jon2g.onetaphabits"
echo ""
echo "Add every SHA-1 printed below under Firebase → Project settings → Your apps → Android."
echo "Then enable Google under Authentication → Sign-in method and re-download google-services.json."
echo ""

MAUI_DEBUG_SHA="$(read_sha1 keytool -list -v -alias androiddebugkey -keystore "$MAUI_DEBUG_KEYSTORE" -storepass android -keypass android)"
ANDROID_SDK_DEBUG_SHA="$(read_sha1 keytool -list -v -alias androiddebugkey -keystore "$ANDROID_SDK_DEBUG_KEYSTORE" -storepass android -keypass android)"

echo "=== Debug — .NET MAUI / Xamarin (typical Rider & dotnet build -c Debug) ==="
echo "Keystore: $MAUI_DEBUG_KEYSTORE"
if [[ -n "$MAUI_DEBUG_SHA" ]]; then
  echo "$MAUI_DEBUG_SHA"
else
  echo "(could not read — check keystore path and password)"
fi
echo ""

echo "=== Debug — Android SDK (~/.android/debug.keystore) ==="
echo "Keystore: $ANDROID_SDK_DEBUG_KEYSTORE"
if [[ -n "$ANDROID_SDK_DEBUG_SHA" ]]; then
  echo "$ANDROID_SDK_DEBUG_SHA"
else
  echo "(could not read — check keystore path and password)"
fi
echo ""

RELEASE_SHA=""
if [[ -f "$RELEASE_KEYSTORE" ]]; then
  if [[ -f "$SECRETS_ENV" ]]; then
    # shellcheck disable=SC1090
    source "$SECRETS_ENV"
  fi
  if [[ -n "${ANDROID_SIGNING_PASSWORD:-}" ]]; then
    RELEASE_SHA="$(read_sha1 keytool -list -v -alias onetaphabits -keystore "$RELEASE_KEYSTORE" \
      -storepass "$ANDROID_SIGNING_PASSWORD" -keypass "$ANDROID_SIGNING_PASSWORD")"
    echo "=== Release (onetaphabits) ==="
    if [[ -n "$RELEASE_SHA" ]]; then
      echo "$RELEASE_SHA"
    else
      echo "(could not read release keystore)"
    fi
    echo ""
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

if [[ -f "$GOOGLE_SERVICES_JSON" ]]; then
  if grep -q '"client_type": 3' "$GOOGLE_SERVICES_JSON" 2>/dev/null; then
    echo "google-services.json: oauth_client type 3 (Web client) present — OK for Google Sign-In."
  else
    echo "google-services.json: missing oauth_client type 3 — re-download after registering SHA-1."
  fi

  ANDROID_OAUTH_COUNT="$(grep -c '"client_type": 1' "$GOOGLE_SERVICES_JSON" 2>/dev/null || true)"
  echo "google-services.json: android oauth_client entries (type 1): ${ANDROID_OAUTH_COUNT:-0}"

  echo ""
  echo "=== google-services.json SHA-1 coverage ==="
  JSON_HASHES="$(grep -o '"certificate_hash": "[^"]*"' "$GOOGLE_SERVICES_JSON" | sed 's/"certificate_hash": "//;s/"$//' || true)"

  check_hash() {
    local label="$1"
    local sha="$2"
    if [[ -z "$sha" ]]; then
      echo "$label: (keystore SHA unavailable — skip)"
      return
    fi

    local normalized
    normalized="$(normalize_sha1 "$sha")"
    if echo "$JSON_HASHES" | grep -qi "$normalized"; then
      echo "$label: OK (registered in google-services.json)"
    else
      echo "$label: MISSING from google-services.json — add SHA in Firebase and re-download"
    fi
  }

  check_hash "MAUI debug" "$MAUI_DEBUG_SHA"
  check_hash "Android SDK debug" "$ANDROID_SDK_DEBUG_SHA"
  check_hash "Release" "$RELEASE_SHA"
else
  echo "google-services.json not found — copy from google-services.json.example after Firebase setup."
fi
