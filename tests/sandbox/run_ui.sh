#!/bin/bash
# usage: run_ui.sh SCALE OUTPREFIX
export DISPLAY=:99
SCALE=$1; P=$2
pkill -f "Xvfb :99" ; sleep 0.5
Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 &
sleep 1
rm -f /home/claude/app/ytdl-settings.txt
PATH=/home/claude/app/fakebin:$PATH YTD_SCALE=$SCALE mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_$P.log 2>&1 &
sleep 5
WID=$(xdotool search --name "^YT Downloader$" | head -1)
xdotool windowmove $WID 0 0; sleep 1
import -window root -crop 1400x1100+0+0 /home/claude/app/shots/${P}_1_empty.png
# type links into the links box (click into it first)
eval $(xdotool getwindowgeometry --shell $WID)
xdotool mousemove $(python3 -c "print(int(60*$SCALE))") $(python3 -c "print(int(150*$SCALE))") click 1
xdotool type --delay 5 "https://youtu.be/J9Skrt_rxI8?si=VjHrL1MQcBW063Sm"; xdotool key Return
xdotool type --delay 5 "https://www.youtube.com/watch?v=aBcDeFgHiJk"; xdotool key Return
xdotool type --delay 5 "https://www.youtube.com/playlist?list=PLTEST"; xdotool key Return
xdotool type --delay 5 "https://www.youtube.com/watch?v=private1"
sleep 14
import -window root -crop 1400x1100+0+0 /home/claude/app/shots/${P}_2_checked.png
