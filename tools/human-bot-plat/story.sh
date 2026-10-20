#!/bin/bash
# The story video: three bots learn the co-op level room by room (a failed try, then the solve), with Arabic lines.
#   bash tools/human-bot-plat/story.sh <outDir>      -> <outDir>/story.mp4   (needs ref_P2.json from run.sh)
# One run without pictures plans the cut (story_clips.py); the sound comes from one run; then each clip's frames are drawn
# by its own run (the runs are the same to the frame) and encoded right away, so the pictures never pile up on the disk.
GD=${GD:-~/godot/Godot_v4.4.1-stable_mono_linux_x86_64/Godot_v4.4.1-stable_mono_linux.x86_64}
here=$(cd "$(dirname "$0")/../.." && pwd); cd "$here"; H=tools/human-bot-plat
O=$(realpath -m "$1"); rm -rf "$O"; mkdir -p "$O/c"
run() { STORY=$H/story_P2.txt xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-bot P2 "$@"; }
THINGLOG=1 NODRAW=1 WAV="$O/v.wav" T=3000 run "$O/plan" $H/P2.cfg > "$O/plan.out" 2>&1
CLIPS=$(python3 $H/story_clips.py "$O/plan/events.log"); echo "$CLIPS" > "$O/clips.txt"
n=0; : > "$O/c/list.txt"
one() {
  local a=$1 b=$2 n=$3 d e
  CLIPS="$a-$b" T=$(python3 -c "print($b/60+0.05)") run "$O/f$n" $H/P2.cfg > "$O/f$n.out" 2>&1
  d=$(python3 -c "print(($b-$a+1)/60)"); e=$(python3 -c "print(max(0,($b-$a+1)/60-0.3))")
  ffmpeg -y -loglevel error -framerate 60 -start_number $a -i "$O/f$n/f%06d.png" -ss $(python3 -c "print(($a-1)/60)") -t $d -i "$O/v.wav" \
    -frames:v $((b-a+1)) -vf "fade=in:st=0:d=0.3,fade=out:st=$e:d=0.3" -af "afade=in:st=0:d=0.2,afade=out:st=$e:d=0.3" \
    -c:v libx264 -pix_fmt yuv420p -crf 20 -c:a aac -b:a 192k -ar 44100 "$O/c/$n.mp4"
  rm -rf "$O/f$n"
}
for c in ${CLIPS//,/ }; do
  n=$((n+1)); echo "file '$O/c/$n.mp4'" >> "$O/c/list.txt"
  one ${c%-*} ${c#*-} $n &
  [ $((n % ${PAR:-3})) = 0 ] && wait
done
wait
ffmpeg -y -loglevel error -f concat -safe 0 -i "$O/c/list.txt" -c copy "$O/story.mp4"
cat "$O/plan/summary.txt"; ffprobe -v error -show_entries format=duration -of csv=p=0 "$O/story.mp4"
