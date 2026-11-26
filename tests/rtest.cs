using System; using System.Collections.Generic; using System.Windows.Forms; using System.Diagnostics;
class X { [STAThread] static void Main() {
  var f = new Form(); int n=0, ok=0; var sw=Stopwatch.StartNew();
  f.Shown += (s,e) => { Action go=null; go = () => { var r=new YTD.Runner(f); var sb=new System.Text.StringBuilder();
      r.Line += l => sb.AppendLine(l);
      r.Exited += c => { n++; if (sb.ToString().Trim()=="2026.10.26") ok++; else Console.WriteLine("BAD run "+n+": ["+sb+"] code="+c);
                         if (n<15) go(); else { Console.WriteLine(ok+"/"+n+" ok, avg "+sw.ElapsedMilliseconds/n+" ms"); f.Close(); } };
      r.Start("/home/claude/app/fakebin/yt-dlp", new List<string>{"--version"}, null); }; go(); };
  Application.Run(f); } }
