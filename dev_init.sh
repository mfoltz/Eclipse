#!/usr/bin/env bash
set -e
BEPINEX_PLUGIN_DIR="/path/to/BepInEx/plugins"

if ! command -v dotnet >/dev/null; then
    echo ".NET SDK is required. Run .codex/install.sh first." >&2
    exit 1
fi

PROJECT="$(dirname "$0")/Eclipse.csproj"
dotnet build --no-restore "$PROJECT"

DLL_PATH="$(dirname "$0")/bin/Release/net6.0/Eclipse.dll"

if [ ! -f "$DLL_PATH" ]; then
    echo "Build failed: $DLL_PATH not found." >&2
    exit 1
fi

cp "$DLL_PATH" "$BEPINEX_PLUGIN_DIR"
echo "Copied $(basename "$DLL_PATH") to $BEPINEX_PLUGIN_DIR"

