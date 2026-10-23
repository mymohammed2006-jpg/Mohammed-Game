#!/bin/bash
# Up to four copies of the game in a versus match (test/NetTest.cs). Run inside the Firebase emulator.
# vs.sh <out> then per player "role:ENV=v,ENV=v" e.g. host:FIN=8 guest:FIN=12 g2:FIN=10 g3:
GD=/root/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64
O=$(realpath -m "$1"); shift; GP=$(cd "$(dirname "$0")/.." && pwd)
rm -rf "$O"; mkdir -p "$O"; : > "$O/idle.txt"; cd "$GP"
for spec in "$@"; do
  role=${spec%%:*}; envs=${spec#*:}; E=(); IFS=',' read -ra kv <<< "$envs"; for x in "${kv[@]}"; do [ -n "$x" ] && E+=("$x"); done
  env "${E[@]}" NP=${NP:-4} xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --net-test $role "$O/$role" "$O/idle.txt" > "$O/$role.log" 2>&1 &
  sleep 4
done
wait
