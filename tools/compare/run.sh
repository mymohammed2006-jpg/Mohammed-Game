#!/bin/bash
# tools/compare/run.sh <levelId> <n|e> [every=60] [from=0] [to=all]
# Plays the bot's S run in the original game (browser) and in the port (Godot), frame by frame at 60 fps,
# then compares the game state of every frame and the pixels of every `every`-th frame.
#   MODE=play  real play through step() with a fixed key script instead (movement, dash, hits, deaths, restarts)
#   RPV=1|2    the bot run played as an old replay of that version (old level shapes; the bot may get hit)
set -e
HERE=$(cd "$(dirname "$0")" && pwd); MG=$(cd "$HERE/../.." && pwd)
GD=${GODOT:-$HOME/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
[ -x "$GD" ] || GD=$MG/../gm/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64
GAME=${GAME:-/mnt/project-files/Mohammed_Game/Mohammed Game}
WORK=${WORK:-$MG/../cmpwork}; OUT=${OUT:-$WORK/out}
ID=$1; M=$2; EV=${3:-60}; FROM=${4:-0}; TO=${5:-}
if [ "$MODE" = play ]; then FLAG=--play-run; TAG=play-; else FLAG=--bot-run; TAG=; fi
[ -n "$RPV" ] && TAG="v$RPV-$TAG"
# the original game, made drivable (no song decoding, hand-driven clocks)
if [ ! -f "$WORK/game/harness.html" ] || [ "$GAME/index.html" -nt "$WORK/game/harness.html" ]; then
  mkdir -p "$WORK/game"; ln -sfn "$GAME/songs" "$WORK/game/songs"
  python3 "$HERE/make_harness.py" "$GAME" "$WORK/game/harness.html" "$HERE/fonts.css" >/dev/null
fi
rm -rf "$OUT/js-$TAG$ID$M" "$OUT/gd-$TAG$ID$M"   # no frames left over from an earlier run
(cd "$MG" && dotnet build -v q 2>&1 | grep -E " error " | sed 's/ \[.*//' | sort -u | head -20)
# the original runs in Electron 44 like the players' PC app (set ELECTRON= to an empty value to use plain Chromium)
ELECTRON=${ELECTRON-$HOME/electron44/electron}; [ -x "$ELECTRON" ] || ELECTRON=
if [ -n "$ELECTRON" ]; then
  ELECTRON="$ELECTRON" xvfb-run -a -s "-screen 0 1920x1080x24" node "$HERE/driver.js" "$WORK/game/harness.html" $ID $M "$OUT/js-$TAG$ID$M" $EV $FROM $TO &
else
  echo "WARNING: Electron 44 not found (bash tools/setup.sh installs it); using plain Chromium, whose math differs slightly"
  node "$HERE/driver.js" "$WORK/game/harness.html" $ID $M "$OUT/js-$TAG$ID$M" $EV $FROM $TO &
fi
xvfb-run -a -s "-screen 0 1280x720x24" "$GD" --rendering-driver opengl3 --path "$MG" -- $FLAG $ID $M "$OUT/gd-$TAG$ID$M" $EV $FROM $TO 2>&1 \
  | grep -E "ERROR|Exception|   at " | grep -v "ALSA\|audio_driver\|init_output\|audio_server\|RIDs\|_free_rids" | head -20 || true
wait
# the original sometimes doesn't start when several runs share the machine (two xvfb-run picking one display): run it once more
if [ ! -s "$OUT/js-$TAG$ID$M/state.log" ] && [ -n "$ELECTRON" ]; then
  echo "original produced no frames, running it again"
  ELECTRON="$ELECTRON" xvfb-run -a -s "-screen 0 1920x1080x24" node "$HERE/driver.js" "$WORK/game/harness.html" $ID $M "$OUT/js-$TAG$ID$M" $EV $FROM $TO
fi
python3 "$HERE/diff.py" "$OUT/js-$TAG$ID$M" "$OUT/gd-$TAG$ID$M"
echo "frames: $OUT/js-$TAG$ID$M (original)  $OUT/gd-$TAG$ID$M (port)   side by side: python3 $HERE/sbs.py <a.png> <b.png> <out.png>"
