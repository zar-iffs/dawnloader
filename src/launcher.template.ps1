#Requires -Version 5.1
<#
  Dawnloader - local Windows app around yt-dlp + ffmpeg (YouTube, TikTok, Instagram, Facebook, X and 1000+ other sites,
  Google Drive incl. private files, direct video links).
  Launch with "Start Dawnloader.bat" (double-click). Nothing is uploaded anywhere; everything runs on this PC.

  First launch: if yt-dlp / ffmpeg / deno are missing, the app offers to download them (about 180 MB, no admin needed)
  into %LOCALAPPDATA%\Dawnloader\tools. Manual alternative (PowerShell):
                                 winget install yt-dlp.yt-dlp
                                 winget install Gyan.FFmpeg
                                 winget install DenoLand.Deno    (recommended - yt-dlp uses it for YouTube)
  The window itself is C# code below, compiled once by PowerShell and cached in %LOCALAPPDATA%\Dawnloader.
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing, System.Web.Extensions, System.IO.Compression
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$Source = @'
@@CSHARP@@
'@

try {
    $sha  = [System.Security.Cryptography.SHA256]::Create()
    $hash = -join ($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Source))[0..7] | ForEach-Object { $_.ToString('x2') })
    $cacheDir = Join-Path $env:LOCALAPPDATA 'Dawnloader'
    $dll = Join-Path $cacheDir "ytd-$hash.dll"
    if (-not (Test-Path $dll)) {
        New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
        Get-ChildItem -Path $cacheDir -Filter 'ytd-*.dll' -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
        $refs = @(
            [System.Diagnostics.Process].Assembly.Location,                       # System.dll
            [System.Windows.Forms.Form].Assembly.Location,
            [System.Drawing.Point].Assembly.Location,
            [System.Web.Script.Serialization.JavaScriptSerializer].Assembly.Location,
            [System.IO.Compression.ZipArchive].Assembly.Location
        )
        Add-Type -TypeDefinition $Source -ReferencedAssemblies $refs -OutputAssembly $dll -OutputType Library -IgnoreWarnings
    }
    Add-Type -Path $dll
    [YTD.Program]::Run($ScriptDir)
}
catch {
    [void][System.Windows.Forms.MessageBox]::Show("Dawnloader could not start:`n`n$($_.Exception.Message)", 'Dawnloader')
}
