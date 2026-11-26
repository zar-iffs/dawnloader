#!/bin/bash
export DISPLAY=:99; S=1; P=p100
pkill -f "mono /home/claude/app/ytd.exe"; pkill -f "Xvfb :99"; sleep 0.5; Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
rm -f /home/claude/app/ytdl-settings.txt; rm -rf /root/Downloads/YT
YTD_DRIVE_BASE=http://127.0.0.1:8767 PATH=/home/claude/app/fakebin:$PATH YTD_SCALE=$S mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_$P.log 2>&1 & sleep 5
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 0.5
G=$(xdotool getwindowgeometry $WID | grep Geometry | awk '{print $2}'); W=${G%x*}; H=${G#*x}
shot(){ import -window root -crop ${W}x${H}+0+0 /home/claude/app/shots/${P}_$1.png; }
xdotool mousemove 60 150 click 1
for u in "https://www.youtube.com/playlist?list=PLTEST" "https://www.youtube.com/watch?v=QwErTyUiOp1&list=PLTEST&index=3" "https://www.youtube.com/watch?v=aBcDeFgHiJk"; do
  xdotool type --delay 2 "$u"; xdotool key Return; done
sleep 12; shot 1_checked
xdotool mousemove 300 522 click 3; sleep 1; shot 2_menu
xdotool mousemove 340 532 click 1; sleep 9; shot 3_whole
