#!/bin/bash
# Three copies of the game online on the co-op level (test/TeamNetTest.cs). Run inside the Firebase emulator, e.g.
#   npx firebase-tools emulators:exec --only firestore,auth --project mohammed-game-f7563 "bash tools/team/net3.sh <out> <r0> <r1> <r2>"
# env PAGE (start page), T (game seconds), SCALE, EVERY pass through. Each route is one player's TeamTest route.
GD=${GD:-~/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
O=$(realpath -m "$1"); R0=$(realpath "$2"); R1=$(realpath "$3"); R2=$(realpath "$4"); here=$(cd "$(dirname "$0")/../.." && pwd)
rm -rf "$O"; mkdir -p "$O"; cd "$here"
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-net host "$O/h" "$R0" > "$O/h.log" 2>&1 &
sleep 3
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-net g1 "$O/a" "$R1" > "$O/a.log" 2>&1 &
sleep 3
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-net g2 "$O/b" "$R2" > "$O/b.log" 2>&1 &
wait
