#!/bin/bash
# A short video of chosen moments of one bot run: pictures only for those frames, the game's own sound, Arabic captions.
#   CLIPS="540-1100,5150-5450,..." [CHAOS=1 ALL3=0.3 T=2400] bash tools/human-bot-plat/clips.sh <outDir> <seed>
# The frames are the @numbers in that run's events.log (run.sh with the same env and seed plays the same run).
# Needs ref_P2.json from run.sh. -> <outDir>/clips.mp4
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); cd "$here"; H=tools/human-bot-plat
O=$(realpath -m "$1"); rm -rf "$O/v" "$O/c"; mkdir -p "$O/v" "$O/c"
SEED=$2 LABELS=${LABELS:-1} WAV="$O/v.wav" xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-bot P2 "$O/v" $H/P2.cfg > "$O/v.out" 2>&1
n=0; : > "$O/c/list.txt"
for c in ${CLIPS//,/ }; do
  a=${c%-*}; b=${c#*-}; n=$((n+1)); d=$(python3 -c "print(($b-$a+1)/60)"); e=$(python3 -c "print(max(0,($b-$a+1)/60-0.25))")
  ffmpeg -y -loglevel error -framerate 60 -start_number $a -i "$O/v/f%06d.png" -ss $(python3 -c "print(($a-1)/60)") -t $d -i "$O/v.wav" \
    -frames:v $((b-a+1)) -vf "fade=in:st=0:d=0.25,fade=out:st=$e:d=0.25" -af "afade=in:st=0:d=0.15,afade=out:st=$e:d=0.25" \
    -c:v libx264 -pix_fmt yuv420p -crf 20 -c:a aac -b:a 192k -ar 44100 "$O/c/$n.mp4"
  echo "file '$O/c/$n.mp4'" >> "$O/c/list.txt"
done
ffmpeg -y -loglevel error -f concat -safe 0 -i "$O/c/list.txt" -c copy "$O/clips.mp4"
rm -f "$O"/v/f*.png
cat "$O/v/summary.txt"; ffprobe -v error -show_entries format=duration -of csv=p=0 "$O/clips.mp4"
