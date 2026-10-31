#!/bin/bash
# The human-like bots on the co-op level, offline (test/TeamBot.cs). A test tool: nothing of it is in the game players get.
#   bash tools/human-bot-plat/run.sh <outDir> [seeds=1-20]      many runs, NODRAW, then a table of what happened
#   VIDEO=1 bash tools/human-bot-plat/run.sh <outDir> <seed>    one run with pictures and the game's sound -> <outDir>/video.mp4
# First it plays the level clean (no mistakes) to learn each room's starting beat and places (ref_P2.json).
# env as in test/TeamBot.cs (SEED is set here; JIT, REACT, MIS, MISN, ALL3, FPS, VIEW pass through).
GD=${GD:-~/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); cd "$here"; H=tools/human-bot-plat
O=$(realpath -m "$1"); mkdir -p "$O"; SEEDS=${2:-1-20}
run() { xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-bot P2 "$@"; }
NODRAW=1 REF=$H/ref_P2.json run "$O/ref" $H/P2.cfg > "$O/ref.out" 2>&1
grep -q "won=True" "$O/ref/summary.txt" || { echo "the clean run did not finish:"; cat "$O/ref/summary.txt"; }
if [ -n "$VIDEO" ]; then
  rm -rf "$O/v"; SEED=$SEEDS WAV="$O/v.wav" run "$O/v" $H/P2.cfg > "$O/v.out" 2>&1
  ffmpeg -y -loglevel error -framerate ${FPS:-60} -i "$O/v/f%06d.png" -i "$O/v.wav" -c:v libx264 -pix_fmt yuv420p -crf 20 -c:a aac -b:a 192k -shortest "$O/video.mp4"
  rm -f "$O"/v/f*.png; cat "$O/v/summary.txt"; exit
fi
a=${SEEDS%-*}; b=${SEEDS#*-}; n=0
for s in $(seq $a $b); do
  ( SEED=$s NODRAW=1 run "$O/s$s" $H/P2.cfg > "$O/s$s.out" 2>&1 ) &
  n=$((n+1)); [ $((n % ${PAR:-4})) = 0 ] && wait
done; wait
for s in $(seq $a $b); do echo "seed $s: $(cat "$O/s$s/summary.txt" 2>/dev/null || echo crashed)"; done | tee "$O/table.txt"
cat "$O"/s*/bugs.log 2>/dev/null | grep "^\[" | sed 's/^\[[^]]*\] //' | sort | uniq -c | sort -rn | tee "$O/bugs.txt"
