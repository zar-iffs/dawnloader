#!/usr/bin/env python3
"""Builds dist/Dawnloader.ps1 = src/launcher.template.ps1 with src/ytd.cs embedded.
Rules enforced: ASCII only (use \\uXXXX in C#), no line starting with '@ inside the here-string, CRLF output."""
import sys, pathlib
root = pathlib.Path(__file__).resolve().parent.parent
cs = (root/'src/ytd.cs').read_text(encoding='utf-8').rstrip('\n')
tpl = (root/'src/launcher.template.ps1').read_text(encoding='utf-8')
out = tpl.replace('@@CSHARP@@', cs)
bad = [i for i,c in enumerate(out) if ord(c) > 127]
if bad:
    line = out.count('\n', 0, bad[0]) + 1
    sys.exit(f'ERROR: non-ASCII character at line {line} - use \\uXXXX escapes')
for n,l in enumerate(cs.split('\n'),1):
    if l.startswith("'@"):
        sys.exit(f"ERROR: ytd.cs line {n} starts with '@ (ends the PowerShell here-string)")
import re
m = re.search(r'const string AppVersion = "([\d.]+)"', cs)
if not m: sys.exit('ERROR: AppVersion not found in ytd.cs')
(root/'VERSION').write_text(m.group(1) + '\n')
dst = root/'dist/Dawnloader.ps1'
dst.write_bytes(out.replace('\n','\r\n').encode('ascii'))
print(f'built {dst} ({out.count(chr(10))+1} lines)')
