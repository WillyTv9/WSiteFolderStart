#!/bin/bash
# Assembla WSiteFolderStart.app a partire dai sorgenti della repo.
#
# Uso:
#   ./macos/build.sh          -> build/WSiteFolderStart.app
#   ./macos/build.sh --zip    -> in più build/WSiteFolderStart-macOS.zip (pronto per una Release)
set -euo pipefail

cd "$(dirname "$0")/.."

NAME="WSiteFolderStart"
OUT="build/$NAME.app"
VERSION="$(tr -d '[:space:]' < VERSION)"

echo "▸ Costruisco $NAME $VERSION"
rm -rf "$OUT"
mkdir -p "$OUT/Contents/MacOS" "$OUT/Contents/Resources/templates"

cp macos/Info.plist                "$OUT/Contents/Info.plist"
cp macos/launcher                  "$OUT/Contents/MacOS/$NAME"
chmod +x                           "$OUT/Contents/MacOS/$NAME"
cp macos/main.js macos/fallback.applescript "$OUT/Contents/Resources/"

for f in icon.png icon_dock.png AppIcon.icns bg_light.jpg bg_dark.jpg; do
  cp "assets/$f" "$OUT/Contents/Resources/$f"
done
cp templates/* "$OUT/Contents/Resources/templates/"

# Scrive la versione (solo su macOS, dove esiste PlistBuddy)
if [ -x /usr/libexec/PlistBuddy ]; then
  /usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$OUT/Contents/Info.plist"
  /usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION"            "$OUT/Contents/Info.plist"
fi

# Firma "ad-hoc": non sostituisce la notarizzazione Apple, ma evita bundle "rotti"
if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$OUT" >/dev/null 2>&1 || echo "  (firma ad-hoc non riuscita, continuo)"
fi

if [ "${1:-}" = "--zip" ]; then
  ZIP="build/$NAME-macOS.zip"
  rm -f "$ZIP"
  ditto -c -k --keepParent "$OUT" "$ZIP"   # ditto conserva permessi eseguibili e attributi
  echo "✔ $ZIP"
fi

echo "✔ $OUT"
