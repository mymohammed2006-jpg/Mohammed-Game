#!/bin/bash
# The full check of some levels, every mode, against the original running in Electron 44:
#   bash tools/compare/sweep.sh L1 F7 ...      -> one line per run in $WORK/sweep.txt (and on screen)
# Modes: n, e, MODE=play n/e, RPV=2 n/e (levels with old AFK shapes: L1-L8, F1-F7), RPV=1 n/e (F7).
# EVERY=<n> sets the picture spacing (default 120).
HERE=$(cd "$(dirname "$0")" && pwd); MG=$(cd "$HERE/../.." && pwd)
WORK=${WORK:-$MG/../cmpwork}; mkdir -p "$WORK"; SUM=$WORK/sweep.txt
EV=${EVERY:-120}
one() {  # <label> <env...> -- <id> <m>
  local label=$1; shift
  local out; out=$(env "$@" bash "$HERE/run.sh" $ID $M $EV 2>&1)
  local st; st=$(echo "$out" | grep -o "state: [0-9]* lines differ of [0-9]*")
  local px; px=$(echo "$out" | grep -o "mean diff avg [0-9.]*/255")
  local first; first=$(echo "$out" | grep -m1 "^STATE")
  local tag=; [ "$label" = play ] && tag=play-; [ "$label" = v2 ] && tag=v2-; [ "$label" = v1 ] && tag=v1-
  local raw; raw=$(diff "$WORK/out/js-$tag$ID$M/state.log" "$WORK/out/gd-$tag$ID$M/state.log" 2>/dev/null | grep -c '^<')
  echo "$ID $M $label | ${st:-NO STATE (failed)} | bit-exact lines off: $raw | $px ${first:+| $first}" | tee -a "$SUM"
}
for ID in "$@"; do
  for M in n e; do
    one bot MODE=
    one play MODE=play
    case $ID in L1|L2|L3|L4|L5|L6|L7|L8|F1|F2|F3|F4|F5|F6|F7) one v2 MODE= RPV=2 ;; esac
    [ $ID = F7 ] && one v1 MODE= RPV=1
  done
done
