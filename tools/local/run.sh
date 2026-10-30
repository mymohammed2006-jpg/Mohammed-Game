#!/bin/bash
# Local play without people (test/LocalTest.cs): bash tools/local/run.sh <boss|vs|team> <levelId> <players> <outDir> [fps]
# env as in test/LocalTest.cs (T, EVERY, SHOTS, HP, STILL, PC, SIGN, FIN, WIN, PZ, KB, PAGE, LOBBY, AUTH)
GD=${GD:-~/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); O=$(realpath -m "$4"); rm -rf "$O"; mkdir -p "$O"; cd "$here"
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --local-test "$1" "$2" "$3" "$O" ${5:-60} > "$O/godot.log" 2>&1
tail -3 "$O/godot.log"; cat "$O/result.txt" 2>/dev/null
