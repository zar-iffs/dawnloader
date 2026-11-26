#!/bin/bash
export DISPLAY=:99; S=1; P=m100
pkill -f "mono /home/claude/app/ytd.exe"; pkill -f "Xvfb :99"; sleep 0.5; Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
printf "Cookies=1\n" > /home/claude/app/ytdl-settings.txt; rm -rf /root/Downloads/YT
YTD_DRIVE_BASE=http://127.0.0.1:8767 PATH=/home/claude/app/fakebin:$PATH YTD_SCALE=$S mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_$P.log 2>&1 & sleep 5
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 0.5
G=$(xdotool getwindowgeometry $WID | grep Geometry | awk '{print $2}'); W=${G%x*}; H=${G#*x}
shot(){ import -window root -crop ${W}x${H}+0+0 /home/claude/app/shots/${P}_$1.png; }
xdotool mousemove 60 150 click 1
for u in "https://drive.google.com/file/d/PUBLIC1abcdefghij/view" "https://drive.google.com/file/d/PRIVATE1abcdefgh/view" "http://127.0.0.1:8765/My%20holiday%20clip.mp4" "https://www.youtube.com/watch?v=QwErTyUiOp1" "https://www.youtube.com/watch?v=memberz123"; do
  xdotool type --delay 2 "$u"; xdotool key Return; done
sleep 14; shot 1_checked
xdotool mousemove $(( W - 120 )) $(( H - 57 )) click 1
sleep 25; shot 2_done
