#!/bin/bash
# tools/compare/fps.sh <levelId> <n|e> [rates="60 144 240"]
# v41 fixed steps: plays the level with the same key script (in song time) at several frame rates
# and checks that the game is the same at every moment the rates share.
HERE=$(cd "$(dirname "$0")" && pwd); MG=$(cd "$HERE/../.." && pwd)
GD=${GODOT:-$HOME/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
OUT=${OUT:-$MG/../cmpwork/fps}; ID=$1; M=$2; RATES=${3:-60 144 240}
(cd "$MG" && dotnet build -v q 2>&1 | grep -E " error " | sed 's/ \[.*//' | sort -u | head -20)
for r in $RATES; do
  rm -rf "$OUT/$ID$M-$r"
  FPS=$r xvfb-run -a -s "-screen 0 1280x720x24" "$GD" --rendering-driver opengl3 --path "$MG" -- --play-run $ID $M "$OUT/$ID$M-$r" 1000000000 2>&1 \
    | grep -E "ERROR|Exception|   at " | grep -v "ALSA\|audio_driver\|init_output\|audio_server\|RIDs\|_free_rids" | head -5 &
done
wait
python3 "$HERE/fpsdiff.py" $(for r in $RATES; do echo "$OUT/$ID$M-$r/state.log"; done)
