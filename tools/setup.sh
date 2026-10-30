#!/bin/bash
# One-time setup of a fresh container: .NET 8 SDK, xvfb, Godot 4.5.2 (.NET), then import the project and build it.
# bash tools/setup.sh      (run from anywhere; installs Godot into ~/godot)
set -e
MG=$(cd "$(dirname "$0")/.." && pwd)
command -v dotnet >/dev/null || { sudo -n true 2>/dev/null && S=sudo || S=; $S apt-get update -q && $S apt-get install -y -q dotnet-sdk-8.0; }
command -v xvfb-run >/dev/null || { sudo -n true 2>/dev/null && S=sudo || S=; $S apt-get install -y -q xvfb; }
GD=$HOME/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64
if [ ! -x "$GD" ]; then
  mkdir -p ~/godot && cd ~/godot
  curl -sSLo g.zip https://github.com/godotengine/godot/releases/download/4.5.2-stable/Godot_v4.5.2-stable_mono_linux_x86_64.zip
  unzip -q g.zip && rm g.zip
fi
# Electron 44 = the PC app's engine; the check tool runs the original game in it (its math differs from plain Chromium)
if [ ! -x "$HOME/electron44/electron" ]; then
  mkdir -p ~/electron44 && cd ~/electron44
  curl -sSLo e.zip https://github.com/electron/electron/releases/download/v44.7.0/electron-v44.7.0-linux-x64.zip
  unzip -q e.zip && rm e.zip
fi
cd "$MG"
dotnet build -v q 2>&1 | grep -E " error |rror\(s\)" || true
"$GD" --headless --import --path . >/dev/null 2>&1 || true
echo "ready: $GD"
