#!/bin/bash
# Compiles the C# embedded in dist/Dawnloader.ps1 with Microsoft's compiler (Roslyn) against the
# .NET Framework 4.8 reference assemblies, with every warning treated as an error - the same rule
# Windows PowerShell's Add-Type uses. Needs mono and the Microsoft.Net.Compilers package in /tmp/roslyn.
set -e
cd "$(dirname "$0")/.."
python3 - <<'PY'
s=open('dist/Dawnloader.ps1',encoding='ascii').read().replace('\r\n','\n')
i=s.index("$Source = @'\n")+len("$Source = @'\n"); j=s.index("\n'@\n",i)
open('/tmp/shipped.cs','w').write(s[i:j])
PY
A=/usr/lib/mono/4.8-api
mono /tmp/roslyn/tools/csc.exe -nologo -langversion:5 -warnaserror+ -warn:4 -nostdlib -target:library \
  -r:$A/mscorlib.dll -r:$A/System.dll -r:$A/System.Core.dll -r:$A/System.Windows.Forms.dll -r:$A/System.Drawing.dll \
  -r:$A/System.Web.Extensions.dll -r:$A/System.IO.Compression.dll -out:/tmp/shipped.dll /tmp/shipped.cs
echo "OK - compiles cleanly the way Windows will compile it"
