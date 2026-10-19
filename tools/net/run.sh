#!/bin/bash
# Two copies of the game online against each other (test/NetTest.cs). Run inside the Firebase emulator, e.g.
#   npx firebase-tools emulators:exec --only firestore,auth --project mohammed-game-f7563 "bash tools/net/run.sh <host route> <out>"
# The host plays <host route>, the guest plays tools/net/slow.txt (the same S route 1.5 s later). PNGs + state.log in <out>/h and <out>/g.
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
R=$(realpath "$1"); O=$(realpath -m "${2:-/tmp/nettest}"); here=$(cd "$(dirname "$0")/../.." && pwd)
rm -rf "$O/h" "$O/g" "$O/code.txt"; mkdir -p "$O"; cd "$here"
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --net-test host "$O/h" "$R" > "$O/h.log" 2>&1 &
sleep 2
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --net-test guest "$O/g" "$here/tools/net/slow.txt" > "$O/g.log" 2>&1 &
wait
