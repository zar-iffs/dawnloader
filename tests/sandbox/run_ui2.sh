#!/bin/bash
export DISPLAY=:99
S=$1; P=$2
px(){ python3 -c "print(int($1*$S))"; }
pkill -f "mono /home/claude/app/ytd.exe"; pkill -f "Xvfb :99"; sleep 0.5
Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
rm -f /home/claude/app/ytdl-settings.txt; rm -rf /root/Downloads/YT
PATH=/home/claude/app/fakebin:$PATH YTD_SCALE=$S mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_$P.log 2>&1 &
sleep 5
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 0.5
G=$(xdotool getwindowgeometry $WID | grep Geometry | awk '{print $2}'); W=${G%x*}; H=${G#*x}
shot(){ import -window root -crop ${W}x${H}+0+0 /home/claude/app/shots/${P}_$1.png; }
xdotool mousemove $(px 60) $(px 150) click 1
xdotool type --delay 3 "https://youtu.be/J9Skrt_rxI8?si=VjHrL1MQcBW063Sm"; xdotool key Return
xdotool type --delay 3 "https://www.youtube.com/watch?v=aBcDeFgHiJk"; xdotool key Return
xdotool type --delay 3 "https://www.youtube.com/playlist?list=PLTEST"; xdotool key Return
xdotool type --delay 3 "https://www.youtube.com/watch?v=private1"
sleep 12; shot 2_checked
xdotool mousemove $(px 340) $(px 297) click 1; sleep 1.5; shot 3_audio
xdotool mousemove $(( W - $(px 120) )) $(( H - $(px 57) )) click 1
sleep 4; shot 4_downloading
sleep 30; shot 5_done
