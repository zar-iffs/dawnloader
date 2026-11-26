using System; using System.Collections.Generic; using System.Diagnostics; using System.Web.Script.Serialization; using YTD;
class E { static string Run(string args){ var p=new Process(); p.StartInfo=new ProcessStartInfo("yt-dlp",args){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true}; p.Start(); string o=p.StandardOutput.ReadToEnd(); p.WaitForExit(); return o.Trim(); }
 static void Main(){ int ok=0,bad=0;
  foreach(var spec in new[]{"v1","v2","tt","v1:other","v2:other"}){ string file=spec.Split(':')[0]; bool other=file=="tt"||spec.EndsWith("other");
   string fm=Run("--load-info-json /home/claude/sim/"+file+".info.json --print \"%(formats.:.{format_id,ext,vcodec,acodec,width,height,tbr,abr,filesize,filesize_approx,protocol})j\"");
   var arr=(object[])new JavaScriptSerializer().DeserializeObject(fm); var fs=new List<Fmt>();
   foreach(Dictionary<string,object> f in arr){ var x=new Fmt(); x.Id=(string)f["format_id"]; x.Ext=(string)f["ext"]; x.V=f.ContainsKey("vcodec")?(string)f["vcodec"]:"none"; x.A=f.ContainsKey("acodec")?(string)f["acodec"]:"none"; x.Proto=(string)f["protocol"]; x.H=f.ContainsKey("height")&&f["height"]!=null?Convert.ToInt32(f["height"]):0; int w=f.ContainsKey("width")&&f["width"]!=null?Convert.ToInt32(f["width"]):0; if(w>0&&x.H>0) x.H=Math.Min(w,x.H); x.Tbr=f.ContainsKey("tbr")?Convert.ToDouble(f["tbr"]):0; x.Abr=f.ContainsKey("abr")?Convert.ToDouble(f["abr"]):0; fs.Add(x);}
   foreach(var h in new[]{0,2160,1440,1080,720,480,360}) foreach(var cont in new[]{"mp4","mkv","webm"}) {
     var o=new Opts{Video=true,Height=h,Container=cont,H264First=other}; bool up=h>0&&h<=1080;
     string res=h>0?"res:"+h:"res"; string sort=cont=="mp4"?res+",vcodec:h264,acodec:aac":cont=="webm"?res+",ext:webm:webm":up?res+",vcodec:h264":res; if(other&&up&&cont!="webm") sort="vcodec:h264,"+res+(cont=="mp4"?",acodec:aac":"");
     string y=Run("--load-info-json /home/claude/sim/"+file+".info.json -f \"bv*+ba/b\" -S "+sort+" --merge-output-format "+cont+" --print format_id");
     var v=Est.PickVideo(fs,o); string mine=v.IsMuxed? v.Id : v.Id+"+"+Est.PickAudio(fs,(cont=="mp4")?"aac":"opus").Id;
     System.Func<string,string> n=z=>System.Text.RegularExpressions.Regex.Replace(z,@"-\d+(?=\+|$)",""); if(n(mine)==n(y)) ok++; else { bad++; Console.WriteLine("MISMATCH "+spec+" h="+h+" "+cont+" "+": est="+mine+" yt-dlp="+y); } }
   if(!other) foreach(var af in new[]{"mp3","m4a","opus","flac","wav","best"}){
     string src=af=="m4a"?"ba[ext=m4a]/ba/b":af=="opus"?"ba[acodec=opus]/ba/b":"ba/b";
     string y=Run("--load-info-json /home/claude/sim/"+file+".info.json -f \""+src+"\" --print format_id");
     var a=Est.PickAudio(fs, af=="m4a"?"aac":af=="opus"?"opus":""); if(a.Id==y) ok++; else { bad++; Console.WriteLine("MISMATCH audio "+af+": est="+a.Id+" yt-dlp="+y);} }
  }
  Console.WriteLine(ok+" match, "+bad+" mismatch"); } }
