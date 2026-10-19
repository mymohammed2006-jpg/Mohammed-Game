#!/bin/bash
# tools/compare/audio.sh [level=L1]
# The song must play at the speed of the level's clock whatever rate the sound card runs at.
# Our containers have no sound card (44100); most PCs run at 48000, where a wrong rate makes the song slow and deep.
HERE=$(cd "$(dirname "$0")" && pwd); MG=$(cd "$HERE/../.." && pwd)
GD=${GODOT:-$HOME/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
ID=${1:-L1}; LOG=$(mktemp); bad=0
(cd "$MG" && dotnet build -v q 2>&1 | grep -E " error " | head)
for R in 44100 48000; do
  printf '[audio]\n\ndriver/mix_rate=%s\n' $R > "$MG/override.cfg"
  MG_AUDIO_CHECK=1 timeout 25 xvfb-run -a "$GD" --rendering-driver opengl3 --path "$MG" -- --dev $ID > "$LOG" 2>&1
  rm -f "$MG/override.cfg"
  r=$(grep "^MG_AUDIO " "$LOG" | awk '{d=$3-$2; if(d<0)d=-d; if(d>m)m=d; n++; last=$2} END{printf "%d %.3f %.1f", n, m, last}')
  set -- $r
  if [ "${1:-0}" -lt 10 ] || awk "BEGIN{exit !($2 > 0.02)}"; then echo "sound card $R: WRONG (checks ${1:-0}, song off by up to $2 s at $3 s)"; bad=1
  else echo "sound card $R: ok (song matches the clock within $2 s over $3 s)"; fi
done
rm -f "$LOG"; exit $bad
