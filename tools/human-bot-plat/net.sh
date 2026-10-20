#!/bin/bash
# The human-like bots online: three copies of the game on the co-op level (test/TeamBotNet.cs), on the Firebase emulator:
#   bash tools/human-bot-plat/net.sh <outDir>          (needs the clean run's ref_P2.json: run run.sh once first)
# env SEED, SCALE (default 1), T, and per player: LEAVE_H/LEAVE_A/LEAVE_B or KILL_H/KILL_A/KILL_B = song seconds to leave at.
# The emulator: firebase.json + the live rules (online/firestore-coop3.rules) in $FB (default: a folder next to the output).
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); O=$(realpath -m "$1"); C=$here/tools/human-bot-plat/P2.cfg
rm -rf "$O"; mkdir -p "$O"
FB=${FB:-$O/../fb}; mkdir -p "$FB"; RULES=${RULES:-$here/../online/firestore-coop3.rules}
[ -f "$FB/firebase.json" ] || { cp "$RULES" "$FB/firestore.rules"; echo '{"firestore":{"rules":"firestore.rules"},"emulators":{"auth":{"port":9099},"firestore":{"port":8085},"ui":{"enabled":false}}}' > "$FB/firebase.json"; }
cat > "$O/go.sh" <<EOS
cd "$here"
run() { xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-bot-net "\$@"; }
LEAVE=\$LEAVE_H KILL=\$KILL_H run host "$O/h" "$C" > "$O/h.out" 2>&1 &
sleep 3; LEAVE=\$LEAVE_A KILL=\$KILL_A run g1 "$O/a" "$C" > "$O/a.out" 2>&1 &
sleep 3; LEAVE=\$LEAVE_B KILL=\$KILL_B run g2 "$O/b" "$C" > "$O/b.out" 2>&1 &
wait
EOS
cd "$FB" && npx -y firebase-tools@13 emulators:exec --only firestore,auth --project mohammed-game-f7563 "bash $O/go.sh" > "$O/emu.out" 2>&1
for r in h a b; do echo "$r: $(cat $O/$r/summary.txt 2>/dev/null) $(tail -1 $O/$r/notes.log 2>/dev/null)"; done
