#!/bin/bash
# The radio level online: two copies of the game (test/TeamNetTest.cs with LV=P3), each one's square by TeamTest's bot and route,
# through the real menus, room code and connection, on the Firebase emulator:
#   bash tools/duo/net2.sh <outDir> <page> <route_blue> <route_gold>      (env T, SCALE, EVERY, RULES, NET_DELAY_MS)
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); O=$(realpath -m "$1"); R0=$(realpath "$3"); R1=$(realpath "$4")
rm -rf "$O"; mkdir -p "$O"
FB=${FB:-$O/../fb}; mkdir -p "$FB"; RULES=${RULES:-/mnt/project-files/online/firestore-v48.rules}
cp "$RULES" "$FB/firestore.rules"; echo '{"firestore":{"rules":"firestore.rules"},"emulators":{"auth":{"port":9099},"firestore":{"port":8085},"ui":{"enabled":false}}}' > "$FB/firebase.json"
cat > "$O/go.sh" <<EOS
cd "$here"
run() { LV=P3 PAGE=$2 xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-net "\$@"; }
run host "$O/h" "$R0" > "$O/h.out" 2>&1 &
sleep 3; run g1 "$O/a" "$R1" > "$O/a.out" 2>&1 &
wait
EOS
cd "$FB" && npx -y firebase-tools@13 emulators:exec --only firestore,auth --project mohammed-game-f7563 "bash $O/go.sh" > "$O/emu.out" 2>&1
for r in h a; do echo "$r: $(grep -c . $O/$r/state.log 2>/dev/null) lines, $(grep -m1 -oE 'WON.*|DONE' $O/$r/state.log | head -c 80)"; done
