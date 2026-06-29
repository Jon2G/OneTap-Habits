#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet build OneTapHabits.csproj -f net9.0-android -c Debug -t:Run "$@"
