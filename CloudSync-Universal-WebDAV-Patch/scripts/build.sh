#!/usr/bin/env bash
# ============================================================
#  CloudSync build script (Linux/macOS bash)
#  Usage: ./build.sh [Release|Debug]
# ============================================================
set -e

CONFIG="${1:-Release}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

# Check dependencies
missing=()
for dll in StardewModdingAPI.dll 0Harmony.dll MonoGame.Framework.dll "Stardew Valley.dll" SMAPI.Toolkit.CoreInterfaces.dll; do
    [ -f "$dll" ] || missing+=("$dll")
done

if [ ${#missing[@]} -gt 0 ]; then
    echo "[ERROR] Missing dependency DLLs in project root:"
    for m in "${missing[@]}"; do
        echo "  $m"
    done
    echo "Please copy them from your Stardew Valley game folder. See README.md."
    exit 1
fi

echo "Building CloudSync ($CONFIG)..."
dotnet build CloudSync.csproj -c "$CONFIG" -v minimal

echo ""
echo "[OK] Build succeeded."
echo "Output: bin/$CONFIG/net6.0/CloudSync.dll"
