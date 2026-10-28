#!/bin/bash
# Builds the Windows release: the game, its installer (Mohammed-Game-Setup.exe), the update bridge (Mohammed-Game-Update.exe)
# and latest.yml, into build/release.
# Same file names as the old PC app, so its auto-update picks the new game up.
# Needs: dotnet 8 (also builds the bridge), Godot 4.4.1 .NET (+ export templates), node (npx), makensis.  Run: bash tools/release/build.sh
set -e
MG=$(cd "$(dirname "$0")/../.." && pwd)
GD=${GODOT:-$HOME/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
cd "$MG"
V=$(sed -n 's/^config\/version="\(.*\)"/\1/p' project.godot)
[ -n "$V" ] || { echo "no config/version in project.godot"; exit 1; }
echo "version $V"

T=$HOME/.local/share/godot/export_templates/4.4.1.stable.mono
if [ ! -f "$T/windows_release_x86_64.exe" ]; then
  mkdir -p "$T" && cd "$T"
  curl -sSLo t.tpz https://github.com/godotengine/godot/releases/download/4.4.1-stable/Godot_v4.4.1-stable_mono_export_templates.tpz
  unzip -q -j t.tpz 'templates/windows_release_x86_64.exe' 'templates/windows_release_x86_64_console.exe' 'templates/version.txt' -d .
  rm t.tpz; cd "$MG"
fi

rm -rf build/win build/release; mkdir -p build/win build/release
"$GD" --headless --import --path . >/dev/null 2>&1 || true
"$GD" --headless --path . --export-release "Windows Desktop" "$MG/build/win/Mohammed Game.exe" 2>&1 | grep -E "ERROR|error" && { cat ~/.local/share/godot/mono/build_logs/*/msbuild_issues.csv; grep -h -E "error" ~/.local/share/godot/mono/build_logs/*/msbuild_log.txt 2>/dev/null | sort -u | head -40; exit 1; } || true   # show why the .NET build failed
[ -f "build/win/Mohammed Game.exe" ] && [ -f "build/win/Mohammed Game.pck" ] || { echo "export failed"; exit 1; }
rm -f build/win/*_console.exe
cp -r licenses build/win/   # the libraries' licenses go with the game

# the program's own icon and name (Godot's export would need rcedit for this)
npx --yes resedit-cli@3.1.1 --in "build/win/Mohammed Game.exe" --out "build/win/Mohammed Game.exe" --delete-allicon --icon 1,assets/icon.ico \
  --product-name "Mohammed Game" --file-description "Mohammed Game" --company-name Mohammed --original-filename "Mohammed Game.exe" \
  --internal-name "Mohammed Game" --file-version "$V.0" --product-version "$V.0"

cd installer
makensis -V2 -DVERSION="$V" -DSRC="$MG/build/win" -DOUT="$MG/build/release/Mohammed-Game-Setup.exe" installer.nsi
# the bridge for v40-v42: their updater stops any download after 30 s, so they get this small file, which downloads the installer
cd "$MG/installer/bridge"
dotnet build -c Release -v q -o "$MG/build/bridge" -p:Version="$V"
cp "$MG/build/bridge/Mohammed-Game-Update.exe" "$MG/build/release/"
cd "$MG/build/release"
SHA=$(openssl dgst -sha512 -binary Mohammed-Game-Setup.exe | base64 -w0)
SIZE=$(stat -c %s Mohammed-Game-Setup.exe)
BSHA=$(openssl dgst -sha512 -binary Mohammed-Game-Update.exe | base64 -w0)
# files: the old Electron app (v39) takes the installer from here; path/sha512: the Godot game up to v42 takes the bridge;
# fullPath/fullSha512: v43 on takes the installer
cat > latest.yml <<YML
version: $V
files:
  - url: Mohammed-Game-Setup.exe
    sha512: $SHA
    size: $SIZE
path: Mohammed-Game-Update.exe
sha512: $BSHA
fullPath: Mohammed-Game-Setup.exe
fullSha512: $SHA
releaseDate: '$(date -u +%Y-%m-%dT%H:%M:%S.000Z)'
YML
ls -la "$MG/build/release"
