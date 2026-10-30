#!/bin/bash
# Up to four copies of the game against the boss together (test/BossNetTest.cs). Run inside the Firebase emulator.
# boss.sh <out> then per player "role:ENV=v,ENV=v" e.g. host:NP=2,HP=1 g1:HP=1,STILL=1
GD=/root/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64
O=$(realpath -m "$1"); shift; GP=$(cd "$(dirname "$0")/.." && pwd)
rm -rf "$O"; mkdir -p "$O"; cd "$GP"
for spec in "$@"; do
  role=${spec%%:*}; envs=${spec#*:}; E=(); IFS=',' read -ra kv <<< "$envs"; for x in "${kv[@]}"; do [ -n "$x" ] && E+=("$x"); done
  env "${E[@]}" xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --boss-net $role "$O/$role" > "$O/$role.log" 2>&1 &
  sleep 4
done
wait
