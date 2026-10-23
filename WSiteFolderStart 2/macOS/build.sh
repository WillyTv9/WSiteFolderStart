#!/bin/bash
# Richiede Xcode (o Command Line Tools) installato. Per il vero Liquid Glass serve Xcode 26.
set -e
cd "$(dirname "$0")"
APP="WSiteFolderStart.app"
rm -rf "$APP" AppIcon.iconset
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources" AppIcon.iconset

swiftc -O -parse-as-library -target "$(uname -m)-apple-macos15.0" main.swift \
  -o "$APP/Contents/MacOS/WSiteFolderStart"

for s in 16 32 128 256 512; do
  sips -z $s $s icon.png --out AppIcon.iconset/icon_${s}x${s}.png >/dev/null
  sips -z $((s*2)) $((s*2)) icon.png --out AppIcon.iconset/icon_${s}x${s}@2x.png >/dev/null
done
iconutil -c icns AppIcon.iconset -o "$APP/Contents/Resources/AppIcon.icns"
rm -rf AppIcon.iconset

cp Info.plist "$APP/Contents/Info.plist"
codesign --force --deep --sign - "$APP"
echo "Fatto: $(pwd)/$APP"
