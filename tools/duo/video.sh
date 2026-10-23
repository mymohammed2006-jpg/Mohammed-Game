#!/bin/bash
# A video of the bots on one page of the radio level: the whole map on top (for the video only), each one's own screen below.
#   bash tools/duo/video.sh <page> <outDir> <route0> <route1>    (env T = seconds, FROM)  -> <outDir>/video.mp4
here=$(cd "$(dirname "$0")/../.." && pwd); cd "$here"
O=$(realpath -m "$2"); rm -rf "$O"; mkdir -p "$O"
VIEW=duo EVERY=2 WAV="$O/v.wav" bash tools/duo/run.sh $1 "$O" "$3" "$4"
cd "$O"; n=0
for f in f*_0.png; do b=${f%_0.png}; printf "%s\n" "$b"; done > list.txt
ffmpeg -v error -y -framerate 30 -pattern_type glob -i 'f*_0.png' -framerate 30 -pattern_type glob -i 'f*_1.png' -framerate 30 -pattern_type glob -i 'f*_2.png' -ss ${FROM:-0} -i v.wav \
  -filter_complex "[0]crop=1280:600:0:60,scale=1152:540[a];[a]pad=1920:540:384:0:0x05070f[top];[1]scale=960:540[l];[2]scale=960:540[r];[l][r]hstack[bot];[top][bot]vstack[v]" \
  -map "[v]" -map 3:a -c:v libx264 -pix_fmt yuv420p -crf 21 -c:a aac -b:a 192k -shortest video.mp4
rm -f f*.png; ls -la video.mp4
