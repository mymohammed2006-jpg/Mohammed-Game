#!/bin/bash
# One bot run of the radio level: bash tools/duo/run.sh <page> <outDir> <route0> <route1>   (env T, NODRAW, VIEW=duo, WAV, FPS)
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); cd "$here"
O=$(realpath -m "$2"); mkdir -p "$O"
xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-run P3 $1 "$O" "$(realpath $3)" "$(realpath $4)" "$(realpath $4)" ${FPS:-60} ${EVERY:-1} > "$O/run.out" 2>&1
