#!/bin/bash
export DISPLAY=:99
pkill -f "Xvfb :99"; sleep 0.5; Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
rm -rf /tmp/lad /tmp/e2eout; mkdir -p /tmp/lad; printf 'OutDir=/tmp/e2eout\n' > /home/claude/app/ytdl-settings.txt
env -u HTTPS_PROXY -u HTTP_PROXY -u https_proxy -u http_proxy PATH=/tmp/minbin LOCALAPPDATA=/tmp/lad YTD_TOOL_MIRROR=http://127.0.0.1:8770 YTD_SCALE=1 /usr/bin/mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_e2e.log 2>&1 &
sleep 6
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 1
D=$(xdotool search --name "first-time setup" | head -1); xdotool windowactivate $D; sleep 0.5; xdotool key Return
sleep 14
xdotool mousemove 60 150 click 1; xdotool type --delay 5 "http://127.0.0.1:8765/avc.mp4"
sleep 8; import -window root -crop 1400x1100+0+0 shots/e2e_1.png
xdotool mousemove 813 726 click 1
sleep 12; import -window root -crop 1400x1100+0+0 shots/e2e_2.png
ls -la /tmp/e2eout
