#!/usr/bin/env bash
set -euo pipefail

ANDROID_SDK_DIR="${ANDROID_SDK_ROOT:-/home/vscode/android-sdk}"
PROJECT_FILE="/workspaces/Feuerwehr-KPP/src/Mobile/mobile.Android/de.openelp.feuerwehr.mobile.Android.csproj"

if [[ "$ANDROID_SDK_DIR" == "/usr/local/android" || ! -w "$(dirname "$ANDROID_SDK_DIR")" ]]; then
  ANDROID_SDK_DIR="/home/vscode/android-sdk"
fi

ANDROID_JAR="$ANDROID_SDK_DIR/platforms/android-36/android.jar"
mkdir -p "$ANDROID_SDK_DIR"

if [[ ! -f "$PROJECT_FILE" ]]; then
  echo "Android post-start: project file not found, skipping dependency check."
  exit 0
fi

if [[ -f "$ANDROID_JAR" ]]; then
  echo "Android post-start: API 36 already present at $ANDROID_SDK_DIR."
  exit 0
fi

echo "Android post-start: installing missing Android API 36 into $ANDROID_SDK_DIR ..."
dotnet build "$PROJECT_FILE" \
  -t:InstallAndroidDependencies \
  -f net10.0-android \
  -p:AndroidSdkDirectory="$ANDROID_SDK_DIR" \
  -p:AcceptAndroidSDKLicenses=true

echo "Android post-start: installation completed."
