#!/bin/bash
# One side of a before/after clip for the story video: the story run in a game folder (the old v41 code or the new one),
# frames a-b with the game's sound and a fixed line at the top (TAG, colour TAGC); extra env (TP, HOLD) as TeamBot says.
#   bash tools/human-bot-plat/pair.sh <gameDir> <out.mp4> <a> <b> "<tag>" <colour> [VAR=value ...]
GD=${GD:-~/godot/Godot_v4.5.2-stable_mono_linux_x86_64/Godot_v4.5.2-stable_mono_linux.x86_64}
G=$(realpath "$1"); out=$(realpath -m "$2"); a=$3 b=$4 tag=$5 col=$6; w=$(mktemp -d); shift 6
H=$(cd "$(dirname "$0")" && pwd)
(cd "$G"; env "$@" STORY=$H/story_P2.txt TAG="$tag" TAGC="$col" CLIPS="$a-$b" WAV="$w/v.wav" T=$(python3 -c "print($b/60+0.05)") \
  xvfb-run -a -s "-screen 0 1280x720x24" $GD --rendering-driver opengl3 --path . -- --team-bot P2 "$w/f" $H/P2.cfg > "$w/out.txt" 2>&1)
d=$(python3 -c "print(($b-$a+1)/60)"); e=$(python3 -c "print(max(0,($b-$a+1)/60-0.25))")
ffmpeg -y -loglevel error -framerate 60 -start_number $a -i "$w/f/f%06d.png" -ss $(python3 -c "print(($a-1)/60)") -t $d -i "$w/v.wav" \
  -frames:v $((b-a+1)) -vf "fade=in:st=0:d=0.25,fade=out:st=$e:d=0.25" -af "afade=in:st=0:d=0.15,afade=out:st=$e:d=0.25" \
  -c:v libx264 -pix_fmt yuv420p -crf 20 -c:a aac -b:a 192k -ar 44100 "$out"
rm -rf "$w"
