#!/bin/bash
export DISPLAY=:99
pkill -f "Xvfb :99"; sleep 0.5; Xvfb :99 -screen 0 2000x1300x24 >/dev/null 2>&1 & sleep 1
rm -rf /tmp/siteaudio; printf 'OutDir=/tmp/siteaudio\n' > /home/claude/app/ytdl-settings.txt
YTD_SCALE=1 mono /home/claude/app/ytd.exe /home/claude/app > /tmp/mono_sites.log 2>&1 &
sleep 6
WID=$(xdotool search --name "^YT Downloader$" | head -1); xdotool windowmove $WID 0 0; sleep 1
xdotool mousemove 60 150 click 1
xdotool type --delay 3 "https://www.tiktok.com/@kenaliubatanda/video/7690730713057922356"; xdotool key Return
xdotool type --delay 3 "https://www.facebook.com/watch/?v=10153231379946729"; xdotool key Return
xdotool type --delay 3 "https://www.instagram.com/reel/C8Y9Z0Xy1Ab/"; sleep 1; xdotool mousemove 340 297 click 1
sleep 35; import -window root -crop 1400x1100+0+0 shots/sites_1.png
xdotool mousemove 813 726 click 1
sleep 45; import -window root -crop 1400x1100+0+0 shots/sites_2.png
ls -la /tmp/siteaudio
