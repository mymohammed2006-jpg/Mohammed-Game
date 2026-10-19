#!/bin/bash
# tools/compare/ui/ui.sh : the menus/online check. Runs the same key script in the port (Godot) and in the original (browser),
# then compares every frame's UI state, the Firebase requests, and the captured pictures.
set -e
HERE=$(cd "$(dirname "$0")" && pwd); MG=$(cd "$HERE/../../.." && pwd)
GD=${GODOT:-$HOME/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
GAME=${GAME:-/mnt/project-files/Mohammed_Game/Mohammed Game}
WORK=${WORK:-$MG/../cmpwork}; OUT=${OUT:-$WORK/out}
if [ ! -f "$WORK/game/harness.html" ] || [ "$GAME/index.html" -nt "$WORK/game/harness.html" ]; then
  mkdir -p "$WORK/game"; ln -sfn "$GAME/songs" "$WORK/game/songs"
  python3 "$HERE/../make_harness.py" "$GAME" "$WORK/game/harness.html" "$HERE/../fonts.css" >/dev/null
fi
python3 "$HERE/make_ui.py" "$MG" >/dev/null
(cd "$MG" && dotnet build -v q 2>&1 | grep -E " error " | sed 's/ \[.*//' | sort -u | head -20)
rm -rf "$OUT/gd-ui" "$OUT/js-ui"
xvfb-run -a -s "-screen 0 1280x720x24" "$GD" --rendering-driver opengl3 --path "$MG" -- --ui-run "$OUT/gd-ui" "$HERE" 2>&1 \
  | grep -E "ERROR|Exception|   at |ui-run" | grep -v "ALSA\|audio_driver\|init_output\|audio_server\|RIDs\|_free_rids" | head -20 || true
node "$HERE/ui-driver.js" "$WORK/game/harness.html" "$OUT/js-ui" "$OUT/gd-ui/durations.json"
python3 "$HERE/ui-diff.py" "$OUT/js-ui" "$OUT/gd-ui"
