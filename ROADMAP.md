# Roadmap

Done (v2026.11)
- Clip download - inline "Clip a part" box in step 2 (range slider + start/end times; follows the selected row; size estimate updates)
- Fix-it buttons in the Status column (Update yt-dlp / Use Firefox sign-in / Retry / Show in folder / Download again)
- Clipboard catcher ("Catch copied links" checkbox)
- Skip what you already have (download history per Video/Audio; playlists fetch only new videos) + Show in folder
- Five skins (Crimson, Midnight, Glass, Sunrise, Forest) - palette button in the header
- About box with responsible-use notice, GitHub credit, Terms of use and Privacy; friendly message for DRM-protected sites; README disclaimer
- Sign-in picker: icon buttons for the browsers installed on the PC (Off / Firefox / Chrome / Edge / Brave / cookies.txt)
- Header: icons of popular sites (their own favicons, cached) + "+ 1000 more sites" list read from yt-dlp
- Window fits short screens (Download button was hidden under the list)

Still to do
- Small wins: subtitle language picker, shutdown when finished, speed limit, interface language switch
- Check Creative Commons badge for YouTube (verify yt-dlp reports the licence)

Rejected
- PDF / document support (Google Docs/Slides export, PDF page picker, web page to PDF): dropped - Google Docs and Slides already have File > Download > PDF. Earlier working version kept in docs/archive/ytd.with-pdf.cs
- "Fit to size" (not needed)
- File sharing / expiring links (cost, legal and direction concerns)

Done
- Renamed to Dawnloader; new sunrise-and-arrow logo (header + window icon)

Copyright awareness (decided)
- About box / permanent footer line: only download content you own, that is licensed for reuse, or that you have permission to save
- Friendly message for DRM-protected sites ("copy protection, cannot be downloaded")
- README disclaimer (present; expand when publishing)
- Not doing: per-item copyright warnings, first-run consent popup

Open
- Check the name for trademark/search clashes before publishing
- GitHub publishing: screenshot in README, release zip of .ps1 + .bat
