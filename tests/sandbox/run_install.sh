#!/bin/bash
export DISPLAY=:99
pkill -f "Xvfb :99"; sleep 0.5; Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
rm -rf /tmp/lad; mkdir -p /tmp/lad; rm -f /home/claude/app/ytdl-settings.txt
env -u HTTPS_PROXY -u HTTP_PROXY -u https_proxy -u http_proxy PATH=/tmp/minbin LOCALAPPDATA=/tmp/lad YTD_TOOL_MIRROR=http://127.0.0.1:8770 YTD_SCALE=1 /usr/bin/mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_inst.log 2>&1 &
sleep 6
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 1
import -window root -crop 1400x1100+0+0 shots/inst_1_prompt.png
xdotool search --name "first-time setup" | head -1 > /tmp/dlg; D=$(cat /tmp/dlg); echo dialog=$D
xdotool windowactivate $D 2>/dev/null; sleep 0.5; xdotool key Return
sleep 4; import -window root -crop 1400x1100+0+0 shots/fail_1.png; exit
sleep 4; import -window root -crop 1400x1100+0+0 shots/inst_3_progress.png
sleep 8; import -window root -crop 1400x1100+0+0 shots/inst_4_done.png
ls -la /tmp/lad/YTDownloader/tools
