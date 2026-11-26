// Dawnloader - WinForms UI around yt-dlp. C# 5 only (compiled by Windows PowerShell 5.1 Add-Type).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace YTD
{
    public static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        public static void Run(string scriptDir)
        {
            try { if (Environment.OSVersion.Platform == PlatformID.Win32NT) SetProcessDPIAware(); } catch { }
            try { Application.EnableVisualStyles(); } catch { }
            try { Application.SetCompatibleTextRenderingDefault(false); } catch { }
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) T.S = g.DpiX / 96f;
            string ov = Environment.GetEnvironmentVariable("YTD_SCALE");   // for testing other display scales
            if (!string.IsNullOrEmpty(ov)) T.S = float.Parse(ov, CultureInfo.InvariantCulture);
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) T.FontName = "DejaVu Sans"; // wider than Segoe UI: worst case for clipping
            T.Init();
            string db = Environment.GetEnvironmentVariable("YTD_DRIVE_BASE"); if (!string.IsNullOrEmpty(db)) { Web.DriveBase = db; Web.ThumbBase = db; }
            string dcb = Environment.GetEnvironmentVariable("YTD_DOCS_BASE"); if (!string.IsNullOrEmpty(dcb)) Web.DocsBase = dcb;
            Application.Run(new MainForm(scriptDir));
        }

        [STAThread]
        public static void Main(string[] args) { Run(args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory); }
    }

    // ------------------------------------------------------------------ theme + skins
    public interface ISurface { void PaintSurface(Graphics g); }

    public class Skin
    {
        public string Name;
        public bool Dark, Glass;
        public Color Bg, Bg2, Card, Border, Line, Text, Muted, Hint, Accent, AccentDark, AccentHover, AccentTint, AccentOff,
                     Ok, Warn, Bad, Soft, Hover, Down, BorderHover, Input, HeadA, HeadB, HeadText, HeadSub, HeadBtn,
                     LogoBg, LogoFg, LogoCut, Track, DangerBg, DangerHover, DangerBorder, TextOff, ColHead;
    }

    public static class T
    {
        public static float S = 1f;
        public static string FontName = "Segoe UI";
        public static int P(float v) { return (int)Math.Round(v * S); }
        public static Font F(float px, bool bold) { return new Font(FontName, px * S, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel); }

        public static Font Body, BodyBold, Small, Title, CardTitle, Big, Header, HeaderSub, TileTitle;
        public static Skin Cur;
        public static Color Bg { get { return Cur.Bg; } }
        public static Color Card { get { return Cur.Card; } }
        public static Color Border { get { return Cur.Border; } }
        public static Color Line { get { return Cur.Line; } }
        public static Color Text { get { return Cur.Text; } }
        public static Color Muted { get { return Cur.Muted; } }
        public static Color Hint { get { return Cur.Hint; } }
        public static Color Accent { get { return Cur.Accent; } }
        public static Color AccentDark { get { return Cur.AccentDark; } }
        public static Color AccentTint { get { return Cur.AccentTint; } }
        public static Color Ok { get { return Cur.Ok; } }
        public static Color Warn { get { return Cur.Warn; } }
        public static Color Bad { get { return Cur.Bad; } }
        public static Color Soft { get { return Cur.Soft; } }
        public static Color Input { get { return Cur.Input; } }

        static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        static Color A(int a, int r, int g, int b) { return Color.FromArgb(a, r, g, b); }

        static Skin LightBase(string name)
        {
            var s = new Skin(); s.Name = name;
            s.Card = Color.White; s.Input = Color.White; s.DangerBg = Color.White; s.DangerHover = C(254, 232, 233); s.DangerBorder = C(240, 170, 175);
            s.HeadText = Color.White; s.HeadBtn = Color.White; s.LogoBg = A(55, 255, 255, 255); s.LogoFg = Color.White;
            s.Ok = C(22, 150, 74); s.Warn = C(205, 120, 6); s.Bad = C(200, 30, 40); s.TextOff = C(170, 175, 185);
            return s;
        }

        public static readonly string[] SkinNames = { "Crimson", "Midnight", "Glass", "Sunrise", "Forest" };

        public static Skin MakeSkin(string name)
        {
            Skin s;
            switch (name)
            {
                case "Midnight":
                    s = new Skin(); s.Name = name; s.Dark = true;
                    s.Bg = s.Bg2 = C(17, 19, 24); s.Card = C(27, 30, 37); s.Border = s.Line = C(46, 51, 62);
                    s.Text = C(230, 233, 240); s.Muted = C(150, 158, 174); s.Hint = C(105, 112, 128);
                    s.Accent = C(240, 72, 92); s.AccentDark = C(200, 40, 60); s.AccentHover = C(250, 95, 112); s.AccentTint = C(58, 30, 38); s.AccentOff = C(110, 50, 60);
                    s.Ok = C(80, 205, 125); s.Warn = C(240, 170, 60); s.Bad = C(255, 110, 120);
                    s.Soft = C(40, 44, 54); s.Hover = C(36, 40, 49); s.Down = C(48, 53, 64); s.BorderHover = C(74, 81, 96); s.Input = C(21, 24, 30);
                    s.HeadA = C(42, 26, 36); s.HeadB = C(19, 21, 27); s.HeadText = C(242, 244, 248); s.HeadSub = C(160, 168, 184); s.HeadBtn = C(230, 233, 240);
                    s.LogoBg = s.Accent; s.LogoFg = Color.White; s.LogoCut = s.Accent;
                    s.Track = C(44, 49, 60); s.DangerBg = C(27, 30, 37); s.DangerHover = C(60, 30, 36); s.DangerBorder = C(120, 50, 60);
                    s.TextOff = C(90, 96, 110); s.ColHead = C(31, 35, 43);
                    break;
                case "Glass":
                    s = new Skin(); s.Name = name; s.Glass = true;
                    s.Bg = C(188, 214, 255); s.Bg2 = C(238, 212, 250);
                    s.Card = A(150, 255, 255, 255); s.Border = A(215, 255, 255, 255); s.Line = C(206, 212, 240);
                    s.Text = C(28, 34, 62); s.Muted = C(80, 88, 122); s.Hint = C(130, 138, 165);
                    s.Accent = C(88, 86, 214); s.AccentDark = C(64, 62, 180); s.AccentHover = C(108, 106, 230); s.AccentTint = A(190, 236, 236, 255); s.AccentOff = C(170, 170, 230);
                    s.Ok = C(20, 135, 78); s.Warn = C(176, 102, 0); s.Bad = C(196, 36, 60);
                    s.Soft = A(150, 255, 255, 255); s.Hover = A(205, 255, 255, 255); s.Down = A(235, 240, 242, 255); s.BorderHover = C(160, 168, 220); s.Input = C(251, 251, 255);
                    s.HeadA = A(120, 255, 255, 255); s.HeadB = A(55, 255, 255, 255); s.HeadText = C(28, 34, 62); s.HeadSub = C(70, 78, 112); s.HeadBtn = C(28, 34, 62);
                    s.LogoBg = s.Accent; s.LogoFg = Color.White; s.LogoCut = s.Accent;
                    s.Track = A(150, 255, 255, 255); s.DangerBg = A(200, 255, 255, 255); s.DangerHover = A(235, 255, 236, 240); s.DangerBorder = C(236, 176, 190);
                    s.TextOff = C(150, 156, 180); s.ColHead = C(241, 242, 253);
                    break;
                case "Sunrise":
                    s = LightBase(name);
                    s.Bg = s.Bg2 = C(250, 246, 240); s.Border = s.Line = C(236, 226, 214);
                    s.Text = C(46, 32, 24); s.Muted = C(128, 108, 94); s.Hint = C(170, 150, 136);
                    s.Accent = C(232, 89, 12); s.AccentDark = C(196, 70, 6); s.AccentHover = C(245, 110, 30); s.AccentTint = C(255, 240, 228); s.AccentOff = C(245, 180, 140);
                    s.Soft = C(246, 238, 230); s.Hover = C(252, 248, 243); s.Down = C(242, 234, 225); s.BorderHover = C(214, 196, 180);
                    s.HeadA = C(247, 103, 7); s.HeadB = C(214, 51, 108); s.HeadSub = C(255, 232, 220); s.LogoCut = C(222, 72, 50);
                    s.Track = C(238, 228, 218); s.ColHead = C(252, 248, 243);
                    break;
                case "Forest":
                    s = LightBase(name);
                    s.Bg = s.Bg2 = C(240, 245, 241); s.Border = s.Line = C(216, 229, 220);
                    s.Text = C(24, 40, 30); s.Muted = C(92, 112, 100); s.Hint = C(140, 160, 148);
                    s.Accent = C(43, 138, 62); s.AccentDark = C(30, 110, 48); s.AccentHover = C(55, 158, 76); s.AccentTint = C(230, 245, 233); s.AccentOff = C(150, 200, 160);
                    s.Soft = C(232, 240, 234); s.Hover = C(246, 250, 247); s.Down = C(226, 237, 229); s.BorderHover = C(170, 196, 178);
                    s.HeadA = C(47, 140, 66); s.HeadB = C(12, 74, 44); s.HeadSub = C(214, 240, 222); s.LogoCut = C(30, 110, 48);
                    s.Track = C(222, 232, 225); s.ColHead = C(246, 250, 247);
                    break;
                default:
                    s = LightBase("Crimson");
                    s.Bg = s.Bg2 = C(243, 244, 247); s.Border = s.Line = C(226, 228, 234);
                    s.Text = C(28, 32, 42); s.Muted = C(105, 112, 128); s.Hint = C(150, 156, 168);
                    s.Accent = C(220, 38, 58); s.AccentDark = C(176, 22, 44); s.AccentHover = C(235, 50, 70); s.AccentTint = C(254, 236, 238); s.AccentOff = C(236, 160, 168);
                    s.Soft = C(238, 240, 244); s.Hover = C(246, 247, 250); s.Down = C(232, 234, 239); s.BorderHover = C(190, 194, 204);
                    s.HeadA = C(214, 32, 56); s.HeadB = C(120, 14, 38); s.HeadSub = C(255, 225, 230); s.LogoCut = C(190, 26, 50);
                    s.Track = C(226, 229, 235); s.ColHead = C(248, 249, 251);
                    break;
            }
            return s;
        }

        public static void Init()
        {
            if (Cur == null) Cur = MakeSkin("Crimson");
            Body = F(13.5f, false); BodyBold = F(13.5f, true); Small = F(12f, false);
            CardTitle = F(15f, true); Big = F(17f, true); TileTitle = F(15f, true);
            Header = F(22f, true); HeaderSub = F(13f, false); Title = F(14f, true);
        }

        // Paints whatever is behind control c (the nearest ISurface ancestor: card, header or window), in c's coordinates.
        public static void PaintUnder(Graphics g, Control c)
        {
            Control p = c.Parent; int dx = c.Left, dy = c.Top;
            while (p != null && !(p is ISurface)) { dx += p.Left; dy += p.Top; p = p.Parent; }
            if (p == null) { using (var b = new SolidBrush(Bg)) g.FillRectangle(b, 0, 0, c.Width, c.Height); return; }
            var st = g.Save(); g.TranslateTransform(-dx, -dy); ((ISurface)p).PaintSurface(g); g.Restore(st);
        }

        // Opaque colour that matches what a (possibly translucent) colour looks like over the card.
        public static Color Solid(Color c)
        {
            if (c.A == 255) return c;
            Color under = Cur.Glass ? Blend(Cur.Bg, Cur.Bg2, 0.5f) : Cur.Bg;
            float a = c.A / 255f;
            return Color.FromArgb((int)(c.R * a + under.R * (1 - a)), (int)(c.G * a + under.G * (1 - a)), (int)(c.B * a + under.B * (1 - a)));
        }
        public static Color Blend(Color x, Color y, float t) { return Color.FromArgb((int)(x.R + (y.R - x.R) * t), (int)(x.G + (y.G - x.G) * t), (int)(x.B + (y.B - x.B) * t)); }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = rad * 2;
            if (d <= 0.5f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static string Bytes(double b)
        {
            if (b < 0) return "?";
            if (b >= 1e9) return (b / 1073741824.0).ToString(b >= 1e11 ? "0" : "0.0", CultureInfo.InvariantCulture) + " GB";
            if (b >= 1e6) return (b / 1048576.0).ToString("0", CultureInfo.InvariantCulture) + " MB";
            if (b >= 1e3) return (b / 1024.0).ToString("0", CultureInfo.InvariantCulture) + " KB";
            return ((int)b) + " B";
        }

        public static string Dur(double s)
        {
            if (s <= 0) return "";
            var t = TimeSpan.FromSeconds(Math.Round(s));
            return t.TotalHours >= 1 ? string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds)
                                     : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
        }

        // "1:20", "01:02:03", "75", "1m20s" -> seconds; -1 if invalid
        public static double ParseTime(string s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            if (s == "") return -1;
            var m = Regex.Match(s, @"^(?:(\d+)h)?\s*(?:(\d+)m)?\s*(?:(\d+(?:\.\d+)?)s)?$");
            if (m.Success && m.Value != "" && (m.Groups[1].Success || m.Groups[2].Success || m.Groups[3].Success))
                return (m.Groups[1].Success ? int.Parse(m.Groups[1].Value) * 3600 : 0) + (m.Groups[2].Success ? int.Parse(m.Groups[2].Value) * 60 : 0) +
                       (m.Groups[3].Success ? double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0);
            var parts = s.Split(':'); if (parts.Length > 3) return -1;
            double tot = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                double v; if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v < 0) return -1;
                if (i > 0 && v >= 60) return -1;
                tot = tot * 60 + v;
            }
            return tot;
        }
    }

    // ------------------------------------------------------------------ vector icons (24x24 grid)
    public static class Icons
    {
        public static void Draw(Graphics g, string name, RectangleF b, Color c)
        {
            if (string.IsNullOrEmpty(name)) return;
            float k = b.Width / 24f;
            Func<float, float, PointF> P = (x, y) => new PointF(b.X + x * k, b.Y + y * k);
            Func<float, float, float, float, RectangleF> R = (x, y, w, h) => new RectangleF(b.X + x * k, b.Y + y * k, w * k, h * k);
            var oldMode = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(c, Math.Max(1.4f, 2.1f * k)))
            using (var br = new SolidBrush(c))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                switch (name)
                {
                    case "download":
                        g.DrawLine(pen, P(12, 3.5f), P(12, 15));
                        g.DrawLines(pen, new[] { P(6.5f, 9.5f), P(12, 15), P(17.5f, 9.5f) });
                        g.DrawLine(pen, P(4.5f, 20), P(19.5f, 20));
                        break;
                    case "paste":
                        using (var p = T.Round(R(5, 4.5f, 14, 16.5f), 2.2f * k)) g.DrawPath(pen, p);
                        using (var p = T.Round(R(8.5f, 2.5f, 7, 4.5f), 1.4f * k)) g.FillPath(br, p);
                        g.DrawLine(pen, P(9, 11.5f), P(15, 11.5f));
                        g.DrawLine(pen, P(9, 15.5f), P(13, 15.5f));
                        break;
                    case "clear":
                        g.DrawLine(pen, P(7, 7), P(17, 17)); g.DrawLine(pen, P(17, 7), P(7, 17));
                        break;
                    case "folder":
                        g.DrawPolygon(pen, new[] { P(3, 6), P(9.5f, 6), P(11.5f, 8.5f), P(21, 8.5f), P(21, 19), P(3, 19) });
                        break;
                    case "open":
                        g.DrawPolygon(pen, new[] { P(3, 6), P(9.5f, 6), P(11.5f, 8.5f), P(19, 8.5f), P(19, 11) , P(7, 11), P(3, 19) });
                        g.DrawLines(pen, new[] { P(3, 19), P(7, 11), P(22, 11), P(18, 19), P(3, 19) });
                        break;
                    case "refresh":
                        g.DrawArc(pen, R(4.5f, 4.5f, 15, 15), -50, 290);
                        g.DrawLines(pen, new[] { P(20.2f, 3.5f), P(19.3f, 8.6f), P(14.4f, 7.6f) });
                        break;
                    case "video":
                        using (var p = T.Round(R(2.5f, 5, 19, 14), 3.2f * k)) g.DrawPath(pen, p);
                        g.FillPolygon(br, new[] { P(10, 8.8f), P(15.6f, 12), P(10, 15.2f) });
                        break;
                    case "audio":
                        g.DrawLine(pen, P(9.5f, 17), P(9.5f, 5.5f));
                        g.DrawLine(pen, P(19.5f, 15), P(19.5f, 3.5f));
                        using (var p2 = new Pen(c, 3.2f * k)) g.DrawLine(p2, P(9.5f, 5.2f), P(19.5f, 3.2f));
                        g.FillEllipse(br, R(3.5f, 14.2f, 7, 5.6f));
                        g.FillEllipse(br, R(13.5f, 12.2f, 7, 5.6f));
                        break;
                    case "stop":
                        using (var p = T.Round(R(6, 6, 12, 12), 2.2f * k)) g.FillPath(br, p);
                        break;
                    case "log":
                        g.DrawLine(pen, P(5, 7), P(19, 7)); g.DrawLine(pen, P(5, 12), P(19, 12)); g.DrawLine(pen, P(5, 17), P(14, 17));
                        break;
                    case "check":
                        g.DrawLines(pen, new[] { P(5, 12.5f), P(10, 17.5f), P(19, 7) });
                        break;
                    case "info":
                        g.DrawEllipse(pen, R(3, 3, 18, 18));
                        g.DrawLine(pen, P(12, 11), P(12, 16.5f));
                        g.FillEllipse(br, R(10.7f, 6.4f, 2.6f, 2.6f));
                        break;
                    case "play":
                        g.FillPolygon(br, new[] { P(8, 5), P(19, 12), P(8, 19) });
                        break;
                    case "palette":
                        g.DrawEllipse(pen, R(3, 3, 18, 18));
                        g.FillEllipse(br, R(7, 7.5f, 3, 3)); g.FillEllipse(br, R(11, 5.5f, 3, 3)); g.FillEllipse(br, R(15, 8, 3, 3));
                        g.FillEllipse(br, R(13, 14, 4.5f, 4.5f));
                        break;
                    case "doc":
                        g.DrawPolygon(pen, new[] { P(6, 3), P(14, 3), P(19, 8), P(19, 21), P(6, 21) });
                        g.DrawLines(pen, new[] { P(14, 3), P(14, 8), P(19, 8) });
                        g.DrawLine(pen, P(9, 13), P(16, 13)); g.DrawLine(pen, P(9, 17), P(14, 17));
                        break;
                    case "cut":
                        g.DrawEllipse(pen, R(3.5f, 14.5f, 5.5f, 5.5f)); g.DrawEllipse(pen, R(15, 14.5f, 5.5f, 5.5f));
                        g.DrawLine(pen, P(8.2f, 15.5f), P(18, 3.5f)); g.DrawLine(pen, P(15.8f, 15.5f), P(6, 3.5f));
                        break;
                    case "list":
                        g.FillEllipse(br, R(3.5f, 5.5f, 3, 3)); g.FillEllipse(br, R(3.5f, 10.5f, 3, 3)); g.FillEllipse(br, R(3.5f, 15.5f, 3, 3));
                        g.DrawLine(pen, P(9.5f, 7), P(20, 7)); g.DrawLine(pen, P(9.5f, 12), P(20, 12)); g.DrawLine(pen, P(9.5f, 17), P(20, 17));
                        break;
                }
            }
            g.SmoothingMode = oldMode;
        }

        public static void DrawLogo(Graphics g, RectangleF b, Color sun, Color cut)
        {
            float k = b.Width / 24f;
            var oldMode = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var br = new SolidBrush(sun))
            using (var pen = new Pen(sun, Math.Max(1.4f, 1.9f * k)))
            using (var cp = new Pen(cut, Math.Max(1.3f, 1.8f * k)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                cp.StartCap = LineCap.Round; cp.EndCap = LineCap.Round; cp.LineJoin = LineJoin.Round;
                Func<float, float, PointF> P = (x, y) => new PointF(b.X + x * k, b.Y + y * k);
                g.FillPie(br, b.X + 3.5f * k, b.Y + 10.5f * k, 17f * k, 17f * k, 180, 180);
                g.DrawLine(pen, P(2f, 19f), P(22f, 19f));
                g.DrawLine(pen, P(12, 8.3f), P(12, 5f));
                g.DrawLine(pen, P(4.9f, 11.9f), P(2.8f, 9.6f));
                g.DrawLine(pen, P(19.1f, 11.9f), P(21.2f, 9.6f));
                g.DrawLine(cp, P(12, 12.6f), P(12, 17f));
                g.DrawLines(cp, new[] { P(9.4f, 14.6f), P(12, 17.2f), P(14.6f, 14.6f) });
            }
            g.SmoothingMode = oldMode;
        }

        public static Icon AppIcon()
        {
            try
            {
                var bmp = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = T.Round(new RectangleF(1, 1, 30, 30), 8)) using (var br = new LinearGradientBrush(new RectangleF(0, 0, 32, 32), Color.FromArgb(214, 32, 56), Color.FromArgb(246, 140, 52), LinearGradientMode.Vertical)) g.FillPath(br, p);
                    DrawLogo(g, new RectangleF(4, 4, 24, 24), Color.White, Color.FromArgb(226, 70, 56));
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
            catch { return null; }
        }
    }

    // ------------------------------------------------------------------ custom controls
    public class FlatButton : Control
    {
        public string IconName; public int Kind;   // 0 primary, 1 secondary, 2 danger, 3 on-header, 4 subtle
        bool hover, down;
        public FlatButton(string text, string icon, int kind)
        {
            Text = text; IconName = icon; Kind = kind;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand; TabStop = true; Font = T.BodyBold;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) OnClick(EventArgs.Empty); base.OnKeyDown(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }

        public int PreferredWidth(int height)
        {
            if (string.IsNullOrEmpty(Text)) return height;
            int tw = TextRenderer.MeasureText(Text ?? "", Font).Width;
            int icon = string.IsNullOrEmpty(IconName) ? 0 : (int)(height * 0.46f) + T.P(8);
            return tw + icon + T.P(32);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur;
            T.PaintUnder(g, this);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color bg, fg, border;
            switch (Kind)
            {
                case 0: bg = down ? k.AccentDark : (hover ? k.AccentHover : k.Accent); fg = Color.White; border = bg; break;
                case 2: bg = down ? k.DangerHover : (hover ? k.DangerHover : k.DangerBg); fg = k.Bad; border = k.DangerBorder; break;
                case 3: bg = Color.FromArgb(hover ? 70 : 38, k.HeadBtn); fg = k.HeadBtn; border = Color.FromArgb(105, k.HeadBtn); break;
                case 4: bg = hover ? k.Soft : Color.Transparent; fg = k.Muted; border = hover ? k.Border : Color.Transparent; break;
                default: bg = down ? k.Down : (hover ? k.Hover : k.Input); fg = k.Text; border = hover ? k.BorderHover : k.Border; break;
            }
            if (!Enabled)
            {
                if (Kind == 0) { bg = k.AccentOff; border = bg; fg = Color.White; }
                else { fg = k.TextOff; }
            }
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var p = T.Round(r, T.P(8)))
            {
                if (bg.A > 0) using (var b = new SolidBrush(bg)) g.FillPath(b, p);
                if (border.A > 0) using (var pen = new Pen(border, 1f)) g.DrawPath(pen, p);
                if (Focused && ShowFocusCues) using (var pen = new Pen(Color.FromArgb(120, k.Accent), 2f)) g.DrawPath(pen, p);
            }
            Size ts = TextRenderer.MeasureText(Text ?? "", Font);
            float iconSz = string.IsNullOrEmpty(IconName) ? 0 : Math.Min(Height * (string.IsNullOrEmpty(Text) ? 0.56f : 0.46f), T.P(26));
            float gap = iconSz > 0 && !string.IsNullOrEmpty(Text) ? T.P(8) : 0;
            float total = iconSz + gap + (string.IsNullOrEmpty(Text) ? 0 : ts.Width);
            float x = (Width - total) / 2f;
            if (iconSz > 0) Icons.Draw(g, IconName, new RectangleF(x, (Height - iconSz) / 2f, iconSz, iconSz), fg);
            if (!string.IsNullOrEmpty(Text))
                TextRenderer.DrawText(g, Text, Font, new Rectangle((int)(x + iconSz + gap), 0, ts.Width + 2, Height), fg,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    public class Tile : Control
    {
        public string Title, Sub, IconName; bool on, hover;
        public event EventHandler Picked;
        public Tile(string title, string sub, string icon)
        {
            Title = title; Sub = sub; IconName = icon;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand; TabStop = true;
        }
        public bool Checked { get { return on; } set { on = value; Invalidate(); } }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnClick(EventArgs e) { if (Picked != null) Picked(this, e); base.OnClick(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) OnClick(EventArgs.Empty); base.OnKeyDown(e); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur; T.PaintUnder(g, this);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(1, 1, Width - 2.5f, Height - 2.5f);
            Color bg = on ? k.AccentTint : (hover ? k.Hover : k.Input);
            Color bd = on ? k.Accent : (hover ? k.BorderHover : k.Border);
            using (var p = T.Round(r, T.P(10)))
            {
                using (var b = new SolidBrush(bg)) g.FillPath(b, p);
                using (var pen = new Pen(bd, on ? 2f * Math.Max(1f, T.S) : 1f)) g.DrawPath(pen, p);
            }
            float c = Height * 0.6f; float cx = T.P(11), cy = (Height - c) / 2f;
            using (var b = new SolidBrush(on ? k.Accent : k.Soft)) g.FillEllipse(b, cx, cy, c, c);
            Icons.Draw(g, IconName, new RectangleF(cx + c * 0.2f, cy + c * 0.2f, c * 0.6f, c * 0.6f), on ? Color.White : k.Muted);
            int tx = (int)(cx + c + T.P(10));
            Color tc = Enabled ? k.Text : k.Muted;
            int th = T.TileTitle.Height, sh = T.Small.Height, top = (Height - th - sh - T.P(2)) / 2;
            TextRenderer.DrawText(g, Title, T.TileTitle, new Point(tx, top), tc, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, Sub, T.Small, new Point(tx, top + th + T.P(2)), k.Muted, TextFormatFlags.NoPadding);
        }
        public int PreferredW()
        {
            float c = Height * 0.6f;
            int w = Math.Max(TextRenderer.MeasureText(Title, T.TileTitle).Width, TextRenderer.MeasureText(Sub, T.Small).Width);
            return (int)(T.P(11) + c + T.P(10) + w + T.P(14));
        }
    }

    public class Card : Panel, ISurface
    {
        public string Title, Step;
        public Card(string step, string title)
        {
            Step = step; Title = title; BackColor = Color.Transparent;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }
        public int ContentTop { get { return T.P(46); } }
        public void PaintSurface(Graphics g)
        {
            T.PaintUnder(g, this);
            var sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = T.Round(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), T.P(12)))
            {
                using (var b = new SolidBrush(T.Card)) g.FillPath(b, p);
                using (var pen = new Pen(T.Border)) g.DrawPath(pen, p);
            }
            g.SmoothingMode = sm;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; PaintSurface(g); g.SmoothingMode = SmoothingMode.AntiAlias;
            float d = T.P(24);
            using (var b = new SolidBrush(T.Accent)) g.FillEllipse(b, T.P(16), T.P(14), d, d);
            TextRenderer.DrawText(g, Step, T.BodyBold, new Rectangle(T.P(16), T.P(14), (int)d, (int)d), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, Title, T.CardTitle, new Point(T.P(48), T.P(14) + ((int)d - T.CardTitle.Height) / 2), T.Text, TextFormatFlags.NoPadding);
        }
    }

    public class HeaderPanel : Panel, ISurface
    {
        public HeaderPanel()
        {
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }
        public void PaintSurface(Graphics g)
        {
            var k = T.Cur; var r = new Rectangle(0, 0, Width, Height);
            if (k.HeadA.A < 255) T.PaintUnder(g, this);
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), k.HeadA, k.HeadB, LinearGradientMode.Horizontal)) g.FillRectangle(br, r);
            if (k.Glass) using (var pen = new Pen(Color.FromArgb(200, 255, 255, 255))) g.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur; PaintSurface(g);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float d = T.P(40);
            using (var b = new SolidBrush(k.LogoBg)) g.FillEllipse(b, T.P(20), (Height - d) / 2, d, d);
            Icons.DrawLogo(g, new RectangleF(T.P(25), (Height - d) / 2 + T.P(6), T.P(30), T.P(30)), k.LogoFg, k.LogoCut);
        }
    }

    public class Bar : Control
    {
        double v;
        public Bar() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); }
        public double Value { get { return v; } set { v = Math.Max(0, Math.Min(1, value)); Invalidate(); } }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; T.PaintUnder(g, this); g.SmoothingMode = SmoothingMode.AntiAlias;
            float rad = Height / 2f;
            using (var p = T.Round(new RectangleF(0, 0, Width - 1, Height - 1), rad)) using (var b = new SolidBrush(T.Cur.Track)) g.FillPath(b, p);
            if (v > 0.001)
                using (var p = T.Round(new RectangleF(0, 0, Math.Max(Height, (float)((Width - 1) * v)), Height - 1), rad)) using (var b = new SolidBrush(T.Accent)) g.FillPath(b, p);
        }
    }

    // Menu colours that follow the skin
    public class SkinColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return T.Solid(T.Cur.Input); } }
        public override Color ImageMarginGradientBegin { get { return T.Solid(T.Cur.Input); } }
        public override Color ImageMarginGradientMiddle { get { return T.Solid(T.Cur.Input); } }
        public override Color ImageMarginGradientEnd { get { return T.Solid(T.Cur.Input); } }
        public override Color MenuBorder { get { return T.Solid(T.Cur.BorderHover); } }
        public override Color MenuItemBorder { get { return T.Solid(T.Cur.AccentTint); } }
        public override Color MenuItemSelected { get { return T.Solid(T.Cur.AccentTint); } }
        public override Color SeparatorDark { get { return T.Cur.Line; } }
        public override Color SeparatorLight { get { return T.Cur.Line; } }
        public override Color CheckBackground { get { return T.Solid(T.Cur.AccentTint); } }
        public override Color CheckSelectedBackground { get { return T.Solid(T.Cur.AccentTint); } }
        public override Color CheckPressedBackground { get { return T.Solid(T.Cur.AccentTint); } }
    }
    public class SkinRenderer : ToolStripProfessionalRenderer
    {
        public SkinRenderer() : base(new SkinColors()) { RoundedEdges = false; }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { e.TextColor = e.Item.Enabled ? T.Text : T.Cur.TextOff; base.OnRenderItemText(e); }
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var r = e.ImageRectangle; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Icons.Draw(e.Graphics, "check", new RectangleF(r.X, r.Y, r.Width, r.Height), T.Accent);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor = T.Muted; base.OnRenderArrow(e); }
    }

    // ------------------------------------------------------------------ process runner (events delivered on UI thread)
    public class Runner
    {
        Process p; Control ui; public bool Killed;
        public event Action<string> Line;
        public event Action<int> Exited;
        public Runner(Control ui) { this.ui = ui; }

        public void Start(string exe, List<string> args, string pathEnv)
        {
            var psi = new ProcessStartInfo(exe);
            var sb = new StringBuilder();
            foreach (var a in args) { if (sb.Length > 0) sb.Append(' '); sb.Append(Quote(a)); }
            psi.Arguments = sb.ToString();
            psi.UseShellExecute = false; psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8; psi.StandardErrorEncoding = Encoding.UTF8;
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
            if (pathEnv != null) psi.EnvironmentVariables["PATH"] = pathEnv;
            p = new Process(); p.StartInfo = psi;
            var outEof = new ManualResetEvent(false); var errEof = new ManualResetEvent(false);
            p.OutputDataReceived += (s, e) => { if (e.Data != null) Post(e.Data); else outEof.Set(); };
            p.ErrorDataReceived += (s, e) => { if (e.Data != null) Post(e.Data); else errEof.Set(); };
            p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
            var proc = p;
            var th = new Thread(() =>
            {
                int code = -1;
                try { proc.WaitForExit(); code = proc.ExitCode; } catch { }
                outEof.WaitOne(10000); errEof.WaitOne(10000);   // all lines are posted before Exited
                try { ui.BeginInvoke((MethodInvoker)delegate { if (Exited != null) Exited(code); }); } catch { }
            });
            th.IsBackground = true; th.Start();
        }

        void Post(string s) { try { ui.BeginInvoke((MethodInvoker)delegate { if (Line != null) Line(s); }); } catch { } }

        public void Kill()
        {
            Killed = true;
            try
            {
                if (p == null || p.HasExited) return;
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var k = new ProcessStartInfo("taskkill", "/PID " + p.Id + " /T /F");   // also stops ffmpeg children
                    k.CreateNoWindow = true; k.UseShellExecute = false;
                    Process.Start(k).WaitForExit(5000);
                }
                else p.Kill();
            }
            catch { }
        }

        public static string Quote(string s)
        {
            if (s.Length > 0 && s.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return s;
            s = Regex.Replace(s, @"(\\*)""", "$1$1\\\"");
            s = Regex.Replace(s, @"(\\+)$", "$1$1");
            return "\"" + s + "\"";
        }
    }

    // ------------------------------------------------------------------ data
    public class Fmt
    {
        public string Id = "", Ext = "", V = "none", A = "none", Proto = "", Url = "";
        public int H; public double Tbr, Abr, Size;
        public bool IsVideoOnly { get { return V != "none" && A == "none" && H > 0 && Proto != "mhtml"; } }
        public bool IsAudioOnly { get { return A != "none" && V == "none"; } }
        public bool IsMuxed { get { return A != "none" && V != "none"; } }
        public double Bytes(double dur) { if (Size > 0) return Size; if (Tbr > 0 && dur > 0) return Tbr * 1000 / 8 * dur; return -1; }
    }

    public class Item
    {
        public string Url, Id = "", Title, Channel = "", Error, Status = "", StatusKind = "";
        public bool IsPlaylist; public int Count;
        public double Duration, PlDuration;
        public List<Fmt> Formats;
        public int State;          // 0 waiting to check, 1 checking, 2 ready, 3 error
        public double Est = -1;
        public ListViewItem Lvi;
        public bool Done;
        public string ThumbUrl;
        public string Kind = "other";      // youtube | drive | other
        public string DriveId, FileName;
        public long ProbeSize = -1; public bool Probed;
        public bool UseCookies;
        public string Extractor = "", Site = "";
        public bool PlForce;              // user asked (right-click) for the whole playlist of a video link
        public string DocKind = "";       // document | presentation | spreadsheets (Google) | pdf | web
        public string DocFile;            // PDF fetched while checking (cache)
        public int Pages = -1;
        public double ClipStart = -1, ClipEnd = -1;
        public string Action = "";        // fix-it button shown in the Status column
        public string DonePath, ArchiveKey = "";
        public bool Archived, ForceAgain;
        public int PlHave;                // playlist videos already downloaded before
        public List<string> PlKeys;
        public bool HasClip { get { return ClipStart >= 0 && ClipEnd > ClipStart; } }
        public bool IsDoc { get { return DocKind != ""; } }
    }

    public class Opts
    {
        public bool Video = true; public int Height; public string Container = "mp4";
        public string AFmt = "mp3"; public string ABr = "0"; public bool Playlist, Subs, Thumb = true, H264First, Doc; public string Cookies = ""; public string CookieFile = ""; public string Pages = "";
    }

    // ------------------------------------------------------------------ format choice + size estimate (mirrors the yt-dlp -S rules used for download)
    public static class Est
    {
        static int CodecRank(string v) { if (v.StartsWith("av01")) return 4; if (v.StartsWith("vp9") || v.StartsWith("vp09")) return 3; if (v.StartsWith("h265") || v.StartsWith("hevc") || v.StartsWith("hvc1") || v.StartsWith("hev1")) return 2; if (H264(v)) return 1; return 0; }
        static bool H264(string v) { return v.StartsWith("avc") || v.StartsWith("h264"); }
        static bool H264(Fmt f) { return H264(f.V); }
        public static string CodecName(string v) { return H264(v) ? "H.264" : v.StartsWith("av01") ? "AV1" : v.StartsWith("vp") ? "VP9" : CodecRank(v) == 2 ? "H.265" : v; }

        public static Fmt PickVideo(List<Fmt> fs, Opts o)
        {
            // "bv*" in the download command = any format that has video, including ones with sound built in
            var vids = new List<Fmt>();
            foreach (var f in fs) if ((f.IsVideoOnly || f.IsMuxed) && f.H > 0 && f.Proto != "mhtml") vids.Add(f);
            if (vids.Count == 0) return null;
            int cap = o.Height > 0 ? o.Height : int.MaxValue;
            var cand = new List<Fmt>();
            foreach (var f in vids) if (f.H <= cap) cand.Add(f);
            if (cand.Count == 0) { int min = int.MaxValue; foreach (var f in vids) min = Math.Min(min, f.H); foreach (var f in vids) if (f.H == min) cand.Add(f); }
            bool upTo1080 = o.Height > 0 && o.Height <= 1080;      // 1080p or lower: prefer H.264 = plays on any TV/phone
            Fmt best = null;
            bool h264First = o.H264First && upTo1080 && o.Container != "webm";
            foreach (var f in cand) if (best == null || Better(f, best, upTo1080, o.Container, h264First)) best = f;
            return best;
        }

        static bool Better(Fmt a, Fmt b, bool upTo1080, string cont, bool h264First)
        {
            int[] ka = Key(a, upTo1080, cont), kb = Key(b, upTo1080, cont);
            if (h264First) { ka = new[] { H264(a) ? 1 : 0, ka[0], ka[2] }; kb = new[] { H264(b) ? 1 : 0, kb[0], kb[2] }; }
            for (int i = 0; i < ka.Length; i++) if (ka[i] != kb[i]) return ka[i] > kb[i];
            return a.Tbr > b.Tbr;
        }
        static int[] Key(Fmt f, bool upTo1080, string cont)
        {
            int aac = f.A.StartsWith("mp4a") ? 1 : 0;
            if (cont == "mp4") return new[] { f.H, H264(f) ? 1 : 0, aac };
            if (cont == "mkv" && upTo1080) return new[] { f.H, H264(f) ? 1 : 0, f.IsVideoOnly ? 1 : 0 };
            if (cont == "webm") return new[] { f.H, f.Ext == "webm" ? 1 : 0, CodecRank(f.V) };
            return new[] { f.H, CodecRank(f.V), 0 };
        }

        public static Fmt PickAudio(List<Fmt> fs, string prefer)   // prefer: "aac", "opus" or ""
        {
            Fmt best = null;
            foreach (var f in fs)
            {
                if (!f.IsAudioOnly) continue;
                if (best == null) { best = f; continue; }
                int pa = Pref(f, prefer), pb = Pref(best, prefer);
                if (pa != pb) { if (pa > pb) best = f; continue; }
                if (f.Abr > best.Abr) best = f;
            }
            return best;
        }
        static int Pref(Fmt f, string prefer)
        {
            bool aac = f.A.StartsWith("mp4a"), opus = f.A.StartsWith("opus");
            if (prefer == "aac") return aac ? 2 : (opus ? 1 : 0);
            return opus ? 2 : (aac ? 1 : 0);
        }

        // bytes for one video; desc gets a short "what you'll get" text
        public static double ForVideo(List<Fmt> fs, double dur, Opts o, out string desc)
        {
            desc = "";
            if (fs == null || fs.Count == 0) return -1;
            if (o.Video)
            {
                var v = PickVideo(fs, o);
                if (v == null)
                {
                    Fmt m = null; foreach (var f in fs) if (f.IsMuxed && (m == null || f.H > m.H)) m = f;
                    if (m == null) return -1;
                    desc = m.H + "p"; return m.Bytes(dur);
                }
                bool aacFirst = o.Container == "mp4";
                var a = v.IsMuxed ? null : PickAudio(fs, aacFirst ? "aac" : "opus");
                desc = v.H + "p " + CodecName(v.V);
                double vb = v.Bytes(dur), ab = a != null ? a.Bytes(dur) : 0;
                if (vb < 0) return -1;
                return vb + Math.Max(0, ab);
            }
            else
            {
                string prefer = o.AFmt == "m4a" ? "aac" : (o.AFmt == "opus" ? "opus" : "");
                var a = PickAudio(fs, prefer);
                double src = a != null ? a.Bytes(dur) : -1;
                double kbps = 0;
                switch (o.AFmt)
                {
                    case "mp3": kbps = o.ABr == "0" ? 245 : double.Parse(o.ABr.TrimEnd('K'), CultureInfo.InvariantCulture); break;
                    case "wav": kbps = 1536; break;
                    case "flac": kbps = 900; break;
                    case "m4a": case "opus": if (o.ABr != "0") kbps = double.Parse(o.ABr.TrimEnd('K'), CultureInfo.InvariantCulture); break;
                }
                desc = o.AFmt.ToUpperInvariant();
                if (kbps > 0 && dur > 0) return kbps * 1000 / 8 * dur;
                return src;
            }
        }

        public static double ForItem(Item it, Opts o, out string desc)
        {
            desc = "";
            if (it.Formats == null) return -1;
            o.H264First = it.Kind != "youtube";
            double one = ForVideo(it.Formats, it.Duration, o, out desc);
            if (!it.IsPlaylist) return one;
            if (one < 0 || it.Duration <= 0) return -1;
            double rate = one / it.Duration;                      // bytes per second, measured on the first video
            double total = it.PlDuration > 0 ? it.PlDuration : it.Duration * it.Count;
            return rate * total;
        }
    }

    // ------------------------------------------------------------------ web: Google Drive original files + direct-link sizes
    public class WebFile { public string Name, Error, Kind = "", ContentType = ""; public long Size = -1; public HttpWebRequest Req; }

    public static class Web
    {
        public static string DriveBase = "https://drive.usercontent.google.com";
        public static string ThumbBase = "https://drive.google.com";
        const string UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0 Safari/537.36";

        public static string DriveId(string url)
        {
            if (!Regex.IsMatch(url, @"^https?://(docs|drive|drive\.usercontent)\.google\.com/", RegexOptions.IgnoreCase)) return null;
            if (url.Contains("/folders/")) return null;
            var m = Regex.Match(url, @"/file/d/([\w-]{10,})"); if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(url, @"[?&]id=([\w-]{10,})"); if (m.Success) return m.Groups[1].Value;
            return null;
        }

        public static CookieContainer LoadCookies(string file)
        {
            var cc = new CookieContainer(); cc.Capacity = 10000; cc.PerDomainCapacity = 1000;
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return cc;
            foreach (var raw in File.ReadAllLines(file))
            {
                string line = raw.TrimEnd('\r');
                if (line.StartsWith("#HttpOnly_")) line = line.Substring(10);
                else if (line.StartsWith("#") || line.Trim().Length == 0) continue;
                var p = line.Split('\t'); if (p.Length < 7) continue;
                try { var c = new Cookie(p[5], p[6], p[2], p[0]); c.Secure = p[3].ToUpperInvariant() == "TRUE"; cc.Add(c); } catch { }
            }
            return cc;
        }

        static HttpWebRequest Req(string url, CookieContainer cc)
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            var r = (HttpWebRequest)WebRequest.Create(url);
            r.Method = "GET"; r.CookieContainer = cc; r.AllowAutoRedirect = true; r.MaximumAutomaticRedirections = 10;
            r.UserAgent = UA; r.Timeout = 30000; r.ReadWriteTimeout = 60000;
            return r;
        }

        static string ReadMax(Stream s, int max)
        {
            var ms = new MemoryStream(); var buf = new byte[8192]; int n;
            while (ms.Length < max && (n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static string FileNameFrom(string cd)
        {
            if (string.IsNullOrEmpty(cd)) return null;
            var m = Regex.Match(cd, @"filename\*\s*=\s*UTF-8''([^;]+)", RegexOptions.IgnoreCase);
            string n = m.Success ? Uri.UnescapeDataString(m.Groups[1].Value.Trim('"')) : null;
            if (n == null) { m = Regex.Match(cd, @"filename\s*=\s*""?([^"";]+)""?", RegexOptions.IgnoreCase); if (m.Success) n = m.Groups[1].Value; }
            return n == null ? null : Safe(n);
        }
        public static string Safe(string n)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            foreach (char c in "<>:\"/\\|?*") n = n.Replace(c, '_');
            n = n.Trim().TrimEnd('.');
            return n.Length == 0 ? "download" : n;
        }

        // Opens the response that carries the original Drive file (follows the "can't scan for viruses" page).
        // Returns null with info.Kind = signin | quota | noaccess | net. Caller disposes the response.
        public static HttpWebResponse OpenDrive(string id, CookieContainer cc, WebFile info)
        {
            string url = DriveBase + "/download?id=" + Uri.EscapeDataString(id) + "&export=download&confirm=t";
            for (int hop = 0; hop < 3; hop++)
            {
                var req = Req(url, cc); info.Req = req;
                HttpWebResponse resp;
                try { resp = (HttpWebResponse)req.GetResponse(); }
                catch (WebException ex)
                {
                    var er = ex.Response as HttpWebResponse;
                    if (er != null)
                    {
                        int code = (int)er.StatusCode; er.Close();
                        info.Kind = (code == 401 || code == 403) ? "signin" : "noaccess";
                        info.Error = code == 404 ? "File not found, or this Google account has no access" : "Drive refused the file (HTTP " + code + ")";
                    }
                    else { info.Kind = "net"; info.Error = "No connection to Google Drive: " + ex.Message; }
                    return null;
                }
                string ct = (resp.ContentType ?? "").ToLowerInvariant();
                string cd = resp.Headers["Content-Disposition"];
                if (!string.IsNullOrEmpty(cd) || !ct.StartsWith("text/html"))
                {
                    info.Name = FileNameFrom(cd) ?? ("Drive file " + id + ".mp4"); info.Size = resp.ContentLength; info.ContentType = ct;
                    return resp;
                }
                Uri final = resp.ResponseUri; string html;
                using (resp) html = ReadMax(resp.GetResponseStream(), 400000);
                if (final.Host.StartsWith("accounts.") || final.AbsolutePath.Contains("ServiceLogin") || Regex.IsMatch(html, @"<title>[^<]*Sign.in", RegexOptions.IgnoreCase))
                { info.Kind = "signin"; info.Error = "This Drive file needs sign-in"; return null; }
                if (Regex.IsMatch(html, "Too many users|download quota|quota exceeded", RegexOptions.IgnoreCase))
                { info.Kind = "quota"; info.Error = "Drive's download limit for this file was reached - try again later"; return null; }
                var form = Regex.Match(html, @"<form\b[^>]*\bid=""download-form""[^>]*>(.*?)</form>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (form.Success)
                {
                    var am = Regex.Match(form.Value, @"\baction=""([^""]+)""", RegexOptions.IgnoreCase);
                    var q = new StringBuilder();
                    foreach (Match inp in Regex.Matches(form.Groups[1].Value, @"<input\b[^>]*>", RegexOptions.IgnoreCase))
                    {
                        var nm = Regex.Match(inp.Value, @"\bname=""([^""]*)"""); var vm = Regex.Match(inp.Value, @"\bvalue=""([^""]*)""");
                        if (!nm.Success || !Regex.IsMatch(inp.Value, @"type=""hidden""", RegexOptions.IgnoreCase)) continue;
                        q.Append(q.Length == 0 ? "" : "&").Append(Uri.EscapeDataString(WebUtility.HtmlDecode(nm.Groups[1].Value)))
                         .Append('=').Append(Uri.EscapeDataString(vm.Success ? WebUtility.HtmlDecode(vm.Groups[1].Value) : ""));
                    }
                    string action = am.Success ? WebUtility.HtmlDecode(am.Groups[1].Value) : "/download";
                    url = new Uri(final, action).GetLeftPart(UriPartial.Path) + "?" + q;
                    continue;
                }
                info.Kind = "noaccess"; info.Error = "Drive showed a page instead of the file (no access, or not a file link)";
                return null;
            }
            info.Kind = "noaccess"; info.Error = "Drive kept asking for confirmation"; return null;
        }

        // ---- documents
        public static string DocsBase = "https://docs.google.com";
        public static string GDoc(string url, out string id)
        {
            id = null;
            var m = Regex.Match(url, @"^https?://docs\.google\.com/(document|presentation|spreadsheets)/(?:u/\d+/)?d/(?!e/)([\w-]{20,})", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            id = m.Groups[2].Value; return m.Groups[1].Value.ToLowerInvariant();
        }
        public static string ExportUrl(string kind, string id)
        {
            if (kind == "presentation") return DocsBase + "/presentation/d/" + id + "/export/pdf";
            return DocsBase + "/" + kind + "/d/" + id + "/export?format=pdf";
        }
        public static bool IsPdfUrl(string url) { return Regex.IsMatch(url, @"^https?://[^?#]+\.pdf([?#].*)?$", RegexOptions.IgnoreCase); }

        // Downloads a PDF to dest. info.Kind on failure: signin | notpdf | notfound | net | cancel
        public static bool FetchPdf(string url, CookieContainer cc, string dest, WebFile info, Func<bool> cancel, Action<long, long> progress)
        {
            var req = Req(url, cc); info.Req = req; req.Timeout = 60000; req.ReadWriteTimeout = 120000;
            HttpWebResponse resp;
            try { resp = (HttpWebResponse)req.GetResponse(); }
            catch (WebException ex)
            {
                var er = ex.Response as HttpWebResponse;
                if (er != null) { int code = (int)er.StatusCode; er.Close(); info.Kind = (code == 401 || code == 403) ? "signin" : code == 404 ? "notfound" : "net"; info.Error = "The server said HTTP " + code; }
                else { info.Kind = "net"; info.Error = "No connection: " + ex.Message; }
                return false;
            }
            using (resp)
            {
                var s = resp.GetResponseStream();
                var head = new byte[5]; int got = 0, n;
                while (got < 5 && (n = s.Read(head, got, 5 - got)) > 0) got += n;
                if (got < 5 || Encoding.ASCII.GetString(head, 0, 5) != "%PDF-")
                {
                    string html = Encoding.UTF8.GetString(head, 0, got) + ReadMax(s, 300000);
                    Uri final = resp.ResponseUri;
                    if (final.Host.StartsWith("accounts.") || final.AbsolutePath.Contains("ServiceLogin") || Regex.IsMatch(html, @"<title>[^<]*(Sign.in|Log.in)", RegexOptions.IgnoreCase))
                    { info.Kind = "signin"; info.Error = "This document needs sign-in"; }
                    else { info.Kind = "notpdf"; info.Error = "The link gave a web page, not a PDF"; }
                    return false;
                }
                info.Name = FileNameFrom(resp.Headers["Content-Disposition"]);
                if (info.Name == null) { string last = Uri.UnescapeDataString(resp.ResponseUri.AbsolutePath.TrimEnd('/')); last = last.Substring(last.LastIndexOf('/') + 1); info.Name = Safe(last.Length > 0 ? last : "document.pdf"); }
                if (!info.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) info.Name += ".pdf";
                long total = resp.ContentLength, done = 5;
                string part = dest + ".part";
                try
                {
                    using (var fs = new FileStream(part, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(head, 0, 5);
                        var buf = new byte[1 << 18];
                        while ((n = s.Read(buf, 0, buf.Length)) > 0)
                        {
                            if (cancel != null && cancel()) { req.Abort(); info.Kind = "cancel"; info.Error = "cancelled"; throw new OperationCanceledException(); }
                            fs.Write(buf, 0, n); done += n;
                            if (progress != null) progress(done, total);
                        }
                    }
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(part, dest);
                }
                catch (OperationCanceledException) { try { File.Delete(part); } catch { } return false; }
                catch (Exception ex) { try { File.Delete(part); } catch { } info.Kind = "net"; info.Error = ex.Message; return false; }
                info.Size = done;
                return true;
            }
        }

        // Title of a web page (null if it is not a page)
        public static string PageTitle(string url, WebFile info)
        {
            try
            {
                var r = Req(url, new CookieContainer());
                using (var resp = (HttpWebResponse)r.GetResponse())
                {
                    string ct = (resp.ContentType ?? "").ToLowerInvariant();
                    if (!ct.Contains("html")) { info.Kind = "notpage"; info.Error = "Not a web page (" + ct + ")"; return null; }
                    string html = ReadMax(resp.GetResponseStream(), 600000);
                    var m = Regex.Match(html, @"<meta[^>]+property=[""']og:title[""'][^>]+content=[""']([^""']+)", RegexOptions.IgnoreCase);
                    if (!m.Success) m = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    string t = m.Success ? WebUtility.HtmlDecode(m.Groups[1].Value).Trim() : "";
                    t = Regex.Replace(t, @"\s+", " ");
                    return t.Length > 0 ? t : resp.ResponseUri.Host;
                }
            }
            catch (WebException ex)
            {
                var er = ex.Response as HttpWebResponse;
                info.Kind = er != null ? "http" : "net"; info.Error = er != null ? "The site said HTTP " + (int)er.StatusCode : "No connection: " + ex.Message;
                if (er != null) er.Close();
                return null;
            }
            catch (Exception ex) { info.Kind = "net"; info.Error = ex.Message; return null; }
        }

        // Size of a direct media link (-1 if it is not a plain media file).
        public static long DirectSize(string url)
        {
            try
            {
                var r = Req(url, new CookieContainer());
                using (var resp = (HttpWebResponse)r.GetResponse())
                {
                    string ct = (resp.ContentType ?? "").ToLowerInvariant();
                    long len = resp.ContentLength;
                    r.Abort();
                    if (ct.StartsWith("video/") || ct.StartsWith("audio/") || ct.Contains("octet-stream")) return len;
                }
            }
            catch { }
            return -1;
        }

        public static byte[] DriveThumb(string id, CookieContainer cc)
        {
            try
            {
                var r = Req(ThumbBase + "/thumbnail?id=" + Uri.EscapeDataString(id) + "&sz=w320", cc);
                using (var resp = (HttpWebResponse)r.GetResponse())
                {
                    if (!(resp.ContentType ?? "").StartsWith("image/")) return null;
                    var ms = new MemoryStream(); resp.GetResponseStream().CopyTo(ms); return ms.ToArray();
                }
            }
            catch { return null; }
        }
    }

    // ------------------------------------------------------------------ PDF page count without extra tools
    public static class Pdf
    {
        public static int CountPages(string path)
        {
            try
            {
                var fi = new FileInfo(path); if (!fi.Exists || fi.Length > 300L * 1024 * 1024) return -1;
                string raw = Encoding.GetEncoding(28591).GetString(File.ReadAllBytes(path));
                int best = MaxCount(raw);
                if (best > 0) return best;
                var sb = new StringBuilder();
                foreach (Match m in Regex.Matches(raw, @"/Type\s*/ObjStm"))
                {
                    int st = raw.IndexOf("stream", m.Index, StringComparison.Ordinal); if (st < 0) continue;
                    st += 6; if (st < raw.Length && raw[st] == '\r') st++; if (st < raw.Length && raw[st] == '\n') st++;
                    int en = raw.IndexOf("endstream", st, StringComparison.Ordinal); if (en < 0) continue;
                    string body = Inflate(raw, st, en - st); if (body != null) sb.Append(body).Append('\n');
                }
                string all = sb.ToString();
                best = MaxCount(all);
                if (best > 0) return best;
                int pages = Regex.Matches(raw, @"/Type\s*/Page\b(?!s)").Count + Regex.Matches(all, @"/Type\s*/Page\b(?!s)").Count;
                return pages > 0 ? pages : -1;
            }
            catch { return -1; }
        }
        static int MaxCount(string s)
        {
            int best = 0;
            foreach (Match m in Regex.Matches(s, @"/Type\s*/Pages\b"))
            {
                int a = Math.Max(0, m.Index - 400), b = Math.Min(s.Length, m.Index + 400);
                int o = s.LastIndexOf(" obj", m.Index, m.Index - a, StringComparison.Ordinal); if (o >= 0) a = o;
                int e = s.IndexOf("endobj", m.Index, b - m.Index, StringComparison.Ordinal); if (e >= 0) b = e;
                var c = Regex.Match(s.Substring(a, b - a), @"/Count\s+(\d+)");
                int v; if (c.Success && int.TryParse(c.Groups[1].Value, out v) && v > best && v < 1000000) best = v;
            }
            return best;
        }
        static string Inflate(string raw, int start, int len)
        {
            try
            {
                if (len < 3) return null;
                var bytes = Encoding.GetEncoding(28591).GetBytes(raw.Substring(start + 2, len - 2));
                using (var ms = new MemoryStream(bytes)) using (var ds = new DeflateStream(ms, CompressionMode.Decompress)) using (var o = new MemoryStream())
                {
                    var buf = new byte[65536]; int n;
                    try { while ((n = ds.Read(buf, 0, buf.Length)) > 0) { o.Write(buf, 0, n); if (o.Length > 50000000) break; } } catch { }
                    return Encoding.GetEncoding(28591).GetString(o.ToArray());
                }
            }
            catch { return null; }
        }

        // "1-3, 5, 8-10" -> number of pages selected (validated against total); error text on failure
        public static int CountRange(string range, int total, out string error)
        {
            error = null; int n = 0;
            if (!Regex.IsMatch(range ?? "", @"^\s*\d+(\s*-\s*\d+)?(\s*,\s*\d+(\s*-\s*\d+)?)*\s*$")) { error = "Use page numbers like 1-3, 5, 8-10"; return -1; }
            foreach (var part in range.Split(','))
            {
                var p = part.Split('-'); int a = int.Parse(p[0].Trim()), b = p.Length > 1 ? int.Parse(p[1].Trim()) : a;
                if (a < 1 || b < a) { error = "\"" + part.Trim() + "\" is not a valid range"; return -1; }
                if (total > 0 && b > total) { error = "Page " + b + " doesn't exist (" + total + " pages)"; return -1; }
                n += b - a + 1;
            }
            return n;
        }
        public static string QpdfRange(string range) { return Regex.Replace(range, @"\s+", ""); }
    }

    // ------------------------------------------------------------------ main window
    public class MainForm : Form, ISurface
    {
        string dir, ytdlp, ffmpeg, deno, qpdf, pathEnv, settingsFile, ytdlpVersion = "";
        HeaderPanel header; Panel footer; Label lblTitle, lblSub, lblTools; FlatButton btnUpdate, btnSkin, btnAbout;
        Card cLinks, cOpts, cQueue;
        TextBox txtLinks; Label lblHint; FlatButton btnPaste, btnClear; CheckBox ckCatch;
        Tile tVideo, tAudio, tDoc; Panel pVideo, pAudio, pDoc, pMore;
        ComboBox cbQuality, cbContainer, cbAFmt, cbABr, cbCookies, cbPages; TextBox txtPages; CheckBox ckSubs, ckThumb; ContextMenuStrip rowMenu, skinMenu; Label lblVideoHint;
        Label lblAudioHint, lblDocHint, lblCookieInfo;
        Panel lvBox, lvHead; ListView lv; ImageList thumbs; Label lblSummary, lblEmpty; TextBox txtOut; FlatButton btnBrowse, btnOpen;
        Bar bar; Label lblStatus; FlatButton btnDownload, btnCancel, btnLog;
        ToolTip tip = new ToolTip();
        Form logForm; TextBox logBox; StringBuilder log = new StringBuilder();
        List<Item> items = new List<Item>();
        System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();
        Runner fetchRunner, dlRunner; Item fetching; StringBuilder fetchOut = new StringBuilder(); int fetchStage;
        bool busy, cancelAll, loading = true; string lastCookies = ""; bool lastDoc;
        string cookiesFilePath = "", exportedCookies, exportedFor; DateTime exportedAt; string cookieError;
        bool driveFetching; volatile bool driveCancel; bool driveRunning;
        int dlIndex, attempt; bool saw403, sawBot; string lastErr, lastDest; int plSkipped; List<Item> queue;
        static readonly string[] Fallbacks = { "youtube:player_client=web_safari,android_vr", "youtube:player_client=tv,mweb" };
        static readonly int[] Heights = { 0, 2160, 1440, 1080, 720, 480, 360 };
        string skinName = "Crimson"; ListViewItem hotRow;

        public MainForm(string scriptDir)
        {
            dir = scriptDir; settingsFile = Path.Combine(dir, "dawnloader-settings.txt");
            try { string oldSettings = Path.Combine(dir, "ytdl-settings.txt"); if (!File.Exists(settingsFile) && File.Exists(oldSettings)) File.Copy(oldSettings, settingsFile); } catch { }
            skinName = ReadSetting("Skin", "Crimson"); T.Cur = T.MakeSkin(skinName);
            Text = "Dawnloader"; Font = T.Body; StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None; DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            var ic = Icons.AppIcon(); if (ic != null) Icon = ic;
            FindTools();
            Build();
            LoadSettings();
            loading = false; lastCookies = GetOpts().Cookies; lastDoc = tDoc.Checked;
            ApplyMode();
            debounce.Interval = 700; debounce.Tick += (s, e) => { debounce.Stop(); SyncItems(); };
            clipTimer.Interval = 1000; clipTimer.Tick += (s, e) => { if (!winClip) CatchClipboard(); };
            Shown += (s, e) =>
            {
                ApplyWindowTheme();
                CheckVersion(); UpdateToolsLabel(); CleanCache(); StartClipboardWatch();
                if (ytdlp == null || ffmpeg == null)
                    BeginInvoke((MethodInvoker)delegate { AskInstall("This app needs a few free helper tools before it can download."); });
            };
            FormClosing += (s, e) => { SaveSettings(); if (dlRunner != null) dlRunner.Kill(); if (fetchRunner != null) fetchRunner.Kill(); };
        }

        public void PaintSurface(Graphics g)
        {
            var k = T.Cur; var r = ClientRectangle; if (r.Width < 1 || r.Height < 1) return;
            if (k.Bg == k.Bg2) using (var b = new SolidBrush(k.Bg)) g.FillRectangle(b, r);
            else using (var b = new LinearGradientBrush(r, k.Bg, k.Bg2, 35f)) g.FillRectangle(b, r);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { PaintSurface(e.Graphics); }

        string ReadSetting(string key, string def)
        {
            try { foreach (var line in File.ReadAllLines(settingsFile)) if (line.StartsWith(key + "=")) return line.Substring(key.Length + 1); } catch { }
            return def;
        }

        static string DataDir
        {
            get
            {
                string la = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (string.IsNullOrEmpty(la)) la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string d = Path.Combine(la, "Dawnloader"); try { Directory.CreateDirectory(d); } catch { }
                return d;
            }
        }

        // ---------------- tools
        void FindTools()
        {
            var parts = new List<string>();
            try
            {
                parts.Add(Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine));
                parts.Add(Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User));
                string la = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (!string.IsNullOrEmpty(la)) parts.Add(Path.Combine(la, @"Microsoft\WinGet\Links"));
            }
            catch { }
            parts.Add(Environment.GetEnvironmentVariable("PATH"));
            parts.Add(ToolsDir); parts.Add(Path.Combine(ToolsDir, "qpdf"));
            var sb = new StringBuilder(); sb.Append(dir);
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) { sb.Append(Path.PathSeparator); sb.Append(p); }
            pathEnv = sb.ToString();
            ytdlp = Find("yt-dlp"); ffmpeg = Find("ffmpeg"); deno = Find("deno"); qpdf = Find("qpdf");
            if (deno != null) pathEnv = Path.GetDirectoryName(deno) + Path.PathSeparator + pathEnv;
        }

        string Find(string name)
        {
            bool win = Environment.OSVersion.Platform == PlatformID.Win32NT;
            foreach (var d in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrEmpty(d)) continue;
                try
                {
                    string f = Path.Combine(d.Trim().Trim('"'), name + ".exe");
                    if (File.Exists(f)) return f;
                    if (!win) { f = Path.Combine(d.Trim(), name); if (File.Exists(f)) return f; }
                }
                catch { }
            }
            return null;
        }

        // ---------------- layout
        Label L(string text, Font f, Color c) { var l = new Label(); l.Text = text; l.Font = f; l.ForeColor = c; l.AutoSize = true; l.BackColor = Color.Transparent; return l; }
        ComboBox Combo(string[] items, int w)
        {
            var c = new ComboBox(); c.DropDownStyle = ComboBoxStyle.DropDownList; c.Font = T.Body; c.Items.AddRange(items); c.SelectedIndex = 0;
            c.Width = T.P(w);
            if (T.Cur.Dark) { c.FlatStyle = FlatStyle.Flat; c.BackColor = T.Input; c.ForeColor = T.Text; } else c.FlatStyle = FlatStyle.System;
            c.SelectedIndexChanged += (s, e) => OptionsChanged(); return c;
        }
        CheckBox Check(string text)
        {
            var c = new CheckBox(); c.Text = text; c.Font = T.Body; c.ForeColor = T.Text; c.AutoSize = true; c.BackColor = Color.Transparent;
            c.CheckedChanged += (s, e) => OptionsChanged(); return c;
        }
        TextBox Box()
        {
            var t = new TextBox(); t.Font = T.Body; t.BorderStyle = BorderStyle.FixedSingle; t.BackColor = T.Input; t.ForeColor = T.Text; return t;
        }
        // place controls left-to-right on a row, vertically centred on y..y+h, using their real (DPI/font-aware) sizes
        int Row(Control parent, int x, int y, int h, params object[] ctrls)
        {
            foreach (var o in ctrls)
            {
                if (o is int) { x += T.P((int)o); continue; }
                var c = (Control)o;
                if (c is TextBox) c.Height = c.PreferredSize.Height; else if (c.AutoSize) c.Size = c.PreferredSize;
                c.Location = new Point(x, y + (h - c.Height) / 2);
                parent.Controls.Add(c);
                x = c.Right + T.P(8);
            }
            return x;
        }
        Panel Clear(Panel p) { p.BackColor = Color.Transparent; return p; }

        void Build()
        {
            int W = T.P(940), H = T.P(800), M = T.P(16);
            ClientSize = new Size(W, H);
            MinimumSize = new Size(T.P(860), T.P(660));
            var wa = Screen.PrimaryScreen.WorkingArea;
            if (Height > wa.Height) Height = wa.Height;
            if (Width > wa.Width) Width = wa.Width;
            BackColor = T.Solid(T.Bg);
            SuspendLayout();

            // header
            header = new HeaderPanel(); header.Size = new Size(W, T.P(72)); header.Dock = DockStyle.Top;
            header.Resize += (s, e) => PlaceTools();
            lblTitle = L("Dawnloader", T.Header, T.Cur.HeadText); lblTitle.Location = new Point(T.P(72), T.P(10));
            lblSub = L("Video, audio or PDF from YouTube, TikTok, Instagram, X and 1000+ more sites.", T.HeaderSub, T.Cur.HeadSub); lblSub.Location = new Point(T.P(74), T.P(42));
            btnUpdate = new FlatButton("Update yt-dlp", "refresh", 3); btnUpdate.Font = T.BodyBold;
            btnUpdate.Size = new Size(btnUpdate.PreferredWidth(T.P(36)), T.P(36)); btnUpdate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUpdate.Click += (s, e) => { if (MissingTools().Count > 0) InstallTools(); else UpdateYtDlp(); };
            btnSkin = new FlatButton("", "palette", 3); btnSkin.Size = new Size(T.P(36), T.P(36)); btnSkin.Anchor = btnUpdate.Anchor;
            btnAbout = new FlatButton("", "info", 3); btnAbout.Size = btnSkin.Size; btnAbout.Anchor = btnUpdate.Anchor;
            tip.SetToolTip(btnSkin, "Skin - change how Dawnloader looks"); tip.SetToolTip(btnAbout, "About Dawnloader");
            skinMenu = new ContextMenuStrip(); skinMenu.Font = T.Body; skinMenu.Renderer = new SkinRenderer();
            btnSkin.Click += (s, e) =>
            {
                skinMenu.Items.Clear();
                var head = new ToolStripMenuItem("Skin"); head.Enabled = false; skinMenu.Items.Add(head);
                foreach (var n in T.SkinNames)
                {
                    string name = n;
                    var mi = new ToolStripMenuItem(name, null, (a, b) => ChangeSkin(name)); mi.Checked = name == skinName; mi.Enabled = !busy;
                    skinMenu.Items.Add(mi);
                }
                var sz = skinMenu.GetPreferredSize(Size.Empty);
                skinMenu.Show(btnSkin, new Point(btnSkin.Width - sz.Width, btnSkin.Height + T.P(4)));
            };
            btnAbout.Click += (s, e) => ShowAbout();
            lblTools = L("", T.Small, T.Cur.HeadSub); lblTools.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.AddRange(new Control[] { lblTitle, lblSub, btnUpdate, btnSkin, btnAbout, lblTools });

            // footer
            footer = Clear(new Panel()); footer.Size = new Size(W, T.P(84)); footer.Dock = DockStyle.Bottom;
            btnDownload = new FlatButton("Download", "download", 0); btnDownload.Font = T.Big;
            btnDownload.Size = new Size(Math.Max(T.P(220), btnDownload.PreferredWidth(T.P(54))), T.P(54));
            btnDownload.Location = new Point(W - M - btnDownload.Width, T.P(14)); btnDownload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDownload.Click += (s, e) => StartDownloads(null);
            btnCancel = new FlatButton("Cancel", "stop", 2); btnCancel.Font = T.Big; btnCancel.Size = btnDownload.Size; btnCancel.Location = btnDownload.Location;
            btnCancel.Anchor = btnDownload.Anchor; btnCancel.Visible = false; btnCancel.Click += (s, e) => CancelAll();
            btnLog = new FlatButton("Details", "log", 4); btnLog.Font = T.Body; btnLog.Size = new Size(btnLog.PreferredWidth(T.P(32)), T.P(32));
            btnLog.Location = new Point(btnDownload.Left - T.P(12) - btnLog.Width, T.P(36)); btnLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLog.Click += (s, e) => ShowLog();
            bar = new Bar(); bar.Location = new Point(M, T.P(22)); bar.Size = new Size(btnDownload.Left - T.P(24) - M, T.P(8)); bar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus = new Label(); lblStatus.Font = T.Body; lblStatus.ForeColor = T.Muted; lblStatus.AutoSize = false; lblStatus.AutoEllipsis = true; lblStatus.BackColor = Color.Transparent;
            lblStatus.Location = new Point(M, T.P(38)); lblStatus.Size = new Size(btnLog.Left - T.P(8) - M, T.P(26)); lblStatus.Anchor = bar.Anchor;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft; lblStatus.Text = "Ready.";
            footer.Controls.AddRange(new Control[] { btnDownload, btnCancel, btnLog, bar, lblStatus });

            int y = T.P(72) + T.P(12), CW = W - 2 * M;

            // card 1: links
            cLinks = new Card("1", "Paste your links"); cLinks.SetBounds(M, y, CW, T.P(122)); cLinks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnPaste = new FlatButton("Paste", "paste", 1); btnPaste.Font = T.BodyBold;
            int bw = Math.Max(T.P(140), btnPaste.PreferredWidth(T.P(40)));
            btnPaste.SetBounds(CW - M - bw, cLinks.ContentTop - T.P(6), bw, T.P(40)); btnPaste.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPaste.Click += (s, e) => PasteLinks();
            btnClear = new FlatButton("Clear", "clear", 1); btnClear.Font = T.Body;
            btnClear.SetBounds(btnPaste.Left, btnPaste.Bottom + T.P(6), bw, T.P(30)); btnClear.Anchor = btnPaste.Anchor;
            btnClear.Click += (s, e) => { txtLinks.Clear(); txtLinks.Focus(); };
            txtLinks = Box(); txtLinks.Multiline = true; txtLinks.ScrollBars = ScrollBars.Vertical;
            txtLinks.SetBounds(M, cLinks.ContentTop - T.P(6), btnPaste.Left - T.P(12) - M, cLinks.Height - cLinks.ContentTop + T.P(6) - M);
            txtLinks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtLinks.TextChanged += (s, e) => { lblHint.Visible = txtLinks.TextLength == 0; debounce.Stop(); debounce.Start(); };
            lblHint = L("One link per line - YouTube, TikTok, Instagram, Facebook, X, Google Drive / Docs, PDF links ...", T.Body, T.Hint);
            lblHint.BackColor = T.Input; lblHint.Location = new Point(T.P(4), T.P(4)); lblHint.Cursor = Cursors.IBeam;
            lblHint.Click += (s, e) => txtLinks.Focus();
            txtLinks.Controls.Add(lblHint);
            ckCatch = new CheckBox(); ckCatch.Text = "Catch copied links"; ckCatch.Font = T.Body; ckCatch.ForeColor = T.Text; ckCatch.AutoSize = true; ckCatch.BackColor = Color.Transparent;
            ckCatch.Size = ckCatch.PreferredSize; ckCatch.Location = new Point(btnPaste.Right - ckCatch.Width, T.P(14) + (T.P(24) - ckCatch.Height) / 2);
            ckCatch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ckCatch.CheckedChanged += (s, e) => { if (loading) return; lastClip = SafeClipboardText(); StartClipboardWatch(); SetStatus(ckCatch.Checked ? "Copy a video, Drive, Docs or PDF link anywhere - it will appear here automatically." : "Catching copied links is off.", T.Muted); };
            tip.SetToolTip(ckCatch, "When on, links you copy (Ctrl+C) in your browser or any app are added here automatically.\nOnly video, Google Drive / Docs and PDF links are picked up.");
            cLinks.Controls.AddRange(new Control[] { txtLinks, btnPaste, btnClear, ckCatch });
            y = cLinks.Bottom + T.P(10);

            // card 2: options
            cOpts = new Card("2", "Choose what you want"); cOpts.SetBounds(M, y, CW, T.P(200)); cOpts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tVideo = new Tile("Video", "Picture + sound", "video"); tAudio = new Tile("Audio only", "Music, podcasts", "audio"); tDoc = new Tile("Document", "Save as PDF", "doc");
            int th = T.P(56);
            foreach (var t in new[] { tVideo, tAudio, tDoc }) t.Height = th;
            int tw = Math.Max(T.P(190), Math.Max(tDoc.PreferredW(), Math.Max(tVideo.PreferredW(), tAudio.PreferredW())));
            tVideo.SetBounds(M, cOpts.ContentTop, tw, th); tAudio.SetBounds(tVideo.Right + T.P(10), cOpts.ContentTop, tw, th); tDoc.SetBounds(tAudio.Right + T.P(10), cOpts.ContentTop, tw, th);
            tVideo.Picked += (s, e) => PickMode(tVideo); tAudio.Picked += (s, e) => PickMode(tAudio); tDoc.Picked += (s, e) => PickMode(tDoc);
            int oy = tVideo.Bottom + T.P(10), ow = CW - 2 * M, rh = T.P(34);
            pVideo = Clear(new Panel()); pVideo.SetBounds(M, oy, ow, rh); pVideo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pAudio = Clear(new Panel()); pAudio.Bounds = pVideo.Bounds; pAudio.Anchor = pVideo.Anchor;
            pDoc = Clear(new Panel()); pDoc.Bounds = pVideo.Bounds; pDoc.Anchor = pVideo.Anchor;
            cbQuality = Combo(new[] { "Best available", "2160p (4K)", "1440p (2K)", "1080p (Full HD)", "720p (HD)", "480p", "360p" }, 160);
            cbContainer = Combo(new[] { "MP4", "MKV", "WEBM" }, 90);
            lblVideoHint = Hint();
            int hx = Row(pVideo, 0, 0, rh, L("Quality", T.Body, T.Muted), cbQuality, 10, L("Format", T.Body, T.Muted), cbContainer);
            PlaceHint(pVideo, lblVideoHint, hx);
            cbAFmt = Combo(new[] { "MP3", "M4A", "OPUS", "FLAC", "WAV", "Original (no conversion)" }, 190);
            cbABr = Combo(new[] { "Best", "320 kbps", "192 kbps", "128 kbps" }, 110);
            lblAudioHint = Hint();
            hx = Row(pAudio, 0, 0, rh, L("Format", T.Body, T.Muted), cbAFmt, 10, L("Quality", T.Body, T.Muted), cbABr);
            PlaceHint(pAudio, lblAudioHint, hx);
            cbPages = Combo(new[] { "All pages", "Choose pages" }, 150);
            txtPages = Box(); txtPages.Width = T.P(150);
            txtPages.TextChanged += (s, e) => { if (!loading) { RecomputeAll(); ApplyMode(); } };
            lblDocHint = Hint();
            hx = Row(pDoc, 0, 0, rh, L("Pages", T.Body, T.Muted), cbPages, txtPages);
            PlaceHint(pDoc, lblDocHint, hx);
            tip.SetToolTip(txtPages, "Pages to keep, for example: 1-3, 5, 8-10\nApplies to every document in the list.");
            pMore = Clear(new Panel()); pMore.SetBounds(M, pVideo.Bottom + T.P(6), CW - 2 * M, T.P(34)); pMore.Anchor = pVideo.Anchor;
            ckSubs = Check("English subtitles"); ckThumb = Check("Cover art + title tags");
            cbCookies = Combo(new[] { "Off", "Firefox", "Edge", "Chrome", "Brave", "cookies.txt file..." }, 150);
            cbCookies.SelectedIndexChanged += (s, e) => { if (!loading && cbCookies.SelectedIndex == 5) PickCookiesFile(); };
            lblCookieInfo = new Label(); lblCookieInfo.Size = new Size(T.P(22), T.P(22)); lblCookieInfo.BackColor = Color.Transparent; lblCookieInfo.Cursor = Cursors.Help;
            lblCookieInfo.Paint += (s, e) => Icons.Draw(e.Graphics, "info", new RectangleF(1, 1, lblCookieInfo.Width - 2, lblCookieInfo.Width - 2), T.Muted);
            Row(pMore, 0, 0, T.P(34), ckSubs, 14, ckThumb, 22, L("Sign-in if needed", T.Body, T.Muted), cbCookies, lblCookieInfo);
            tip.SetToolTip(lblCookieInfo, "Every link is first tried WITHOUT sign-in. Only links that need it use the\n" +
                "login saved in the browser chosen here (marked \"signed in\" in the list). Needed for:\n" +
                "  - Google Drive files and Google Docs that are not public (be signed in to the account that has access)\n" +
                "  - YouTube saying \"Sign in to confirm you're not a bot\", age-restricted or members-only videos\n" +
                "  - Instagram, Facebook, TikTok, X: private posts, or when the site limits signed-out downloads\n\n" +
                "Firefox works best. Chrome/Edge often lock their sign-in - if they fail, close the browser,\n" +
                "or export a cookies.txt file with the \"Get cookies.txt LOCALLY\" extension and pick it here.");
            cOpts.Controls.AddRange(new Control[] { tVideo, tAudio, tDoc, pVideo, pAudio, pDoc, pMore });
            y = cOpts.Bottom + T.P(10);

            // card 3: queue
            cQueue = new Card("3", "Check and download"); cQueue.SetBounds(M, y, CW, H - footer.Height - y);
            cQueue.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSummary = L("", T.BodyBold, T.Text); lblSummary.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            int ch = cQueue.Height;
            btnOpen = new FlatButton("Open", "open", 1); btnOpen.Font = T.BodyBold;
            btnBrowse = new FlatButton("Change...", "folder", 1); btnBrowse.Font = T.BodyBold;
            int sh = T.P(36);
            btnOpen.Size = new Size(btnOpen.PreferredWidth(sh), sh); btnBrowse.Size = new Size(btnBrowse.PreferredWidth(sh), sh);
            btnOpen.Location = new Point(CW - M - btnOpen.Width, ch - M - sh); btnBrowse.Location = new Point(btnOpen.Left - T.P(8) - btnBrowse.Width, btnOpen.Top);
            btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right; btnBrowse.Anchor = btnOpen.Anchor;
            btnOpen.Click += (s, e) => OpenFolder(); btnBrowse.Click += (s, e) => Browse();
            var lblSave = L("Save to", T.Body, T.Muted); lblSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblSave.Size = lblSave.PreferredSize; lblSave.Location = new Point(M, btnOpen.Top + (sh - lblSave.Height) / 2);
            txtOut = Box();
            txtOut.Location = new Point(lblSave.Right + T.P(8), 0); txtOut.Width = btnBrowse.Left - T.P(8) - txtOut.Left;
            txtOut.Top = btnOpen.Top + (sh - txtOut.Height) / 2; txtOut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtOut.TextChanged += (s, e) => UpdateSummary();

            if (thumbs == null) { thumbs = new ImageList(); thumbs.ColorDepth = ColorDepth.Depth32Bit; thumbs.ImageSize = new Size(T.P(64), T.P(36)); }
            SetImage("ph", Placeholder("play")); SetImage("doc", Placeholder("doc"));
            lvBox = new Panel(); lvBox.BackColor = T.Line; lvBox.Padding = new Padding(1);
            lvBox.SetBounds(M, cQueue.ContentTop, CW - 2 * M, btnOpen.Top - T.P(12) - cQueue.ContentTop);
            lvBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvHead = new Panel(); lvHead.Height = T.P(28); lvHead.Dock = DockStyle.Top; lvHead.BackColor = T.Cur.ColHead;
            lvHead.Paint += (s, e) => PaintListHeader(e.Graphics);
            lv = new ListView(); lv.View = View.Details; lv.FullRowSelect = true; lv.HeaderStyle = ColumnHeaderStyle.None; lv.BorderStyle = BorderStyle.None;
            lv.Font = T.Body; lv.SmallImageList = thumbs; lv.MultiSelect = false; lv.ShowItemToolTips = true; lv.Dock = DockStyle.Fill;
            lv.BackColor = T.Input; lv.ForeColor = T.Text; lv.OwnerDraw = true;
            lv.Columns.Add("Title", T.P(380)); lv.Columns.Add("Length", T.P(86), HorizontalAlignment.Right);
            lv.Columns.Add("Size (est.)", T.P(96), HorizontalAlignment.Right); lv.Columns.Add("Status", T.P(290));
            lv.DrawColumnHeader += (s, e) => { e.DrawDefault = true; };
            lv.DrawItem += (s, e) => { };
            lv.DrawSubItem += (s, e) => DrawCell(e);
            lv.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) ListClick(e.Location); };
            lv.MouseMove += (s, e) =>
            {
                lv.Cursor = PillAt(e.Location) != null ? Cursors.Hand : Cursors.Default;
                var h = lv.GetItemAt(T.P(4), e.Y);      // owner-drawn rows: repaint fully when the hot row changes (WinForms hover glitch)
                if (h != hotRow) { if (hotRow != null && hotRow.ListView == lv) lv.Invalidate(hotRow.Bounds); if (h != null) lv.Invalidate(h.Bounds); hotRow = h; }
            };
            lv.Resize += (s, e) => FitColumns();
            lv.DoubleClick += (s, e) => { var it = lv.SelectedItems.Count > 0 ? lv.SelectedItems[0].Tag as Item : null; if (it != null && it.DonePath != null) ShowInFolder(it.DonePath); else OpenFolder(); };
            rowMenu = new ContextMenuStrip(); rowMenu.Font = T.Body; rowMenu.Renderer = new SkinRenderer();
            rowMenu.Opening += (s, e) => BuildRowMenu();
            lv.ContextMenuStrip = rowMenu;
            lvBox.Controls.Add(lv); lvBox.Controls.Add(lvHead);
            lblEmpty = L("Your links will appear here with their size before you download.", T.Body, T.Muted);
            lblEmpty.BackColor = T.Input; lblEmpty.Anchor = AnchorStyles.None;
            lv.Controls.Add(lblEmpty);
            cQueue.Controls.AddRange(new Control[] { lblSummary, lvBox, lblSave, txtOut, btnBrowse, btnOpen });
            cQueue.Resize += (s, e) => PlaceQueueLabels();

            Controls.Add(cQueue); Controls.Add(cOpts); Controls.Add(cLinks); Controls.Add(footer); Controls.Add(header);
            ResumeLayout(true);
            FitColumns(); PlaceQueueLabels(); PlaceTools();
            tVideo.Checked = true;
        }

        Label Hint() { var l = L("", T.Small, T.Muted); l.AutoSize = false; l.AutoEllipsis = true; l.TextAlign = ContentAlignment.MiddleLeft; return l; }
        void PlaceHint(Panel p, Label l, int x)
        {
            l.SetBounds(x + T.P(8), 0, Math.Max(T.P(60), p.Width - x - T.P(8)), p.Height); l.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            p.Controls.Add(l);
        }
        void SetHint(Label l, string text) { l.Text = text; tip.SetToolTip(l, text); }

        void SetImage(string key, Image img) { int i = thumbs.Images.IndexOfKey(key); if (i >= 0) thumbs.Images.RemoveAt(i); thumbs.Images.Add(key, img); }

        Bitmap Placeholder(string icon)
        {
            var b = new Bitmap(thumbs.ImageSize.Width, thumbs.ImageSize.Height);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent); g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = T.Round(new RectangleF(0, T.P(2), b.Width - T.P(6), b.Height - T.P(4)), T.P(4))) using (var br = new SolidBrush(T.Solid(T.Soft))) g.FillPath(br, p);
                float s = b.Height * (icon == "doc" ? 0.6f : 0.45f); Icons.Draw(g, icon, new RectangleF((b.Width - T.P(6) - s) / 2, (b.Height - s) / 2, s, s), T.Cur.Hint);
            }
            return b;
        }

        Bitmap ThumbFrom(Image src)
        {
            var b = new Bitmap(thumbs.ImageSize.Width, thumbs.ImageSize.Height);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.Transparent); g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.SmoothingMode = SmoothingMode.AntiAlias;
                var dst = new RectangleF(0, T.P(2), b.Width - T.P(6), b.Height - T.P(4));
                float sr = (float)src.Width / src.Height, dr = dst.Width / dst.Height;
                RectangleF s = sr > dr ? new RectangleF((src.Width - src.Height * dr) / 2, 0, src.Height * dr, src.Height) : new RectangleF(0, (src.Height - src.Width / dr) / 2, src.Width, src.Width / dr);
                using (var p = T.Round(dst, T.P(4))) { g.SetClip(p); g.DrawImage(src, dst, s, GraphicsUnit.Pixel); }
            }
            return b;
        }

        void FitColumns()
        {
            if (lv == null || lv.Columns.Count < 4) return;
            int fixedW = lv.Columns[1].Width + lv.Columns[2].Width + lv.Columns[3].Width;
            lv.Columns[0].Width = Math.Max(T.P(160), lv.Width - SystemInformation.VerticalScrollBarWidth - fixedW - T.P(8));
            if (lvHead != null) lvHead.Invalidate();
        }
        void PaintListHeader(Graphics g)
        {
            using (var b = new SolidBrush(T.Cur.ColHead)) g.FillRectangle(b, lvHead.ClientRectangle);
            using (var pen = new Pen(T.Line)) g.DrawLine(pen, 0, lvHead.Height - 1, lvHead.Width, lvHead.Height - 1);
            string[] names = { "Title", tDoc != null && tDoc.Checked ? "Pages" : "Length", "Size (est.)", "Status" };
            int x = 0;
            for (int i = 0; i < 4 && i < lv.Columns.Count; i++)
            {
                int w = lv.Columns[i].Width;
                var r = new Rectangle(x + T.P(8), 0, w - T.P(16), lvHead.Height);
                TextRenderer.DrawText(g, names[i], T.Small, r, T.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                    (i == 1 || i == 2 ? TextFormatFlags.Right : TextFormatFlags.Left));
                x += w;
            }
        }
        void PlaceQueueLabels()
        {
            if (lblSummary == null) return;
            lblSummary.Size = lblSummary.PreferredSize;
            lblSummary.Location = new Point(cQueue.Width - T.P(16) - lblSummary.Width, T.P(14) + (T.P(24) - lblSummary.Height) / 2);
            lblEmpty.Size = lblEmpty.PreferredSize;
            lblEmpty.Location = new Point((lv.ClientSize.Width - lblEmpty.Width) / 2, (lv.ClientSize.Height - lblEmpty.Height) / 2);
            lblEmpty.Visible = items.Count == 0;
        }
        void PlaceTools()
        {
            if (btnUpdate == null) return;
            int cy = (header.Height - btnUpdate.Height) / 2;
            btnAbout.Location = new Point(header.Width - T.P(16) - btnAbout.Width, cy);
            btnSkin.Location = new Point(btnAbout.Left - T.P(8) - btnSkin.Width, cy);
            btnUpdate.Location = new Point(btnSkin.Left - T.P(12) - btnUpdate.Width, cy);
            lblTools.Size = lblTools.PreferredSize;
            lblTools.Location = new Point(btnUpdate.Left - T.P(14) - lblTools.Width, (header.Height - lblTools.Height) / 2);
        }

        void UpdateToolsLabel()
        {
            var parts = new List<string>();
            parts.Add(ytdlp == null ? "yt-dlp MISSING" : ("yt-dlp " + (ytdlpVersion == "" ? "\u2713" : ytdlpVersion)));
            parts.Add(ffmpeg == null ? "ffmpeg MISSING" : "ffmpeg \u2713");
            parts.Add(deno == null ? "deno missing" : "deno \u2713");
            lblTools.Text = string.Join("   \u00B7   ", parts.ToArray());
            PlaceTools();
            var missing = new List<string>();
            if (ytdlp == null) missing.Add("winget install yt-dlp.yt-dlp");
            if (ffmpeg == null) missing.Add("winget install Gyan.FFmpeg");
            if (deno == null) missing.Add("winget install DenoLand.Deno   (recommended for YouTube)");
            tip.SetToolTip(lblTools, missing.Count == 0 ? "All tools found." : "Click 'Install missing tools' (right), or install them yourself in PowerShell:\n" + string.Join("\n", missing.ToArray()));
            bool need = missing.Count > 0;
            string bt = need ? "Install missing tools" : "Update yt-dlp";
            if (btnUpdate.Text != bt)
            {
                btnUpdate.Text = bt; btnUpdate.IconName = need ? "download" : "refresh";
                btnUpdate.Size = new Size(btnUpdate.PreferredWidth(T.P(36)), T.P(36));
                tip.SetToolTip(btnUpdate, need ? "Downloads the missing tools from their official sites into your user folder. No admin rights needed." : "Get the newest yt-dlp (fixes most YouTube errors).");
                PlaceTools();
            }
            if (!installing && (ytdlp == null || ffmpeg == null)) SetStatus("Some helper tools are missing - click 'Install missing tools' (top right).", T.Bad);
        }

        // ---------------- settings
        void LoadSettings()
        {
            var d = new Dictionary<string, string>();
            try { foreach (var line in File.ReadAllLines(settingsFile)) { int i = line.IndexOf('='); if (i > 0) d[line.Substring(0, i)] = line.Substring(i + 1); } } catch { }
            string v;
            txtOut.Text = d.TryGetValue("OutDir", out v) ? v : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Path.Combine("Downloads", "Dawnloader"));
            string mode = d.TryGetValue("Mode", out v) ? v : "Video";
            tVideo.Checked = mode == "Video"; tAudio.Checked = mode == "Audio"; tDoc.Checked = mode == "Doc";
            SetIdx(cbQuality, d, "Quality", 3); SetIdx(cbContainer, d, "Container", 0); SetIdx(cbAFmt, d, "AFmt", 0);
            SetIdx(cbABr, d, "ABr", 0); SetIdx(cbPages, d, "PagesMode", 0);
            txtPages.Text = d.TryGetValue("Pages", out v) ? v : "";
            if (d.TryGetValue("CookieFile", out v)) cookiesFilePath = v;
            SetIdx(cbCookies, d, "Cookies", 0);
            if (cbCookies.SelectedIndex == 5 && !File.Exists(cookiesFilePath)) cbCookies.SelectedIndex = 0;
            ckSubs.Checked = d.TryGetValue("Subs", out v) && v == "1"; ckThumb.Checked = !d.TryGetValue("Thumb", out v) || v == "1";
            ckCatch.Checked = d.TryGetValue("Catch", out v) && v == "1";
        }
        void SetIdx(ComboBox c, Dictionary<string, string> d, string key, int def)
        {
            string v; int i;
            c.SelectedIndex = d.TryGetValue(key, out v) && int.TryParse(v, out i) && i >= 0 && i < c.Items.Count ? i : def;
        }
        void SaveSettings()
        {
            try
            {
                File.WriteAllLines(settingsFile, new[] {
                    "OutDir=" + txtOut.Text, "Mode=" + (tAudio.Checked ? "Audio" : tDoc.Checked ? "Doc" : "Video"), "Quality=" + cbQuality.SelectedIndex,
                    "Container=" + cbContainer.SelectedIndex, "AFmt=" + cbAFmt.SelectedIndex, "ABr=" + cbABr.SelectedIndex,
                    "PagesMode=" + cbPages.SelectedIndex, "Pages=" + txtPages.Text.Replace("\r", "").Replace("\n", ""),
                    "Cookies=" + cbCookies.SelectedIndex, "CookieFile=" + cookiesFilePath,
                    "Subs=" + (ckSubs.Checked ? 1 : 0), "Thumb=" + (ckThumb.Checked ? 1 : 0), "Catch=" + (ckCatch.Checked ? 1 : 0), "Skin=" + skinName });
            }
            catch { }
        }

        // ---------------- options
        Opts GetOpts()
        {
            var o = new Opts();
            o.Video = tVideo.Checked; o.Doc = tDoc.Checked; o.Height = Heights[Math.Max(0, cbQuality.SelectedIndex)];
            o.Container = new[] { "mp4", "mkv", "webm" }[Math.Max(0, cbContainer.SelectedIndex)];
            o.AFmt = new[] { "mp3", "m4a", "opus", "flac", "wav", "best" }[Math.Max(0, cbAFmt.SelectedIndex)];
            o.ABr = new[] { "0", "320K", "192K", "128K" }[Math.Max(0, cbABr.SelectedIndex)];
            o.Subs = ckSubs.Checked; o.Thumb = ckThumb.Checked;
            o.Cookies = cbCookies.SelectedIndex <= 0 ? "" : cbCookies.SelectedIndex == 5 ? "file" : cbCookies.Text.ToLowerInvariant();
            o.CookieFile = cookiesFilePath;
            o.Pages = cbPages.SelectedIndex == 1 ? txtPages.Text.Trim() : "";
            return o;
        }

        void PickMode(Tile t)
        {
            if (busy) return;
            tVideo.Checked = t == tVideo; tAudio.Checked = t == tAudio; tDoc.Checked = t == tDoc;
            ApplyMode(); OptionsChanged();
        }

        void ApplyMode()
        {
            bool v = tVideo.Checked, doc = tDoc.Checked;
            pVideo.Visible = v; pAudio.Visible = tAudio.Checked; pDoc.Visible = doc;
            ckSubs.Enabled = v && !busy; ckThumb.Enabled = !doc && !busy;
            string f = GetOpts().AFmt;
            cbABr.Enabled = !busy && (f == "mp3" || f == "m4a" || f == "opus");
            string hint;
            switch (f)
            {
                case "mp3": hint = "Plays everywhere - the safe choice."; break;
                case "m4a": hint = "AAC - great on phones and Apple devices."; break;
                case "opus": hint = "Smallest files for the same quality."; break;
                case "flac": hint = "Lossless copy of the original audio - large files."; break;
                case "wav": hint = "Uncompressed - very large files."; break;
                default: hint = "Keeps the site's original audio, no re-encoding."; break;
            }
            SetHint(lblAudioHint, hint);
            cbContainer.Enabled = !busy;
            var vo = GetOpts(); string vh;
            if (vo.Container == "webm") vh = "VP9/AV1 - fine on PCs and phones, not on every TV.";
            else if (vo.Height == 0) vh = "Highest resolution (4K uses VP9/AV1 - plays on modern devices).";
            else if (vo.Height <= 1080) vh = "H.264 - plays on any TV, phone or PC.";
            else vh = "Above 1080p only VP9/AV1 exists - plays on modern TVs, phones and PCs.";
            SetHint(lblVideoHint, vh);
            bool choose = cbPages.SelectedIndex == 1;
            txtPages.Visible = choose; txtPages.Enabled = !busy; cbPages.Enabled = !busy;
            string err = null;
            if (choose && txtPages.Text.Trim() != "") Pdf.CountRange(txtPages.Text, 0, out err);
            if (!choose) SetHint(lblDocHint, "Google Docs, Slides, Sheets, PDF links and web pages - one PDF each.");
            else if (txtPages.Text.Trim() == "") SetHint(lblDocHint, "Type the pages to keep, e.g. 1-3, 5, 8-10");
            else SetHint(lblDocHint, err ?? "Same pages from every document in the list.");
            lblDocHint.ForeColor = err != null ? T.Bad : T.Muted;
            if (lvHead != null) lvHead.Invalidate();
        }

        void OptionsChanged()
        {
            if (loading) return;
            ApplyMode();
            string ck = GetOpts().Cookies;
            if (ck != lastCookies)
            {
                lastCookies = ck; exportedCookies = null;
                foreach (var it in items) if (it.State == 3 || it.UseCookies) { it.State = 0; it.UseCookies = false; SetItemStatus(it, "Checking...", ""); }
            }
            bool doc = tDoc.Checked;
            if (doc != lastDoc)
            {
                lastDoc = doc;     // web pages are videos in Video/Audio mode and PDFs in Document mode -> check again
                foreach (var it in items) if (it.Kind == "other" || it.Kind == "youtube") ResetItem(it);
            }
            // playlist checkbox changes what a "video in a playlist" link means -> re-check those links
            foreach (var it in items)
            {
                bool want = WantsPlaylist(it);
                if (want != it.IsPlaylist && it.Kind != "drive" && !it.IsDoc) { it.IsPlaylist = want; it.State = 0; it.Formats = null; it.Est = -1; SetItemStatus(it, "Checking...", ""); }
            }
            RecomputeAll(); FetchNext();
        }

        void ResetItem(Item it)
        {
            it.State = 0; it.Formats = null; it.Est = -1; it.Error = null; it.Probed = false; it.ProbeSize = -1; it.Pages = -1; it.Archived = false;
            it.DocKind = DocKindOf(it); it.Done = false;
            if (it.Lvi != null) { it.Lvi.SubItems[1].Text = ""; it.Lvi.SubItems[2].Text = ""; it.Lvi.ImageKey = it.IsDoc ? "doc" : "ph"; }
            SetItemStatus(it, "Checking...", "");
        }

        // ---------------- links -> items
        static string CleanUrl(string u)
        {
            u = u.Trim();
            u = Regex.Replace(u, @"([?&])si=[^&]*&?", "$1");
            return u.TrimEnd('?', '&');
        }
        static bool IsPurePlaylist(string u) { return Regex.IsMatch(u, @"tiktok\.com/@[\w.-]+/?(\?|$)", RegexOptions.IgnoreCase) || (u.Contains("list=") && !u.Contains("v=") && !u.Contains("youtu.be/") && !u.Contains("/shorts/")) || u.Contains("/drive/folders/") || u.Contains("/drive/u/"); }
        static string KindOf(string u)
        {
            string id;
            if (Web.GDoc(u, out id) != null) return "gdoc";
            if (Regex.IsMatch(u, @"^https?://([\w-]+\.)?(youtube\.com|youtu\.be|youtube-nocookie\.com)/", RegexOptions.IgnoreCase)) return "youtube";
            if (Web.DriveId(u) != null) return "drive";
            if (Web.IsPdfUrl(u)) return "pdf";
            return "other";
        }
        string DocKindOf(Item it)
        {
            string id;
            if (it.Kind == "gdoc") return Web.GDoc(it.Url, out id);
            if (it.Kind == "pdf") return "pdf";
            if (it.Kind == "other" && tDoc.Checked) return "web";
            return "";
        }
        static string Badge(Item it)
        {
            switch (it.DocKind)
            {
                case "document": return "Doc"; case "presentation": return "Slides"; case "spreadsheets": return "Sheet";
                case "pdf": return "PDF"; case "web": return "Web page";
            }
            if (it.HasClip) return "Clip";
            return "";
        }
        // Playlist links download the whole list automatically. A video opened from a playlist (watch?v=..&list=..)
        // downloads just that video unless the user right-clicks the row and asks for the whole playlist.
        bool WantsPlaylist(Item it) { return IsPurePlaylist(it.Url) || (it.PlForce && InPlaylist(it)); }
        static bool InPlaylist(Item it) { return it.Kind == "youtube" && Regex.IsMatch(it.Url, @"[?&]list=[\w-]+") && !IsPurePlaylist(it.Url); }
        static bool IsMix(string u) { return Regex.IsMatch(u, @"[?&]list=RD"); }

        List<string> CurrentUrls()
        {
            var list = new List<string>();
            foreach (var line in txtLinks.Lines)
            {
                foreach (var part in line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!Regex.IsMatch(part, "^https?://", RegexOptions.IgnoreCase)) continue;
                    string u = CleanUrl(part);
                    if (!list.Contains(u)) list.Add(u);
                }
            }
            return list;
        }

        void SyncItems()
        {
            if (busy) return;
            var urls = CurrentUrls();
            var keep = new List<Item>(); int added = 0;
            foreach (var u in urls)
            {
                Item found = null;
                foreach (var it in items) if (it.Url == u) { found = it; break; }
                if (found == null)
                {
                    found = new Item(); found.Url = u; found.Title = u; found.IsPlaylist = IsPurePlaylist(u);
                    found.Kind = KindOf(u); if (found.Kind == "drive") { found.DriveId = Web.DriveId(u); found.IsPlaylist = false; found.Title = "Google Drive file"; }
                    if (found.Kind == "gdoc" || found.Kind == "pdf") found.IsPlaylist = false;
                    found.DocKind = DocKindOf(found);
                    found.Lvi = new ListViewItem(new[] { found.Kind == "drive" ? "Google Drive file" : u, "", "", "Checking..." }); found.Lvi.ImageKey = found.IsDoc ? "doc" : "ph"; found.Lvi.UseItemStyleForSubItems = false;
                    found.Lvi.Tag = found; added++;
                }
                keep.Add(found);
            }
            foreach (var it in items) if (!keep.Contains(it) && it == fetching && fetchRunner != null) { fetchRunner.Kill(); }
            items = keep;
            lv.BeginUpdate(); lv.Items.Clear(); foreach (var it in items) lv.Items.Add(it.Lvi); lv.EndUpdate();
            // only Google Docs / PDF links pasted -> switch to Document mode by itself
            if (added > 0 && items.Count > 0 && !tDoc.Checked)
            {
                bool allDocs = true; foreach (var it in items) if (it.Kind != "gdoc" && it.Kind != "pdf") allDocs = false;
                if (allDocs) { PickMode(tDoc); SetStatus("Document links found - they will be saved as PDF.", T.Muted); }
            }
            PlaceQueueLabels(); UpdateSummary(); FetchNext();
        }

        // ---------------- fetching info (one link at a time, in the background)
        void FetchNext()
        {
            if (fetchRunner != null || driveFetching) return;
            Item next = null;
            foreach (var it in items) if (it.State == 0 && (ytdlp != null || it.Kind == "drive" || it.IsDoc)) { next = it; break; }
            if (next == null) { UpdateSummary(); return; }
            fetching = next; next.State = 1; SetItemStatus(next, "Checking...", "");
            if (next.IsDoc) { FetchDoc(next); return; }
            if (next.Kind == "drive") { FetchDrive(next); return; }
            if (tDoc.Checked && next.Kind == "youtube")
            {
                next.State = 3; next.Error = "video"; next.Lvi.Text = next.Url;
                SetItemStatus(next, "Video link - not a document", "bad", "video"); FetchNext(); return;
            }
            fetchStage = next.IsPlaylist ? 1 : 2;
            RunFetch(next, next.Url);
        }

        void RunFetch(Item it, string url)
        {
            var o = GetOpts();
            var a = new List<string> { "--no-warnings", "--ignore-config" };
            if (it.UseCookies) CookieArgs(o, a);
            if (fetchStage == 1) { a.Add("--yes-playlist"); a.Add("--flat-playlist"); a.Add("-J"); }
            else
            {
                a.Add("--no-playlist");
                a.Add("--print"); a.Add("%(.{id,title,channel,uploader,duration,extractor_key,playlist_count,thumbnail,webpage_url_domain})j");
                a.Add("--print"); a.Add("%(formats.:.{format_id,ext,vcodec,acodec,width,height,tbr,abr,filesize,filesize_approx,protocol,url})j");
            }
            a.Add("--"); a.Add(url);
            fetchOut.Length = 0;
            fetchRunner = new Runner(this);
            fetchRunner.Line += s => { fetchOut.AppendLine(s); };
            fetchRunner.Exited += code => FetchDone(it, code);
            try { fetchRunner.Start(ytdlp, a, pathEnv); }
            catch (Exception ex) { fetchRunner = null; it.State = 3; SetItemStatus(it, "Can't run yt-dlp", "bad"); Log("ERROR starting yt-dlp: " + ex.Message); }
        }

        void FetchDone(Item it, int code)
        {
            var r = fetchRunner; fetchRunner = null;
            string output = fetchOut.ToString();
            if (!items.Contains(it) || (r != null && r.Killed)) { if (items.Contains(it)) it.State = 0; FetchNext(); return; }
            try
            {
                if (fetchStage == 1)
                {
                    var js = new JavaScriptSerializer(); js.MaxJsonLength = int.MaxValue;
                    string json = FirstJsonLine(output, '{');
                    if (code != 0 || json == null) throw new Exception(ErrorText(output));
                    var d = (Dictionary<string, object>)js.DeserializeObject(json);
                    it.Title = Str(d, "title", it.Url); it.Channel = Str(d, "channel", Str(d, "uploader", ""));
                    var entries = d.ContainsKey("entries") ? d["entries"] as object[] : null;
                    it.Count = entries == null ? 0 : entries.Length;
                    double total = 0; int known = 0; string firstUrl = null;
                    it.PlKeys = new List<string>(); var arch = Archive(GetOpts());
                    if (entries != null)
                        foreach (var eo in entries)
                        {
                            var e = eo as Dictionary<string, object>; if (e == null) continue;
                            string ek = (Str(e, "ie_key", "") ?? "").ToLowerInvariant(), eid = Str(e, "id", "");
                            if (ek != "" && eid != "") it.PlKeys.Add(ek + " " + eid);
                            double du = Num(e, "duration"); if (du > 0) { total += du; known++; }
                            if (firstUrl == null) { firstUrl = Str(e, "url", null); if (firstUrl == null && Str(e, "id", null) != null) firstUrl = "https://www.youtube.com/watch?v=" + Str(e, "id", ""); }
                        }
                    it.PlDuration = known > 0 ? total / known * it.Count : 0;
                    it.PlHave = 0; foreach (var k in it.PlKeys) if (arch.Contains(k)) it.PlHave++;
                    it.Lvi.Text = it.Title + "  (playlist, " + it.Count + " videos)";
                    it.Lvi.SubItems[1].Text = T.Dur(it.PlDuration);
                    Log("Playlist \"" + it.Title + "\": " + it.Count + " videos");
                    if (firstUrl == null) throw new Exception("Playlist is empty or private.");
                    fetchStage = 3;  // sample the first video for bitrate
                    RunFetch(it, firstUrl);
                    return;
                }
                // stage 2 (single) or 3 (playlist sample)
                string meta = FirstJsonLine(output, '{'), fm = FirstJsonLine(output, '[');
                if (code != 0 || meta == null || fm == null) throw new Exception(ErrorText(output));
                var js2 = new JavaScriptSerializer(); js2.MaxJsonLength = int.MaxValue;
                var md = (Dictionary<string, object>)js2.DeserializeObject(meta);
                var arr = (object[])js2.DeserializeObject(fm);
                var list = new List<Fmt>();
                foreach (var fo in arr)
                {
                    var f = fo as Dictionary<string, object>; if (f == null) continue;
                    var x = new Fmt();
                    x.Id = Str(f, "format_id", ""); x.Ext = Str(f, "ext", ""); x.V = Str(f, "vcodec", "none") ?? "none"; x.A = Str(f, "acodec", "none") ?? "none";
                    x.Proto = Str(f, "protocol", ""); x.Url = Str(f, "url", ""); x.H = (int)Num(f, "height"); int w = (int)Num(f, "width"); if (w > 0 && x.H > 0) x.H = Math.Min(w, x.H); x.Tbr = Num(f, "tbr"); x.Abr = Num(f, "abr");
                    x.Size = Num(f, "filesize"); if (x.Size <= 0) x.Size = Num(f, "filesize_approx");
                    list.Add(x);
                }
                it.Formats = list; it.Duration = Num(md, "duration");
                if (fetchStage == 2)
                {
                    it.Id = Str(md, "id", ""); it.Title = Str(md, "title", it.Url); it.Channel = Str(md, "channel", Str(md, "uploader", ""));
                    it.Lvi.Text = it.Title; it.Lvi.SubItems[1].Text = T.Dur(it.Duration);
                    it.Extractor = Str(md, "extractor_key", "");
                    if (it.Extractor.StartsWith("Youtube") && it.Id != "") LoadThumb(it, "https://i.ytimg.com/vi/" + it.Id + "/mqdefault.jpg");
                    else { string tu = Str(md, "thumbnail", ""); if (tu.StartsWith("http")) LoadThumb(it, tu); }
                    string dom = Str(md, "webpage_url_domain", "");
                    it.ArchiveKey = it.Extractor != "" && it.Id != "" ? it.Extractor.ToLowerInvariant() + " " + it.Id : "";
                    it.Archived = !it.ForceAgain && it.ArchiveKey != "" && Archive(GetOpts()).Contains(it.ArchiveKey);
                    if (it.Archived && it.DonePath == null) { string hp = HistoryPath(it.ArchiveKey); if (hp != null) it.DonePath = hp; }
                    if (it.Extractor != "" && it.Extractor != "Generic") it.Site = SiteName(it.Extractor, dom);
                }
                else
                {
                    string id = Str(md, "id", "");
                    if (id != "" && Str(md, "extractor_key", "").StartsWith("Youtube")) LoadThumb(it, "https://i.ytimg.com/vi/" + id + "/mqdefault.jpg");
                    if (it.PlDuration <= 0) it.PlDuration = it.Duration * it.Count;
                    it.Lvi.SubItems[1].Text = T.Dur(it.PlDuration);
                }
                int maxH = 0; foreach (var f in list) if (f.IsVideoOnly || f.IsMuxed) maxH = Math.Max(maxH, f.H);
                it.Lvi.ToolTipText = it.Title + (it.Channel != "" ? "\n" + it.Channel : "") + (it.Site != "" ? "\n" + it.Site : "") + (maxH > 0 ? "\nBest available: " + maxH + "p" : "");
                it.State = 2;
                Recompute(it);
            }
            catch (Exception ex)
            {
                if (NeedsSignIn(ex.Message, it) && !it.UseCookies && GetOpts().Cookies != "")
                {
                    it.UseCookies = true; it.State = 0;            // try this one link again, signed in
                    Log("Needs sign-in, retrying with " + GetOpts().Cookies + ": " + it.Url);
                    UpdateSummary(); FetchNext(); return;
                }
                it.State = 3; it.Error = ex.Message;
                string st = Friendly(ex.Message, 30), act = ActionFor(ex.Message, it);
                if (IsDrm(ex.Message, it.Url)) { st = "Copy-protected (DRM)"; act = ""; }
                else if (NeedsSignIn(ex.Message, it)) st = GetOpts().Cookies == "" ? "Needs sign-in" : "No access with this sign-in";
                SetItemStatus(it, st, "bad", act);
                it.Lvi.ToolTipText = IsDrm(ex.Message, it.Url) ? "This service uses copy protection (DRM), so Dawnloader can't download it.\n\n" + ex.Message : ex.Message;
                Log("Could not read " + it.Url + ": " + ex.Message);
            }
            UpdateSummary();
            FetchNext();
        }

        static string SiteName(string ex, string dom)
        {
            switch (ex.ToLowerInvariant())
            {
                case "tiktok": case "tiktokvm": return "TikTok";
                case "instagram": case "instagramstory": case "instagramios": return "Instagram";
                case "facebook": case "facebookreel": return "Facebook";
                case "twitter": return "X / Twitter";
            }
            return dom != "" ? dom : ex;
        }

        static bool NeedsSignIn(string s, Item it)
        {
            if (Regex.IsMatch(s, @"not a bot|sign in|sign-in|log ?in|logged.in|cookies|Private video|members-only|Join this channel|confirm your age|age.restricted|inappropriate|account|registered users|rate-limit|private account|only available for", RegexOptions.IgnoreCase)) return true;
            return it.Kind != "youtube" && Regex.IsMatch(s, @"HTTP Error (40[13]|429)");
        }

        static bool IsDrm(string s, string url)
        {
            if (Regex.IsMatch(s ?? "", @"\bDRM\b|copy.?protect|Widevine|PlayReady", RegexOptions.IgnoreCase)) return true;
            return Regex.IsMatch(url ?? "", @"^https?://([\w-]+\.)*(netflix\.com|disneyplus\.com|hotstar\.com|primevideo\.com|max\.com|hbomax\.com|open\.spotify\.com|music\.apple\.com|tv\.apple\.com|viu\.com|iq\.com|astrogo\.astro\.com\.my|sooka\.my|hulu\.com|paramountplus\.com|peacocktv\.com|crunchyroll\.com)/", RegexOptions.IgnoreCase)
                && Regex.IsMatch(s ?? "", "Unsupported URL|DRM|not supported|No video", RegexOptions.IgnoreCase);
        }

        // which fix-it button suits this error
        string ActionFor(string s, Item it)
        {
            s = s ?? "";
            if (IsDrm(s, it.Url)) return "";
            if (NeedsSignIn(s, it)) return GetOpts().Cookies == "" ? "firefox" : "";
            if (Regex.IsMatch(s, "HTTP Error 403|Requested format is not available|Sign in to confirm|nsig|signature|player", RegexOptions.IgnoreCase) && it.Kind == "youtube") return "update";
            if (Regex.IsMatch(s, "Unsupported URL|No video formats found|no video in this post|There is no video|No video could be found", RegexOptions.IgnoreCase)) return it.Kind == "other" ? "pdf" : "";
            if (Regex.IsMatch(s, "unavailable|removed|terminated|does not exist|Private video|HTTP Error 404", RegexOptions.IgnoreCase)) return "";
            return "retry";
        }

        static string Friendly(string s, int max)
        {
            if (IsDrm(s, "")) return "Copy-protected (DRM)";
            if (Regex.IsMatch(s, "not a bot", RegexOptions.IgnoreCase)) return "Needs sign-in: set Sign-in to Firefox";
            if (Regex.IsMatch(s, "Private video", RegexOptions.IgnoreCase)) return "Private video";
            if (Regex.IsMatch(s, "HTTP Error 403", RegexOptions.IgnoreCase)) return "Blocked by YouTube (403)";
            if (Regex.IsMatch(s, "age", RegexOptions.None) && Regex.IsMatch(s, "confirm your age|age-restricted|inappropriate", RegexOptions.IgnoreCase)) return "Age-restricted: needs Sign-in";
            if (Regex.IsMatch(s, "members-only|Join this channel", RegexOptions.IgnoreCase)) return "Members-only video";
            if (Regex.IsMatch(s, "unavailable|removed|terminated|does not exist", RegexOptions.IgnoreCase)) return "Video unavailable";
            if (Regex.IsMatch(s, "Unsupported URL", RegexOptions.IgnoreCase)) return "Not a supported link";
            if (Regex.IsMatch(s, "IP address is blocked|not available in your (country|region)|geo.?restrict", RegexOptions.IgnoreCase)) return "Blocked in your country / network";
            if (Regex.IsMatch(s, "rate-limit|login required|registered users|log in|logged.in", RegexOptions.IgnoreCase)) return "Needs sign-in - set Sign-in";
            if (Regex.IsMatch(s, "No video formats found|no video in this post|There is no video|No video could be found", RegexOptions.IgnoreCase)) return "No video in this post";
            if (Regex.IsMatch(s, "HTTP Error 429|Too Many Requests", RegexOptions.IgnoreCase)) return "Limited by site - set Sign-in";
            if (Regex.IsMatch(s, "HTTP Error 404", RegexOptions.IgnoreCase)) return "Not found - check the link";
            if (Regex.IsMatch(s, "getaddrinfo|timed out|Connection|Network is unreachable", RegexOptions.IgnoreCase)) return "Network problem - check internet";
            s = Regex.Replace(s, @"^\[[^\]]+\]\s*[\w-]+:\s*", "");
            return s.Length > max ? s.Substring(0, max - 2) + "..." : s;
        }

        static string FirstJsonLine(string output, char start)
        {
            foreach (var l in output.Split('\n')) { var t = l.Trim(); if (t.Length > 1 && t[0] == start) return t; }
            return null;
        }
        static string ErrorText(string output)
        {
            foreach (var l in output.Split('\n')) if (l.StartsWith("ERROR:")) return Regex.Replace(l.Substring(6).Trim(), @"^\[[^\]]+\]\s*[\w-]+:\s*", "");
            return "Could not read this link.";
        }
        static string Str(Dictionary<string, object> d, string k, string def) { object v; return d.TryGetValue(k, out v) && v != null ? v.ToString() : def; }
        static double Num(Dictionary<string, object> d, string k)
        {
            object v; if (!d.TryGetValue(k, out v) || v == null) return 0;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); } catch { return 0; }
        }

        void LoadThumb(Item it, string url)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var wc = new WebClient();
                wc.DownloadDataCompleted += (s, e) =>
                {
                    try
                    {
                        if (e.Error != null || e.Cancelled) return;
                        using (var ms = new MemoryStream(e.Result)) using (var img = Image.FromStream(ms))
                        {
                            string key = "t" + it.GetHashCode();
                            thumbs.Images.Add(key, ThumbFrom(img));
                            it.Lvi.ImageKey = key; lv.Invalidate();
                        }
                    }
                    catch { }
                    finally { wc.Dispose(); }
                };
                wc.DownloadDataAsync(new Uri(url));
            }
            catch { }
        }

        // ---------------- estimates
        void Recompute(Item it)
        {
            if (it.State != 2) return;
            string desc; var o = GetOpts();
            if (it.IsDoc) { RecomputeDoc(it, o); return; }
            if (it.Kind == "drive")
            {
                it.Est = it.ProbeSize;
                string ext = Path.GetExtension(it.FileName ?? "").TrimStart('.').ToUpperInvariant();
                if (o.Video || o.Doc) { desc = "original " + (ext != "" ? ext : "file"); it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : T.Bytes(it.Est); }
                else { desc = "original \u2192 " + (o.AFmt == "best" ? "audio" : o.AFmt.ToUpperInvariant()); it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : "\u2264 " + T.Bytes(it.Est); }
                it.Lvi.ToolTipText = (it.FileName ?? "") + "\nGoogle Drive original file" + (it.Est >= 0 ? ", " + T.Bytes(it.Est) : "") +
                    (o.Video || o.Doc ? "" : "\nAudio is extracted after download, so the final file is smaller.");
                if (!busy && !it.Done) SetItemStatus(it, desc + (it.UseCookies ? " \u00B7 signed in" : ""), "", it.DonePath != null ? "show" : "");
                return;
            }
            it.Est = Est.ForItem(it, o, out desc);
            if (it.Est < 0 && it.ProbeSize > 0) { it.Est = it.ProbeSize; if (desc == "") desc = "Ready"; }
            bool exact = it.Est >= 0 && it.Est == it.ProbeSize && o.Video;
            string extra = "";
            if (it.HasClip && !it.IsPlaylist)
            {
                double len = (it.Duration > 0 ? Math.Min(it.ClipEnd, it.Duration) : it.ClipEnd) - it.ClipStart;
                if (it.Est > 0 && it.Duration > 0) it.Est = it.Est * Math.Max(0.01, len / it.Duration) + 200000;
                exact = false;
                it.Lvi.SubItems[1].Text = T.Dur(len) + " clip";
                extra += " \u00B7 clip " + Clock(it.ClipStart) + "-" + Clock(it.ClipEnd);
            }
            else if (!it.IsPlaylist) it.Lvi.SubItems[1].Text = T.Dur(it.Duration);
            if (it.IsPlaylist && it.Count > 0 && it.PlHave > 0 && !it.ForceAgain)
            {
                int fresh = Math.Max(0, it.Count - it.PlHave);
                if (it.Est > 0) it.Est = it.Est * fresh / it.Count;
                extra += fresh == 0 ? "" : " \u00B7 " + fresh + " new";
            }
            it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : (exact ? "" : o.Video ? "\u2248 " : "\u2264 ") + T.Bytes(it.Est);
            if (it.Est >= 0 && it.Est == it.ProbeSize && !o.Video) it.Lvi.SubItems[2].Text = "\u2264 " + T.Bytes(it.Est);
            if (!busy && !it.Done)
            {
                bool allHave = it.IsPlaylist && it.Count > 0 && it.PlHave >= it.Count && !it.ForceAgain;
                if (it.Archived && !it.HasClip) { it.Lvi.SubItems[2].Text = "0 B"; SetItemStatus(it, "Already downloaded", "", it.DonePath != null && File.Exists(it.DonePath) ? "show" : "again"); }
                else if (allHave) { it.Lvi.SubItems[2].Text = "0 B"; SetItemStatus(it, "All " + it.Count + " already downloaded", "", "again"); }
                else SetItemStatus(it, (desc == "" ? "Ready" : desc) + extra + (it.UseCookies ? " \u00B7 signed in" : "") +
                    (InPlaylist(it) && !it.IsPlaylist ? " \u00B7 in a playlist" : ""), "", it.DonePath != null && File.Exists(it.DonePath) ? "show" : "");
            }
            it.Lvi.ToolTipText = (it.Lvi.ToolTipText ?? "").Split(new[] { "\n\nRight-click" }, StringSplitOptions.None)[0] +
                (it.IsPlaylist && it.PlHave > 0 ? "\n" + it.PlHave + " of " + it.Count + " videos were downloaded before and will be skipped." : "") +
                (InPlaylist(it) ? (it.IsPlaylist ? "\n\nRight-click to download only this video." : "\n\nRight-click to download the whole playlist.") :
                 (!it.IsPlaylist ? "\n\nRight-click to download only a part (clip)." : ""));
            if (it.Est < 0 && it.Kind == "other" && !it.Probed && !it.IsPlaylist) ProbeDirect(it);
        }

        static string Clock(double sec) { return sec <= 0 ? "0:00" : T.Dur(sec); }
        static string Stamp(double sec)     // file-name safe: 1m05s, 1h02m03s
        {
            var t = TimeSpan.FromSeconds(Math.Round(Math.Max(0, sec)));
            return t.TotalHours >= 1 ? string.Format("{0}h{1:00}m{2:00}s", (int)t.TotalHours, t.Minutes, t.Seconds) : string.Format("{0}m{1:00}s", t.Minutes, t.Seconds);
        }

        void RecomputeDoc(Item it, Opts o)
        {
            string err = null; int sel = it.Pages;
            if (o.Pages != "" && it.DocKind != "web") sel = Pdf.CountRange(o.Pages, it.Pages, out err);
            else if (o.Pages != "") Pdf.CountRange(o.Pages, 0, out err);
            it.Est = it.ProbeSize;
            if (it.Est > 0 && o.Pages != "" && it.Pages > 0 && sel > 0) it.Est = it.Est * sel / it.Pages;
            it.Lvi.SubItems[1].Text = it.Pages <= 0 ? (o.Pages != "" ? "pages " + o.Pages : "") : (o.Pages != "" && sel > 0 ? sel + " of " + it.Pages : it.Pages + (it.Pages == 1 ? " page" : " pages"));
            it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : (o.Pages != "" ? "\u2248 " : "") + T.Bytes(it.Est);
            if (busy || it.Done) return;
            if (err != null) { SetItemStatus(it, err, "bad"); return; }
            string what;
            switch (it.DocKind)
            {
                case "document": what = "Google Doc \u2192 PDF"; break;
                case "presentation": what = "Google Slides \u2192 PDF"; break;
                case "spreadsheets": what = "Google Sheet \u2192 PDF"; break;
                case "web": what = "Web page \u2192 PDF \u00B7 best effort"; break;
                default: what = "PDF"; break;
            }
            SetItemStatus(it, what + (it.UseCookies ? " \u00B7 signed in" : ""), "", it.DonePath != null && File.Exists(it.DonePath) ? "show" : "");
        }

        void RecomputeAll() { foreach (var it in items) Recompute(it); UpdateSummary(); lv.Invalidate(); }

        void UpdateSummary()
        {
            if (lblSummary == null) return;
            if (items.Count == 0) { lblSummary.Text = ""; PlaceQueueLabels(); return; }
            double total = 0; int unknown = 0, checking = 0;
            foreach (var it in items) { if (it.State < 2) checking++; else if (it.Est < 0) unknown++; else if (!(it.Archived && !it.HasClip)) total += it.Est; }
            var sb = new StringBuilder();
            sb.Append(items.Count == 1 ? "1 link" : items.Count + " links");
            if (total > 0) sb.Append("   \u00B7   \u2248 " + T.Bytes(total));
            if (checking > 0) sb.Append("   \u00B7   checking " + checking + "...");
            Color c = T.Text;
            try
            {
                string full = Path.GetFullPath(txtOut.Text.Trim());
                var di = new DriveInfo(Path.GetPathRoot(full));
                double free = di.AvailableFreeSpace;
                sb.Append("   \u00B7   " + T.Bytes(free) + " free");
                if (total > free * 0.95) { c = T.Bad; sb.Append(" - not enough space!"); }
            }
            catch { }
            lblSummary.Text = sb.ToString(); lblSummary.ForeColor = c;
            PlaceQueueLabels();
        }

        void SetItemStatus(Item it, string text, string kind, string action = "")
        {
            it.Status = text; it.StatusKind = kind; it.Action = action ?? "";
            var si = it.Lvi.SubItems[3]; si.Text = text;
            si.ForeColor = kind == "ok" ? T.Ok : kind == "bad" ? T.Bad : kind == "run" ? T.Accent : T.Muted;
            if (it.Lvi.ListView != null) try { lv.Invalidate(it.Lvi.Bounds); } catch { }
        }

        // ---------------- list drawing + fix-it buttons
        static string ActionLabel(string a)
        {
            switch (a)
            {
                case "update": return "Update yt-dlp";
                case "firefox": return "Use Firefox sign-in";
                case "retry": return "Retry";
                case "show": return "Show in folder";
                case "again": return "Download again";
                case "pdf": return "Save as PDF";
                case "video": return "Switch to Video";
                case "qpdf": return "Get PDF tool";
            }
            return null;
        }

        Rectangle CellRect(ListViewItem lvi, int col)
        {
            var b = lvi.Bounds; int x = b.X;
            for (int i = 0; i < col; i++) x += lv.Columns[i].Width;
            return new Rectangle(x, b.Y, lv.Columns[col].Width, b.Height);
        }
        Rectangle PillRect(Item it, Rectangle cell)
        {
            string lab = ActionLabel(it.Action); if (lab == null) return Rectangle.Empty;
            int w = TextRenderer.MeasureText(lab, T.Small).Width + T.P(18), h = T.P(24);
            return new Rectangle(cell.Right - w - T.P(6), cell.Y + (cell.Height - h) / 2, w, h);
        }
        Item PillAt(Point p)
        {
            var hit = lv.GetItemAt(T.P(4), p.Y); if (hit == null) return null;
            var it = hit.Tag as Item; if (it == null || it.Action == "" || (busy && it.Action != "show")) return null;
            return PillRect(it, CellRect(hit, 3)).Contains(p) ? it : null;
        }
        void ListClick(Point p) { var it = PillAt(p); if (it != null) DoAction(it); }

        void DrawCell(DrawListViewSubItemEventArgs e)
        {
            var g = e.Graphics; var it = e.Item.Tag as Item; var k = T.Cur;
            Rectangle r = CellRect(e.Item, e.ColumnIndex), fill = r;
            if (e.ColumnIndex == lv.Columns.Count - 1) fill.Width = Math.Max(r.Width, lv.ClientSize.Width - r.X);
            Color bg = e.Item.Selected ? T.Solid(k.AccentTint) : k.Input;
            using (var b = new SolidBrush(bg)) g.FillRectangle(b, fill);
            using (var pen = new Pen(Color.FromArgb(k.Dark ? 70 : 120, k.Line))) g.DrawLine(pen, fill.Left, fill.Bottom - 1, fill.Right, fill.Bottom - 1);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            int pad = T.P(8);
            if (e.ColumnIndex == 0)
            {
                int x = r.X + T.P(4);
                var img = e.Item.ImageKey != null && thumbs.Images.ContainsKey(e.Item.ImageKey) ? thumbs.Images[e.Item.ImageKey] : null;
                if (img != null) { g.DrawImage(img, x, r.Y + (r.Height - img.Height) / 2, img.Width, img.Height); x += img.Width + T.P(4); }
                string badge = it != null ? Badge(it) : "";
                if (badge != "")
                {
                    int bw = TextRenderer.MeasureText(badge, T.Small).Width + T.P(10), bh = T.P(20);
                    var br = new Rectangle(x, r.Y + (r.Height - bh) / 2, bw, bh);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = T.Round(br, T.P(5))) using (var b = new SolidBrush(T.Solid(k.Soft))) g.FillPath(b, p);
                    TextRenderer.DrawText(g, badge, T.Small, br, k.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    x += bw + T.P(6);
                }
                TextRenderer.DrawText(g, e.Item.Text, lv.Font, new Rectangle(x, r.Y, r.Right - x - T.P(4), r.Height), k.Text, flags);
            }
            else if (e.ColumnIndex == 3)
            {
                var pr = it != null ? PillRect(it, r) : Rectangle.Empty;
                int right = pr.IsEmpty ? r.Right - pad : pr.Left - T.P(6);
                TextRenderer.DrawText(g, e.SubItem.Text, lv.Font, new Rectangle(r.X + pad, r.Y, Math.Max(0, right - r.X - pad), r.Height), e.SubItem.ForeColor, flags);
                if (!pr.IsEmpty)
                {
                    bool off = busy && it.Action != "show";
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = T.Round(new RectangleF(pr.X + 0.5f, pr.Y + 0.5f, pr.Width - 1, pr.Height - 1), T.P(6)))
                    {
                        using (var b = new SolidBrush(off ? k.Input : T.Solid(k.AccentTint))) g.FillPath(b, p);
                        using (var pen = new Pen(off ? k.Line : k.Accent)) g.DrawPath(pen, p);
                    }
                    TextRenderer.DrawText(g, ActionLabel(it.Action), T.Small, pr, off ? k.TextOff : k.Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
            else
                TextRenderer.DrawText(g, e.SubItem.Text, lv.Font, new Rectangle(r.X + T.P(4), r.Y, r.Width - pad - T.P(4), r.Height), k.Text, flags | TextFormatFlags.Right);
        }

        void DoAction(Item it)
        {
            switch (it.Action)
            {
                case "show": if (it.DonePath != null) ShowInFolder(it.DonePath); else OpenFolder(); break;
                case "firefox":
                    cbCookies.SelectedIndex = 1;    // OptionsChanged re-checks the failed links signed in
                    SetStatus("Sign-in set to Firefox - be signed in to the site in Firefox. Checking again...", T.Muted); break;
                case "update": if (!busy) { foreach (var x in items) if (x.Action == "update") retryAfterUpdate = true; UpdateYtDlp(); } break;
                case "retry":
                    if (busy) return;
                    if (it.State == 3) { ResetItem(it); UpdateSummary(); FetchNext(); }
                    else StartDownloads(new List<Item> { it });
                    break;
                case "again":
                    it.ForceAgain = true; it.Archived = false; it.Done = false; Recompute(it); UpdateSummary();
                    SetStatus("This one will be downloaded again.", T.Muted); break;
                case "pdf": PickMode(tDoc); break;
                case "video": PickMode(tVideo); break;
                case "qpdf": InstallQpdf(); break;
            }
        }

        void BuildRowMenu()
        {
            rowMenu.Items.Clear();
            var pt = lv.PointToClient(Cursor.Position); var hit = lv.GetItemAt(T.P(80), pt.Y);
            if (hit != null) hit.Selected = true;
            var it = hit != null ? hit.Tag as Item : null;
            if (it != null && !busy)
            {
                if (InPlaylist(it))
                {
                    if (it.IsPlaylist) rowMenu.Items.Add("Download only this video", null, (a, b) => SetPlaylist(it, false));
                    else rowMenu.Items.Add(IsMix(it.Url) ? "Download the whole YouTube Mix (auto-generated, can be long)" : "Download the whole playlist", null, (a, b) => SetPlaylist(it, true));
                }
                if (!it.IsPlaylist && !it.IsDoc && it.Kind != "drive" && it.State == 2)
                {
                    rowMenu.Items.Add(it.HasClip ? "Change the clip..." : "Download only a part (clip)...", null, (a, b) => EditClip(it));
                    if (it.HasClip) rowMenu.Items.Add("Download the whole video", null, (a, b) => { it.ClipStart = it.ClipEnd = -1; Recompute(it); UpdateSummary(); });
                }
                if (it.Archived || (it.IsPlaylist && it.PlHave > 0 && !it.ForceAgain) || it.Done)
                    rowMenu.Items.Add("Download again", null, (a, b) => { it.Action = "again"; DoAction(it); });
                if (rowMenu.Items.Count > 0) rowMenu.Items.Add(new ToolStripSeparator());
            }
            if (it != null && it.DonePath != null && File.Exists(it.DonePath)) rowMenu.Items.Add("Show in folder", null, (a, b) => ShowInFolder(it.DonePath));
            if (it != null && !busy) rowMenu.Items.Add("Remove from list", null, (a, b) => RemoveLink(it));
            rowMenu.Items.Add("Open download folder", null, (a, b) => OpenFolder());
        }

        void ShowInFolder(string path)
        {
            try
            {
                if (!File.Exists(path)) { OpenFolder(); return; }
                if (Environment.OSVersion.Platform == PlatformID.Win32NT) Process.Start("explorer.exe", "/select,\"" + path + "\"");
                else Process.Start(Path.GetDirectoryName(path));
            }
            catch (Exception ex) { SetStatus("Can't open folder: " + ex.Message, T.Warn); }
        }

        // ---------------- download
        List<string> BuildArgs(Item it, Opts o, string extractorArgs)
        {
            var a = new List<string> { "--newline", "--ignore-config" };
            if (ffmpeg != null) { a.Add("--ffmpeg-location"); a.Add(Path.GetDirectoryName(ffmpeg)); }
            a.Add("-P"); a.Add(txtOut.Text.Trim().TrimEnd('\\', '/'));
            string clipTag = it.HasClip && !it.IsPlaylist ? " [clip " + Stamp(it.ClipStart) + "-" + Stamp(it.ClipEnd) + "]" : "";
            if (it.IsPlaylist) { a.Add("--yes-playlist"); a.Add("-o"); a.Add("%(playlist_title|Single videos).80B/%(playlist_index&{} - |)s%(title).90B [%(id)s].%(ext)s"); }
            else { a.Add("--no-playlist"); a.Add("-o"); a.Add(it.Kind == "youtube" ? "%(title).150B [%(id)s]" + clipTag + ".%(ext)s" : (it.Extractor == "" || it.Extractor == "Generic") ? "%(title).150B" + clipTag + ".%(ext)s" : "%(title).90B [%(id)s]" + clipTag + ".%(ext)s"); }
            if (clipTag != "")
            {
                a.Add("--download-sections"); a.Add("*" + it.ClipStart.ToString("0.##", CultureInfo.InvariantCulture) + "-" + it.ClipEnd.ToString("0.##", CultureInfo.InvariantCulture));
                a.Add("--force-keyframes-at-cuts");
            }
            else if (!it.ForceAgain) { a.Add("--download-archive"); a.Add(ArchiveFile(o)); }
            bool thumbOk = o.Thumb;
            if (o.Video)
            {
                string res = o.Height > 0 ? "res:" + o.Height : "res";
                string cont = o.Container;
                bool upTo1080 = o.Height > 0 && o.Height <= 1080;
                // MP4 always prefers H.264 where it exists (YouTube has it up to 1080p); above that VP9/AV1 is the only way to get the resolution
                string sort = cont == "mp4" ? res + ",vcodec:h264,acodec:aac"
                            : cont == "webm" ? res + ",ext:webm:webm"
                            : upTo1080 ? res + ",vcodec:h264" : res;
                // Other sites (TikTok, Instagram, Facebook ...) often have H.264 only at a lower size and H.265 above it.
                // H.265 doesn't play on many PCs/TVs, so up to 1080p the plays-anywhere codec comes first there.
                if (it.Kind != "youtube" && upTo1080 && cont != "webm")
                    sort = "vcodec:h264," + res + (cont == "mp4" ? ",acodec:aac" : "");
                a.AddRange(new[] { "-f", "bv*+ba/b", "-S", sort, "--merge-output-format", cont });
                if (cont == "webm") thumbOk = false;
                if (o.Subs) a.AddRange(new[] { "--write-subs", "--write-auto-subs", "--sub-langs", "en.*,en", "--embed-subs", "--compat-options", "no-keep-subs" });
            }
            else
            {
                string src = o.AFmt == "m4a" ? "ba[ext=m4a]/ba/b" : o.AFmt == "opus" ? "ba[acodec=opus]/ba/b" : "ba/b";
                a.AddRange(new[] { "-f", src, "-x" });
                if (o.AFmt != "best") { a.Add("--audio-format"); a.Add(o.AFmt); }
                if (o.AFmt == "mp3" || o.AFmt == "m4a" || o.AFmt == "opus") { a.Add("--audio-quality"); a.Add(o.ABr); }
                if (o.AFmt == "wav") thumbOk = false;
            }
            if (thumbOk) a.Add("--embed-thumbnail");
            if (o.Thumb) a.Add("--embed-metadata");
            if (it.UseCookies) CookieArgs(o, a);
            if (extractorArgs != null) { a.Add("--extractor-args"); a.Add(extractorArgs); }
            a.Add("--"); a.Add(it.Url);
            return a;
        }

        void SetBusy(bool b)
        {
            busy = b;
            btnDownload.Visible = !b; btnCancel.Visible = b; btnUpdate.Enabled = !b;
            txtLinks.ReadOnly = b; btnPaste.Enabled = !b; btnClear.Enabled = !b;
            tVideo.Enabled = !b; tAudio.Enabled = !b; tDoc.Enabled = !b;
            foreach (Control c in new Control[] { cbQuality, cbContainer, cbAFmt, cbABr, ckSubs, ckThumb, cbCookies, txtOut, btnBrowse, cbPages, txtPages }) c.Enabled = !b;
            if (!b) ApplyMode();
            lv.Invalidate();
        }

        void StartDownloads(List<Item> only)
        {
            debounce.Stop(); if (only == null) SyncItems();
            bool needYt = false; foreach (var it in only ?? items) if (!it.IsDoc && it.Kind != "drive") needYt = true;
            if (needYt && (ytdlp == null || ffmpeg == null)) { AskInstall("Downloading needs a few free helper tools that aren't installed yet."); return; }
            if (items.Count == 0) { SetStatus("Paste a link first.", T.Warn); txtLinks.Focus(); return; }
            var o = GetOpts();
            if (o.Pages != "")
            {
                string err; Pdf.CountRange(o.Pages, 0, out err);
                if (err != null) { SetStatus("Pages: " + err, T.Bad); txtPages.Focus(); return; }
                bool anyDoc = false; foreach (var it in only ?? items) if (it.IsDoc && it.State == 2) anyDoc = true;
                if (anyDoc && qpdf == null) { AskQpdf(); return; }
            }
            string outDir = txtOut.Text.Trim();
            try { Directory.CreateDirectory(outDir); }
            catch (Exception ex) { MessageBox.Show(this, "Can't use that folder:\n" + ex.Message, "Dawnloader"); return; }
            queue = new List<Item>();
            foreach (var it in only ?? items)
            {
                if (it.State == 3) { string was = Regex.Replace(it.Status ?? "", "^Skipped: ", ""); SetItemStatus(it, "Skipped: " + (was != "" ? was : Friendly(it.Error ?? "", 22)), "bad", it.Action); continue; }
                if (it.State < 2) { SetItemStatus(it, "Skipped: still checking", "bad"); continue; }
                if (only == null && it.Archived && !it.HasClip) { SetItemStatus(it, "Skipped - already downloaded", "", it.DonePath != null ? "show" : "again"); continue; }
                if (only == null && it.IsPlaylist && it.Count > 0 && it.PlHave >= it.Count && !it.ForceAgain) { SetItemStatus(it, "Skipped - all already downloaded", "", "again"); continue; }
                it.Done = false; SetItemStatus(it, "Waiting", ""); queue.Add(it);
            }
            if (queue.Count == 0) { SetStatus("Nothing new to download - see the Status column.", T.Warn); return; }
            cancelAll = false; dlIndex = 0; bar.Value = 0;
            if (fetchRunner != null) { fetchRunner.Kill(); }
            SetBusy(true);
            Log(""); Log("=== Download started " + DateTime.Now.ToString("HH:mm:ss") + " ===");
            NextDownload();
        }

        void NextDownload()
        {
            if (cancelAll || dlIndex >= queue.Count) { FinishAll(); return; }
            attempt = 0;
            StartItem(queue[dlIndex], null);
        }

        int plIdx, plCount;
        void StartItem(Item it, string extractorArgs)
        {
            if (it.IsDoc) { StartDoc(it); return; }
            if (it.Kind == "drive") { StartDrive(it); return; }
            saw403 = false; sawBot = false; lastErr = null; plIdx = 0; plCount = 0; lastDest = null; plSkipped = 0;
            SetItemStatus(it, attempt > 0 ? "Retrying (" + attempt + "/" + Fallbacks.Length + ")..." : "Starting...", "run");
            it.Lvi.EnsureVisible();
            var args = BuildArgs(it, GetOpts(), extractorArgs);
            var sb = new StringBuilder(); foreach (var x in args) sb.Append(Runner.Quote(x)).Append(' ');
            Log("> yt-dlp " + sb.ToString().Trim());
            dlRunner = new Runner(this);
            dlRunner.Line += s => OnDlLine(it, s);
            dlRunner.Exited += code => OnDlExit(it, code);
            try { dlRunner.Start(ytdlp, args, pathEnv); }
            catch (Exception ex) { Log("ERROR: " + ex.Message); OnDlExit(it, -1); }
        }

        void OnDlLine(Item it, string line)
        {
            Match m = Regex.Match(line, @"^\[download\]\s+([\d.]+)%(.*)");
            if (m.Success)
            {
                double pct = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                string rest = m.Groups[2].Value;
                Match sp = Regex.Match(rest, @"at\s+(\S+)\s+ETA\s+(\S+)");
                string extra = sp.Success ? "  \u00B7  " + sp.Groups[1].Value + "  \u00B7  " + sp.Groups[2].Value + " left" : "";
                string pl = plCount > 0 ? " (" + plIdx + "/" + plCount + ")" : "";
                SetItemStatus(it, "Downloading " + (int)pct + "%" + pl, "run");
                double frac = plCount > 0 ? ((plIdx - 1) + pct / 100.0) / plCount : pct / 100.0;
                bar.Value = (dlIndex + frac) / queue.Count;
                SetStatus("Downloading " + (dlIndex + 1) + " of " + queue.Count + ": " + it.Title + extra, T.Text);
                return;
            }
            Log(line);
            Match pm = Regex.Match(line, @"^\[download\] Downloading item (\d+) of (\d+)");
            if (pm.Success) { plIdx = int.Parse(pm.Groups[1].Value); plCount = int.Parse(pm.Groups[2].Value); }
            var dm = Regex.Match(line, @"^\[(?:download|ExtractAudio|VideoConvertor|FixupM3u8)\] Destination: (.+)$");
            if (dm.Success) lastDest = dm.Groups[1].Value.Trim();
            dm = Regex.Match(line, @"^\[Merger\] Merging formats into ""(.+)""$"); if (dm.Success) lastDest = dm.Groups[1].Value;
            dm = Regex.Match(line, @"^\[download\] (.+) has already been downloaded$"); if (dm.Success) lastDest = dm.Groups[1].Value;
            if (Regex.IsMatch(line, @"^\[download\] .+ has already been recorded in the archive")) plSkipped++;
            if (line.StartsWith("[Merger]")) SetItemStatus(it, "Joining video + audio...", "run");
            else if (line.StartsWith("[ExtractAudio]")) SetItemStatus(it, "Converting to " + GetOpts().AFmt.ToUpperInvariant() + "...", "run");
            else if (line.StartsWith("[EmbedThumbnail]") || line.StartsWith("[Metadata]")) SetItemStatus(it, "Adding cover + tags...", "run");
            if (line.Contains("HTTP Error 403")) saw403 = true;
            if (Regex.IsMatch(line, "confirm you.re not a bot", RegexOptions.IgnoreCase)) sawBot = true;
            if (line.StartsWith("ERROR:")) lastErr = line.Substring(6).Trim();
        }

        void OnDlExit(Item it, int code)
        {
            bool killed = dlRunner != null && dlRunner.Killed; dlRunner = null;
            if (killed || cancelAll) { SetItemStatus(it, "Cancelled", "bad", "retry"); FinishAll(); return; }
            if (code == 0)
            {
                it.Done = true;
                string full = lastDest == null ? null : (Path.IsPathRooted(lastDest) ? lastDest : Path.Combine(txtOut.Text.Trim(), lastDest));
                if (full != null && File.Exists(full)) { it.DonePath = full; if (it.ArchiveKey != "" && !it.HasClip) AddHistory(it.ArchiveKey, full); }
                string msg = "\u2713 Done";
                if (it.IsPlaylist && plSkipped > 0) msg += " \u00B7 " + plSkipped + " already had";
                else if (!it.IsPlaylist && plSkipped > 0) msg = "\u2713 Already downloaded before";
                SetItemStatus(it, msg, "ok", it.DonePath != null ? "show" : "");
                if (it.ArchiveKey != "" && !it.HasClip) { it.Archived = true; archiveCache = null; }
                it.ForceAgain = false;
                dlIndex++; NextDownload(); return;
            }
            if (saw403 && it.Kind == "youtube" && attempt < Fallbacks.Length)
            {
                string ea = Fallbacks[attempt]; attempt++;
                Log("--- YouTube refused the stream (HTTP 403). Automatic retry " + attempt + " of " + Fallbacks.Length + " using " + ea + " ---");
                StartItem(it, ea); return;
            }
            bool signHint = sawBot || NeedsSignIn(lastErr ?? "", it) || (saw403 && it.Kind == "youtube");
            if (signHint && !it.UseCookies && GetOpts().Cookies != "")
            {
                it.UseCookies = true; attempt = 0;
                Log("--- Trying again signed in (" + GetOpts().Cookies + ") ---");
                StartItem(it, null); return;
            }
            string why = sawBot ? "needs sign-in" : (saw403 && it.Kind == "youtube") ? "blocked (403)" : Friendly(lastErr ?? ("exit code " + code), 22);
            string act = sawBot ? (GetOpts().Cookies == "" ? "firefox" : "update") : (saw403 && it.Kind == "youtube") ? "update" : ActionFor(lastErr ?? "", it);
            if (IsDrm(lastErr ?? "", it.Url)) { why = "copy-protected (DRM)"; act = ""; }
            SetItemStatus(it, "Failed: " + why, "bad", act);
            it.Lvi.ToolTipText = lastErr ?? why;
            it.Error = ((saw403 && it.Kind == "youtube") || sawBot) ? "blocked" : "other";
            dlIndex++; NextDownload();
        }

        void FinishAll()
        {
            int ok = 0, failed = 0, blocked = 0;
            int driveSign = 0;
            foreach (var it in queue) { if (it.Done) ok++; else if (it.Status.StartsWith("Failed")) { failed++; if (it.Error == "blocked") blocked++; if (it.Error == "drive-signin") driveSign++; } }
            SetBusy(false);
            if (cancelAll) { SetStatus("Cancelled. " + ok + " finished.", T.Warn); bar.Value = 0; }
            else if (failed == 0 && queue.Count == items.Count) { bar.Value = 1; SetStatus("\u2713 All done - " + ok + (ok == 1 ? " download" : " downloads") + " saved. Use 'Show in folder' on a row to find it.", T.Ok); }
            else
            {
                bar.Value = 1;
                int skipped = items.Count - queue.Count;
                string msg = ok + " done" + (failed > 0 ? ", " + failed + " failed" : "") + (skipped > 0 ? ", " + skipped + " skipped" : "") + ".";
                if (blocked > 0)
                {
                    msg += " Blocked by YouTube - use the buttons in the Status column.";
                    Log(""); Log("YouTube is blocking. Try in order: 1) Update yt-dlp  2) Sign-in = Firefox (logged in to YouTube, browser closed)  3) turn off VPN.");
                }
                else if (driveSign > 0) msg += " Drive file needs sign-in: set Sign-in to Firefox (account with access).";
                else if (failed > 0) msg += " Click Details for the reason.";
                SetStatus(msg, failed > 0 ? T.Bad : T.Ok);
            }
            foreach (var it in items) if (it.State == 0) { FetchNext(); break; }
        }

        void CancelAll()
        {
            if (installing) { installCancel = true; try { if (installReq != null) installReq.Abort(); } catch { } SetStatus("Cancelling...", T.Warn); return; }
            cancelAll = true; driveCancel = true;
            try { var pp = printProc; if (pp != null && !pp.HasExited) pp.Kill(); } catch { }
            if (dlRunner != null) dlRunner.Kill(); else if (!driveRunning) FinishAll();
            for (int i = dlIndex + 1; i < queue.Count; i++) SetItemStatus(queue[i], "Not started", "");
        }

        // ---------------- download history (skip what you already have)
        HashSet<string> archiveCache; string archiveCacheFile; DateTime archiveCacheTime;
        static string ArchiveFile(Opts o) { return Path.Combine(DataDir, o.Video ? "archive-video.txt" : "archive-audio.txt"); }
        HashSet<string> Archive(Opts o)
        {
            string f = ArchiveFile(o);
            DateTime t = File.Exists(f) ? File.GetLastWriteTimeUtc(f) : DateTime.MinValue;
            if (archiveCache != null && archiveCacheFile == f && archiveCacheTime == t) return archiveCache;
            var h = new HashSet<string>();
            try { if (File.Exists(f)) foreach (var l in File.ReadAllLines(f)) { var x = l.Trim(); if (x.Length > 0) h.Add(x); } } catch { }
            archiveCache = h; archiveCacheFile = f; archiveCacheTime = t;
            return h;
        }
        static string HistoryFile { get { return Path.Combine(DataDir, "history.txt"); } }
        void AddHistory(string key, string path)
        {
            try { File.AppendAllText(HistoryFile, key + "\t" + path + "\t" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + Environment.NewLine); } catch { }
        }
        static string HistoryPath(string key)
        {
            string found = null;
            try { if (File.Exists(HistoryFile)) foreach (var l in File.ReadAllLines(HistoryFile)) { var p = l.Split('\t'); if (p.Length >= 2 && p[0] == key) found = p[1]; } } catch { }
            return found != null && File.Exists(found) ? found : null;
        }

        void SetPlaylist(Item it, bool whole)
        {
            it.PlForce = whole; it.IsPlaylist = WantsPlaylist(it);
            it.State = 0; it.Formats = null; it.Est = -1; it.Count = 0; it.PlDuration = 0;
            it.Lvi.Text = it.Title; it.Lvi.SubItems[1].Text = ""; it.Lvi.SubItems[2].Text = "";
            SetItemStatus(it, "Checking...", ""); UpdateSummary(); FetchNext();
        }

        void RemoveLink(Item it)
        {
            var keep = new List<string>();
            foreach (var line in txtLinks.Lines) if (CleanUrl(line.Trim()) != it.Url) keep.Add(line);
            txtLinks.Lines = keep.ToArray(); debounce.Stop(); SyncItems();
        }

        // ---------------- sign-in cookies
        void PickCookiesFile()
        {
            using (var d = new OpenFileDialog())
            {
                d.Title = "Pick a cookies.txt file (Netscape format)"; d.Filter = "Cookies file (*.txt)|*.txt|All files|*.*";
                if (d.ShowDialog(this) == DialogResult.OK) { cookiesFilePath = d.FileName; OptionsChanged(); }
                else { loading = true; cbCookies.SelectedIndex = 0; loading = false; OptionsChanged(); }
            }
        }

        void CookieArgs(Opts o, List<string> a)
        {
            if (o.Cookies == "") return;
            if (o.Cookies == "file")
            {
                // yt-dlp rewrites the cookie file it is given, so hand it a copy and keep the user's file untouched
                try { string tmp = Path.Combine(Path.GetTempPath(), "ytd-cookies-copy.txt"); File.Copy(o.CookieFile, tmp, true); a.Add("--cookies"); a.Add(tmp); } catch { }
                return;
            }
            a.Add("--cookies-from-browser"); a.Add(o.Cookies);
        }

        // Runs on a worker thread. Returns the cookies to use for Google Drive requests (empty container = signed out).
        CookieContainer DriveCookies(Opts o)
        {
            cookieError = null;
            if (o.Cookies == "") return new CookieContainer();
            if (o.Cookies == "file") return Web.LoadCookies(o.CookieFile);
            lock (this)
            {
                if (exportedCookies != null && exportedFor == o.Cookies && (DateTime.Now - exportedAt).TotalMinutes < 10 && File.Exists(exportedCookies))
                    return Web.LoadCookies(exportedCookies);
                if (ytdlp == null) { cookieError = "yt-dlp is needed to read the browser sign-in"; return new CookieContainer(); }
                string tmp = Path.Combine(Path.GetTempPath(), "ytd-cookies-" + o.Cookies + ".txt");
                try { File.Delete(tmp); } catch { }
                var psi = new ProcessStartInfo(ytdlp, "--ignore-config --no-warnings --cookies-from-browser " + o.Cookies + " --cookies " + Runner.Quote(tmp) + " --simulate ytd-cookie-export:");
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
                psi.EnvironmentVariables["PATH"] = pathEnv;
                string err = "";
                try
                {
                    using (var p = Process.Start(psi))
                    {
                        var errTask = p.StandardError.ReadToEndAsync(); p.StandardOutput.ReadToEnd();
                        p.WaitForExit(60000); err = errTask.Result;
                    }
                }
                catch (Exception ex) { err = ex.Message; }
                int lines = 0;
                try { foreach (var l in File.ReadAllLines(tmp)) if (l.Length > 0 && (!l.StartsWith("#") || l.StartsWith("#HttpOnly_"))) lines++; } catch { }
                if (lines == 0)
                {
                    var m = Regex.Match(err, @"ERROR:\s*(.+)");
                    string b = o.Cookies.Substring(0, 1).ToUpperInvariant() + o.Cookies.Substring(1);
                    cookieError = "Couldn't read " + b + "'s sign-in" + (m.Success && !m.Value.Contains("ytd-cookie-export") ? ": " + m.Groups[1].Value.Trim() : "") +
                                  ". Close " + b + " and try again, or use Firefox / a cookies.txt file.";
                    BeginInvoke((MethodInvoker)delegate { Log(cookieError); });
                    return new CookieContainer();
                }
                exportedCookies = tmp; exportedFor = o.Cookies; exportedAt = DateTime.Now;
                BeginInvoke((MethodInvoker)delegate { Log("Read " + lines + " sign-in cookies from " + o.Cookies + "."); });
                return Web.LoadCookies(tmp);
            }
        }

        static string DriveStatus(WebFile wf, Opts o)
        {
            switch (wf.Kind)
            {
                case "signin": return o.Cookies == "" ? "Needs sign-in - set Sign-in" : "No access with this Google account";
                case "quota": return "Drive limit reached - try later";
                case "net": return "No connection to Google Drive";
                default: return o.Cookies == "" ? "No access - try Sign-in" : "No access / file not found";
            }
        }

        // ---------------- Google Drive: check (size + name) on a worker thread
        void FetchDrive(Item it)
        {
            driveFetching = true;
            var o = GetOpts();
            var th = new Thread(() =>
            {
                var wf = new WebFile(); byte[] thumb = null;
                try
                {
                    var cc = new CookieContainer();
                    var resp = Web.OpenDrive(it.DriveId, cc, wf);                       // 1st try: signed out
                    if (resp == null && (wf.Kind == "signin" || wf.Kind == "noaccess") && o.Cookies != "")
                    {
                        cc = DriveCookies(o); wf = new WebFile();
                        resp = Web.OpenDrive(it.DriveId, cc, wf);                       // 2nd try: signed in
                        if (resp != null) it.UseCookies = true;
                    }
                    if (resp != null) { try { wf.Req.Abort(); } catch { } try { resp.Close(); } catch { } thumb = Web.DriveThumb(it.DriveId, cc); }
                }
                catch (Exception ex) { wf.Kind = "net"; wf.Error = ex.Message; }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        driveFetching = false;
                        if (items.Contains(it))
                        {
                            if (wf.Name != null)
                            {
                                it.FileName = wf.Name; it.Title = Path.GetFileNameWithoutExtension(wf.Name); it.ProbeSize = wf.Size;
                                it.Lvi.Text = it.Title; it.State = 2; Recompute(it);
                                if (thumb != null) SetThumb(it, thumb);
                                Log("Drive file \"" + wf.Name + "\": " + T.Bytes(wf.Size));
                            }
                            else
                            {
                                it.State = 3; it.Error = wf.Error;
                                SetItemStatus(it, DriveStatus(wf, o), "bad", (wf.Kind == "signin" || wf.Kind == "noaccess") && o.Cookies == "" ? "firefox" : wf.Kind == "net" || wf.Kind == "quota" ? "retry" : "");
                                it.Lvi.ToolTipText = wf.Error + (cookieError != null ? "\n" + cookieError : "");
                                Log("Drive " + it.DriveId + ": " + wf.Error + (cookieError != null ? " (" + cookieError + ")" : ""));
                            }
                        }
                        UpdateSummary(); FetchNext();
                    });
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        void SetThumb(Item it, byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data)) using (var img = Image.FromStream(ms))
                {
                    string key = "t" + it.GetHashCode();
                    thumbs.Images.Add(key, ThumbFrom(img)); it.Lvi.ImageKey = key; lv.Invalidate();
                }
            }
            catch { }
        }

        // ---------------- direct links: real file size via the server
        void ProbeDirect(Item it)
        {
            it.Probed = true;
            var th = new Thread(() =>
            {
                long size = Web.DirectSize(it.Url);
                if (size <= 0 && it.Formats != null && it.Formats.Count > 0)
                {
                    var last = it.Formats[it.Formats.Count - 1];          // yt-dlp lists formats worst -> best
                    if (last.Proto.StartsWith("http") && last.Url.StartsWith("http")) size = Web.DirectSize(last.Url);
                }
                try { BeginInvoke((MethodInvoker)delegate { if (size > 0) { it.ProbeSize = size; Recompute(it); UpdateSummary(); } }); } catch { }
            });
            th.IsBackground = true; th.Start();
        }

        // ---------------- Google Drive: download the original file, then (audio mode) convert with ffmpeg
        void StartDrive(Item it)
        {
            var o = GetOpts();
            if (!o.Video && !o.Doc && ffmpeg == null) { DriveFinished(it, false, "ffmpeg is needed to extract audio", "other"); return; }
            string outDir = txtOut.Text.Trim();
            driveCancel = false; driveRunning = true;
            SetItemStatus(it, "Starting...", "run");
            Log("Google Drive download: " + it.Url);
            var th = new Thread(() =>
            {
                string error = null, errKind = "other", saved = null;
                try
                {
                    var cc = it.UseCookies ? DriveCookies(o) : new CookieContainer();
                    var wf = new WebFile();
                    var resp = Web.OpenDrive(it.DriveId, cc, wf);
                    if (resp == null && !it.UseCookies && (wf.Kind == "signin" || wf.Kind == "noaccess") && o.Cookies != "")
                    { it.UseCookies = true; cc = DriveCookies(o); wf = new WebFile(); resp = Web.OpenDrive(it.DriveId, cc, wf); }
                    if (resp == null) { error = DriveStatus(wf, o) + " - " + wf.Error; errKind = wf.Kind == "signin" ? "drive-signin" : "other"; }
                    else
                    {
                        string path = Path.Combine(outDir, wf.Name);
                        if (File.Exists(path) && new FileInfo(path).Length == wf.Size && o.Video)
                        { try { wf.Req.Abort(); resp.Close(); } catch { } saved = path; }
                        else
                        {
                            if (File.Exists(path)) path = Unique(path);
                            long total = resp.ContentLength, done = 0; var sw = Stopwatch.StartNew(); long lastTick = 0;
                            string part = path + ".part";
                            try
                            {
                                using (resp) using (var s = resp.GetResponseStream()) using (var fs = new FileStream(part, FileMode.Create, FileAccess.Write))
                                {
                                    var buf = new byte[1 << 20]; int n;
                                    while ((n = s.Read(buf, 0, buf.Length)) > 0)
                                    {
                                        if (driveCancel) { wf.Req.Abort(); throw new OperationCanceledException(); }
                                        fs.Write(buf, 0, n); done += n;
                                        if (sw.ElapsedMilliseconds - lastTick > 250)
                                        {
                                            lastTick = sw.ElapsedMilliseconds;
                                            long d = done; double secs = sw.Elapsed.TotalSeconds;
                                            BeginInvoke((MethodInvoker)delegate { DriveProgress(it, d, total, secs); });
                                        }
                                    }
                                }
                                if (total > 0 && done != total) throw new IOException("Download stopped early (" + T.Bytes(done) + " of " + T.Bytes(total) + ")");
                                File.Move(part, path); saved = path;
                            }
                            catch { try { File.Delete(part); } catch { } throw; }
                        }
                    }
                }
                catch (OperationCanceledException) { error = "cancelled"; }
                catch (Exception ex) { error = ex.Message; }
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (saved != null) it.DonePath = saved;
                        if (error == null && !o.Video && !o.Doc) ConvertAudio(it, saved, o);
                        else DriveFinished(it, error == null, error, errKind);
                    });
                }
                catch { }
            });
            th.IsBackground = true; th.Start();
        }

        static string Unique(string path)
        {
            string dirn = Path.GetDirectoryName(path), name = Path.GetFileNameWithoutExtension(path), ext = Path.GetExtension(path);
            for (int i = 2; ; i++) { string p = Path.Combine(dirn, name + " (" + i + ")" + ext); if (!File.Exists(p)) return p; }
        }

        void DriveProgress(Item it, long done, long total, double secs)
        {
            double speed = secs > 0 ? done / secs : 0;
            string pct = total > 0 ? ((int)(done * 100 / total)) + "%" : T.Bytes(done);
            SetItemStatus(it, "Downloading " + pct, "run");
            if (total > 0) bar.Value = (dlIndex + (double)done / total) / queue.Count;
            string left = (total > 0 && speed > 0) ? "  \u00B7  " + T.Dur((total - done) / speed) + " left" : "";
            SetStatus("Downloading " + (dlIndex + 1) + " of " + queue.Count + ": " + it.Title + "  \u00B7  " + T.Bytes(speed) + "/s" + left, T.Text);
        }

        void ConvertAudio(Item it, string src, Opts o)
        {
            string ext; var a = new List<string> { "-hide_banner", "-nostdin", "-y", "-i", src, "-vn", "-map_metadata", "0" };
            string br = o.ABr == "0" ? null : o.ABr.TrimEnd('K') + "k";
            switch (o.AFmt)
            {
                case "mp3": ext = ".mp3"; a.AddRange(br == null ? new[] { "-c:a", "libmp3lame", "-q:a", "0" } : new[] { "-c:a", "libmp3lame", "-b:a", br }); break;
                case "m4a": ext = ".m4a"; a.AddRange(new[] { "-c:a", "aac", "-b:a", br ?? "256k" }); break;
                case "opus": ext = ".opus"; a.AddRange(new[] { "-c:a", "libopus", "-b:a", br ?? "160k" }); break;
                case "flac": ext = ".flac"; a.AddRange(new[] { "-c:a", "flac" }); break;
                case "wav": ext = ".wav"; a.AddRange(new[] { "-c:a", "pcm_s16le" }); break;
                default: ext = ".mka"; a.AddRange(new[] { "-c:a", "copy" }); break;
            }
            string dst = Path.ChangeExtension(src, ext); if (File.Exists(dst)) dst = Unique(dst);
            a.AddRange(new[] { "-metadata", "title=" + it.Title, dst });
            SetItemStatus(it, "Converting to " + (o.AFmt == "best" ? "audio" : o.AFmt.ToUpperInvariant()) + "...", "run");
            Log("> ffmpeg " + string.Join(" ", a.ConvertAll(Runner.Quote).ToArray()));
            double dur = 0;
            dlRunner = new Runner(this);
            dlRunner.Line += line =>
            {
                var m = Regex.Match(line, @"Duration:\s*(\d+):(\d+):(\d+\.?\d*)");
                if (m.Success && dur == 0) dur = int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60 + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                var t = Regex.Match(line, @"time=(\d+):(\d+):(\d+\.?\d*)");
                if (t.Success && dur > 0)
                {
                    double cur = int.Parse(t.Groups[1].Value) * 3600 + int.Parse(t.Groups[2].Value) * 60 + double.Parse(t.Groups[3].Value, CultureInfo.InvariantCulture);
                    SetItemStatus(it, "Converting " + Math.Min(100, (int)(cur * 100 / dur)) + "%", "run");
                }
                else if (line.Contains("Error") || line.Contains("Invalid")) Log(line);
            };
            dlRunner.Exited += code =>
            {
                bool killed = dlRunner != null && dlRunner.Killed; dlRunner = null;
                if (code == 0 && !killed) { try { File.Delete(src); } catch { } it.DonePath = dst; DriveFinished(it, true, null, ""); }
                else { try { File.Delete(dst); } catch { } DriveFinished(it, false, killed ? "cancelled" : "ffmpeg could not extract the audio (exit " + code + ")", "other"); }
            };
            try { dlRunner.Start(ffmpeg, a, pathEnv); }
            catch (Exception ex) { dlRunner = null; DriveFinished(it, false, "Couldn't start ffmpeg: " + ex.Message, "other"); }
        }

        void DriveFinished(Item it, bool ok, string error, string errKind)
        {
            driveRunning = false;
            if (error == "cancelled" || cancelAll) { SetItemStatus(it, "Cancelled", "bad", "retry"); FinishAll(); return; }
            if (ok) { it.Done = true; SetItemStatus(it, "\u2713 Done", "ok", it.DonePath != null ? "show" : ""); }
            else
            {
                it.Error = errKind; Log("Drive download failed: " + error);
                string shortErr = error ?? "";
                int dash = shortErr.IndexOf(" - "); if (dash > 0) shortErr = shortErr.Substring(0, dash);
                SetItemStatus(it, "Failed: " + (shortErr.Length > 26 ? shortErr.Substring(0, 24) + "..." : shortErr), "bad", errKind == "drive-signin" && GetOpts().Cookies == "" ? "firefox" : "retry");
                it.Lvi.ToolTipText = error;
            }
            dlIndex++; NextDownload();
        }

        // ---------------- documents -> PDF
        static string Hash(string s)
        {
            using (var sha = System.Security.Cryptography.SHA1.Create())
            { var b = sha.ComputeHash(Encoding.UTF8.GetBytes(s)); var sb = new StringBuilder(); for (int i = 0; i < 8; i++) sb.Append(b[i].ToString("x2")); return sb.ToString(); }
        }
        static string CacheDir { get { string d = Path.Combine(DataDir, "cache"); try { Directory.CreateDirectory(d); } catch { } return d; } }
        static string CachePath(string url) { return Path.Combine(CacheDir, "doc-" + Hash(url) + ".pdf"); }
        void CleanCache()
        {
            try { foreach (var f in Directory.GetFiles(CacheDir)) if ((DateTime.Now - File.GetLastWriteTime(f)).TotalDays > 2) try { File.Delete(f); } catch { } } catch { }
        }

        int CountPages(string file)
        {
            if (qpdf != null)
            {
                try
                {
                    var psi = new ProcessStartInfo(qpdf, "--show-npages " + Runner.Quote(file)); psi.UseShellExecute = false; psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
                    using (var p = Process.Start(psi)) { string o = p.StandardOutput.ReadToEnd(); p.WaitForExit(30000); int n; if (int.TryParse(o.Trim(), out n) && n > 0) return n; }
                }
                catch { }
            }
            return Pdf.CountPages(file);
        }

        void FetchDoc(Item it)
        {
            driveFetching = true;
            var o = GetOpts(); string kind = it.DocKind, url = it.Url; bool signed = it.UseCookies;
            var th = new Thread(() =>
            {
                var wf = new WebFile(); string title = null, file = null; int pages = -1; byte[] thumb = null; bool usedCookies = signed;
                try
                {
                    if (kind == "web")
                    {
                        title = Web.PageTitle(url, wf);
                        if (title != null && FindBrowser() == null) { wf.Kind = "nobrowser"; wf.Error = "Saving web pages as PDF needs Microsoft Edge or Google Chrome."; title = null; }
                    }
                    else
                    {
                        string id = null, src = url;
                        if (kind != "pdf") { Web.GDoc(url, out id); src = Web.ExportUrl(kind, id); }
                        file = CachePath(url);
                        var cc = signed ? DriveCookies(o) : new CookieContainer();
                        bool ok = Web.FetchPdf(src, cc, file, wf, null, null);
                        if (!ok && (wf.Kind == "signin" || wf.Kind == "notfound") && !signed && o.Cookies != "" && kind != "pdf")
                        { cc = DriveCookies(o); wf = new WebFile(); ok = Web.FetchPdf(src, cc, file, wf, null, null); if (ok) usedCookies = true; }
                        if (ok) { title = Path.GetFileNameWithoutExtension(wf.Name); pages = CountPages(file); if (id != null) thumb = Web.DriveThumb(id, cc); }
                        else file = null;
                    }
                }
                catch (Exception ex) { wf.Kind = "net"; wf.Error = ex.Message; title = null; }
                Ui(() =>
                {
                    driveFetching = false;
                    if (items.Contains(it) && it.DocKind == kind)
                    {
                        if (title != null)
                        {
                            it.Title = title; it.Lvi.Text = title; it.DocFile = file; it.Pages = pages; it.UseCookies = usedCookies;
                            it.ProbeSize = file != null ? new FileInfo(file).Length : -1;
                            it.State = 2; Recompute(it); if (thumb != null) SetThumb(it, thumb);
                            it.Lvi.ToolTipText = title + "\n" + url;
                            Log("Document \"" + title + "\": " + (pages > 0 ? pages + " pages, " : "") + (it.ProbeSize > 0 ? T.Bytes(it.ProbeSize) : "web page"));
                        }
                        else
                        {
                            it.State = 3; it.Error = wf.Error;
                            string st, act = "retry";
                            switch (wf.Kind)
                            {
                                case "signin": st = o.Cookies == "" ? "Private - needs sign-in" : "No access with this sign-in"; act = o.Cookies == "" ? "firefox" : ""; break;
                                case "notfound": st = o.Cookies == "" && kind != "pdf" ? "Not found or private" : "Not found - check the link"; act = o.Cookies == "" && kind != "pdf" ? "firefox" : ""; break;
                                case "notpdf": st = "Not a PDF file"; act = ""; break;
                                case "nobrowser": st = "Needs Edge or Chrome"; act = ""; break;
                                case "notpage": st = "Not a web page"; act = "video"; break;
                                case "http": st = Friendly(wf.Error ?? "", 26); break;
                                default: st = "Network problem"; break;
                            }
                            SetItemStatus(it, st, "bad", act);
                            it.Lvi.ToolTipText = (wf.Error ?? st) + (cookieError != null ? "\n" + cookieError : "") +
                                (wf.Kind == "signin" || wf.Kind == "notfound" ? "\n\nIf the document is private: set Sign-in to the browser where you are signed in to Google,\nor share it as \"Anyone with the link\"." : "");
                            Log("Document " + url + ": " + wf.Error);
                        }
                    }
                    UpdateSummary(); FetchNext();
                });
            });
            th.IsBackground = true; th.Start();
        }

        volatile Process printProc;
        void StartDoc(Item it)
        {
            var o = GetOpts(); string outDir = txtOut.Text.Trim();
            driveCancel = false; driveRunning = true;
            SetItemStatus(it, "Starting...", "run"); it.Lvi.EnsureVisible();
            SetStatus("Saving " + (dlIndex + 1) + " of " + queue.Count + ": " + it.Title, T.Text);
            var th = new Thread(() =>
            {
                string error = null, saved = null, act = "retry";
                try
                {
                    string src = it.DocFile;
                    if (it.DocKind == "web")
                    {
                        Ui(() => SetItemStatus(it, "Printing the page to PDF...", "run"));
                        src = Path.Combine(CacheDir, "web-" + Hash(it.Url) + ".pdf");
                        error = PrintPage(it.Url, src);
                        if (error == null) { int n = CountPages(src); Ui(() => { it.Pages = n; }); }
                        else act = FindBrowser() == null ? "" : "retry";
                    }
                    else if (src == null || !File.Exists(src))
                    {
                        string id; string url = it.DocKind == "pdf" ? it.Url : (Web.GDoc(it.Url, out id) != null ? Web.ExportUrl(it.DocKind, id) : it.Url);
                        src = CachePath(it.Url); var wf = new WebFile();
                        var cc = it.UseCookies ? DriveCookies(o) : new CookieContainer();
                        var sw = Stopwatch.StartNew(); long last = 0;
                        if (!Web.FetchPdf(url, cc, src, wf, () => driveCancel, (d, t) =>
                        {
                            if (sw.ElapsedMilliseconds - last < 250) return; last = sw.ElapsedMilliseconds; double secs = sw.Elapsed.TotalSeconds;
                            Ui(() => DriveProgress(it, d, t, secs));
                        }))
                            error = wf.Kind == "cancel" ? "cancelled" : wf.Error;
                    }
                    if (error == null)
                    {
                        if (driveCancel) throw new OperationCanceledException();
                        string name = Web.Safe(it.Title ?? "document"); if (name.Length > 150) name = name.Substring(0, 150).Trim();
                        string suffix = o.Pages != "" ? " (pages " + Regex.Replace(o.Pages, @"\s+", "") + ")" : "";
                        string dst = Path.Combine(outDir, name + suffix + ".pdf"); if (File.Exists(dst)) dst = Unique(dst);
                        if (o.Pages != "")
                        {
                            string err; int total = it.Pages > 0 ? it.Pages : CountPages(src);
                            Pdf.CountRange(o.Pages, total, out err);
                            if (err != null) { error = err; act = ""; }
                            else { Ui(() => SetItemStatus(it, "Picking pages...", "run")); error = RunQpdf(src, Pdf.QpdfRange(o.Pages), dst); }
                        }
                        else File.Copy(src, dst);
                        if (error == null) saved = dst;
                    }
                }
                catch (OperationCanceledException) { error = "cancelled"; }
                catch (Exception ex) { error = ex.Message; }
                Ui(() => DocFinished(it, saved, error, act));
            });
            th.IsBackground = true; th.Start();
        }

        void DocFinished(Item it, string saved, string error, string act)
        {
            driveRunning = false;
            if (error == "cancelled" || cancelAll) { SetItemStatus(it, "Cancelled", "bad", "retry"); FinishAll(); return; }
            if (saved != null) { it.Done = true; it.DonePath = saved; SetItemStatus(it, "\u2713 Done", "ok", "show"); Log("Saved " + saved); if (it.Pages > 0 && GetOpts().Pages == "") it.Lvi.SubItems[1].Text = it.Pages + (it.Pages == 1 ? " page" : " pages"); }
            else
            {
                Log("Document failed (" + it.Url + "): " + error);
                string e = error ?? "unknown error"; if (e.Length > 26) e = e.Substring(0, 24) + "...";
                SetItemStatus(it, "Failed: " + e, "bad", act); it.Lvi.ToolTipText = error; it.Error = "other";
            }
            bar.Value = (dlIndex + 1.0) / queue.Count;
            dlIndex++; NextDownload();
        }

        string FindBrowser()
        {
            string env = Environment.GetEnvironmentVariable("YTD_BROWSER");
            if (!string.IsNullOrEmpty(env)) return File.Exists(env) ? env : null;
            var c = new List<string>();
            foreach (var v in new[] { "ProgramFiles(x86)", "ProgramFiles", "LOCALAPPDATA" })
            {
                string b = Environment.GetEnvironmentVariable(v); if (string.IsNullOrEmpty(b)) continue;
                c.Add(Path.Combine(b, @"Microsoft\Edge\Application\msedge.exe")); c.Add(Path.Combine(b, @"Google\Chrome\Application\chrome.exe"));
            }
            foreach (var f in c) if (File.Exists(f)) return f;
            foreach (var n in new[] { "msedge", "chrome", "chromium", "google-chrome" }) { var f = Find(n); if (f != null) return f; }
            return null;
        }

        string PrintPage(string url, string dst)
        {
            string br = FindBrowser(); if (br == null) return "Saving web pages as PDF needs Microsoft Edge or Google Chrome.";
            string prof = Path.Combine(Path.GetTempPath(), "dawn-print-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try { File.Delete(dst); } catch { }
            string args = "--headless --disable-gpu --no-first-run --no-default-browser-check --disable-extensions --hide-scrollbars " +
                          "--run-all-compositor-stages-before-draw --virtual-time-budget=10000 --no-pdf-header-footer " +
                          Runner.Quote("--user-data-dir=" + prof) + " " + Runner.Quote("--print-to-pdf=" + dst) + " " + Runner.Quote(url);
            Ui(() => Log("> " + Path.GetFileName(br) + " " + args));
            try
            {
                var psi = new ProcessStartInfo(br, args); psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardError = true; psi.RedirectStandardOutput = true;
                using (var p = Process.Start(psi))
                {
                    printProc = p;
                    p.StandardError.ReadToEndAsync(); p.StandardOutput.ReadToEndAsync();
                    if (!p.WaitForExit(120000)) { try { p.Kill(); } catch { } return "The browser took too long to print the page."; }
                }
            }
            catch (Exception ex) { return "Couldn't start the browser: " + ex.Message; }
            finally { printProc = null; try { Directory.Delete(prof, true); } catch { } }
            if (driveCancel) return "cancelled";
            try
            {
                if (File.Exists(dst) && new FileInfo(dst).Length > 100)
                { var h = new byte[5]; using (var f = File.OpenRead(dst)) f.Read(h, 0, 5); if (Encoding.ASCII.GetString(h) == "%PDF-") return null; }
            }
            catch { }
            return "The browser could not save this page as PDF.";
        }

        string RunQpdf(string src, string range, string dst)
        {
            if (qpdf == null) return "The PDF tool (qpdf) is not installed.";
            try
            {
                var psi = new ProcessStartInfo(qpdf, Runner.Quote(src) + " --pages . " + range + " -- " + Runner.Quote(dst));
                psi.UseShellExecute = false; psi.CreateNoWindow = true; psi.RedirectStandardError = true; psi.RedirectStandardOutput = true;
                using (var p = Process.Start(psi))
                {
                    var err = p.StandardError.ReadToEndAsync(); p.StandardOutput.ReadToEnd(); p.WaitForExit(120000);
                    if ((p.ExitCode == 0 || p.ExitCode == 3) && File.Exists(dst)) return null;
                    string e = err.Result.Trim(); return "qpdf: " + (e.Length > 0 ? e.Split('\n')[0] : "exit code " + p.ExitCode);
                }
            }
            catch (Exception ex) { return "Couldn't run qpdf: " + ex.Message; }
        }

        ToolPkg QpdfPkg()
        {
            return new ToolPkg { Name = "PDF tool (qpdf)", File = "qpdf.zip", Size = 25, PickDir = "/bin/", SubDir = "qpdf",
                Urls = Mirror("https://github.com/qpdf/qpdf/releases/download/v12.3.2/qpdf-12.3.2-msvc64.zip") };
        }
        bool resumeAfterQpdf;
        void AskQpdf()
        {
            var r = MessageBox.Show(this, "Picking pages needs a small free PDF tool (qpdf, about 25 MB download, Apache 2.0 licence).\n\n" +
                "It goes into your own user folder - no admin rights needed. Install it now and continue?",
                "Dawnloader - PDF pages", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes) { resumeAfterQpdf = true; InstallQpdf(); }
        }
        void InstallQpdf() { if (qpdf == null) InstallTools(new List<ToolPkg> { QpdfPkg() }); }

        // ---------------- clip (download only a part)
        void EditClip(Item it)
        {
            using (var d = new Form())
            {
                d.Text = "Download only a part"; d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent; d.BackColor = T.Solid(T.Bg); d.Font = T.Body; d.ClientSize = new Size(T.P(430), T.P(236));
                if (Icon != null) d.Icon = Icon;
                int M = T.P(18), y = T.P(16);
                var t = L(it.Title, T.BodyBold, T.Text); t.AutoSize = false; t.AutoEllipsis = true; t.SetBounds(M, y, d.ClientSize.Width - 2 * M, T.P(22)); d.Controls.Add(t); y += T.P(28);
                var info = L((it.Duration > 0 ? "Length " + T.Dur(it.Duration) + "." : "Length unknown.") + "  Times like 1:20, 80 or 1m20s.", T.Small, T.Muted); info.Location = new Point(M, y); d.Controls.Add(info); y += T.P(30);
                var lS = L("Start", T.Body, T.Muted); var tS = Box(); tS.Width = T.P(110); tS.Text = it.HasClip ? Clock(it.ClipStart) : "0:00";
                var lE = L("End", T.Body, T.Muted); var tE = Box(); tE.Width = T.P(110); tE.Text = it.HasClip ? Clock(it.ClipEnd) : it.Duration > 0 ? T.Dur(it.Duration) : "";
                Row(d, M, y, T.P(32), lS, tS, 16, lE, tE); y += T.P(40);
                var res = L("", T.Small, T.Muted); res.Location = new Point(M, y); res.AutoSize = false; res.SetBounds(M, y, d.ClientSize.Width - 2 * M, T.P(22)); d.Controls.Add(res);
                var ok = new FlatButton("Use this part", "cut", 0); ok.Size = new Size(ok.PreferredWidth(T.P(38)), T.P(38));
                var cancel = new FlatButton("Cancel", "", 1); cancel.Size = new Size(cancel.PreferredWidth(T.P(38)), T.P(38));
                ok.Location = new Point(d.ClientSize.Width - M - ok.Width, d.ClientSize.Height - M - ok.Height); cancel.Location = new Point(ok.Left - T.P(8) - cancel.Width, ok.Top);
                d.Controls.Add(ok); d.Controls.Add(cancel);
                double s0 = -1, e0 = -1;
                Func<bool> check = () =>
                {
                    s0 = T.ParseTime(tS.Text); e0 = tE.Text.Trim() == "" ? (it.Duration > 0 ? it.Duration : -2) : T.ParseTime(tE.Text);
                    string err = null;
                    if (s0 < 0) err = "Start time isn't valid."; else if (e0 == -2) err = "Type an end time."; else if (e0 < 0) err = "End time isn't valid.";
                    else if (e0 <= s0) err = "End must be after the start.";
                    else if (it.Duration > 0 && s0 >= it.Duration) err = "Start is after the end of the video.";
                    if (err == null && it.Duration > 0 && e0 > it.Duration) e0 = it.Duration;
                    if (err != null) { res.Text = err; res.ForeColor = T.Bad; return false; }
                    double frac = it.Duration > 0 ? (e0 - s0) / it.Duration : 0;
                    res.Text = "Clip length " + T.Dur(e0 - s0) + (frac > 0 ? "  \u00B7  about " + Math.Max(1, (int)Math.Round(frac * 100)) + "% of the video" : "");
                    res.ForeColor = T.Muted; return true;
                };
                tS.TextChanged += (a, b) => check(); tE.TextChanged += (a, b) => check(); check();
                ok.Click += (a, b) => { if (check()) { d.DialogResult = DialogResult.OK; d.Close(); } };
                cancel.Click += (a, b) => d.Close();
                d.AcceptButton = null;
                if (d.ShowDialog(this) == DialogResult.OK)
                {
                    it.ClipStart = s0; it.ClipEnd = e0;
                    if (it.ClipStart <= 0 && it.Duration > 0 && it.ClipEnd >= it.Duration) { it.ClipStart = it.ClipEnd = -1; }
                    Recompute(it); UpdateSummary(); lv.Invalidate();
                    SetStatus(it.HasClip ? "Only " + Clock(it.ClipStart) + " to " + Clock(it.ClipEnd) + " of this video will be downloaded." : "The whole video will be downloaded.", T.Muted);
                }
            }
        }

        // ---------------- clipboard catcher
        [DllImport("user32.dll", SetLastError = true)] static extern bool AddClipboardFormatListener(IntPtr hwnd);
        System.Windows.Forms.Timer clipTimer = new System.Windows.Forms.Timer(); bool winClip; string lastClip;
        void StartClipboardWatch()
        {
            if (ckCatch == null || !ckCatch.Checked) { clipTimer.Stop(); return; }
            if (lastClip == null) lastClip = SafeClipboardText();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && !winClip) { try { winClip = AddClipboardFormatListener(Handle); } catch { winClip = false; } }
            if (!winClip) clipTimer.Start();
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x031D && ckCatch != null && !ckCatch.IsDisposed && ckCatch.Checked) BeginInvoke((MethodInvoker)CatchClipboard);
            base.WndProc(ref m);
        }
        static string SafeClipboardText()
        {
            for (int i = 0; i < 2; i++) { try { return Clipboard.ContainsText() ? Clipboard.GetText() : ""; } catch { Thread.Sleep(60); } }
            return null;
        }
        static bool Catchable(string u)
        {
            if (KindOf(u) != "other") return true;
            return Regex.IsMatch(u, @"^https?://([\w-]+\.)*(tiktok\.com|instagram\.com|facebook\.com|fb\.watch|x\.com|twitter\.com|vimeo\.com|dailymotion\.com|reddit\.com|redd\.it|twitch\.tv|soundcloud\.com|bilibili\.com|threads\.net|bsky\.app|pinterest\.com|streamable\.com|rumble\.com|kick\.com|9gag\.com|ted\.com|bandcamp\.com|mixcloud\.com)/", RegexOptions.IgnoreCase)
                || Regex.IsMatch(u, @"\.(mp4|m4v|mkv|webm|mov|mp3|m4a|m3u8)([?#].*)?$", RegexOptions.IgnoreCase);
        }
        void CatchClipboard()
        {
            if (ckCatch == null || !ckCatch.Checked || loading) return;
            string text = SafeClipboardText(); if (text == null || text == lastClip) return;
            lastClip = text;
            var have = CurrentUrls(); var add = new List<string>();
            foreach (Match m in Regex.Matches(text, @"https?://[^\s""'<>]+"))
            {
                string u = CleanUrl(m.Value.TrimEnd('.', ',', ')', ']'));
                if (Catchable(u) && !have.Contains(u) && !add.Contains(u)) add.Add(u);
            }
            if (add.Count == 0) return;
            if (busy) { SetStatus("Copied link noticed - it can be added after the current downloads finish.", T.Warn); lastClip = null; return; }
            string cur = txtLinks.Text.TrimEnd();
            txtLinks.Text = (cur.Length > 0 ? cur + Environment.NewLine : "") + string.Join(Environment.NewLine, add.ToArray()) + Environment.NewLine;
            txtLinks.SelectionStart = txtLinks.TextLength;
            debounce.Stop(); SyncItems();
            SetStatus("Caught " + (add.Count == 1 ? "a link" : add.Count + " links") + " from the clipboard.", T.Ok);
        }

        // ---------------- skins
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] static extern int SetWindowTheme(IntPtr hwnd, string app, string idList);
        void ApplyWindowTheme()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            int dark = T.Cur.Dark ? 1 : 0;
            try { if (DwmSetWindowAttribute(Handle, 20, ref dark, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref dark, 4); } catch { }
            foreach (Control c in new Control[] { lv, txtLinks, txtOut })
                try { SetWindowTheme(c.Handle, T.Cur.Dark ? "DarkMode_Explorer" : "Explorer", null); } catch { }
        }

        void ChangeSkin(string name)
        {
            if (busy || name == skinName) return;
            skinName = name; SaveSettings();
            T.Cur = T.MakeSkin(name);
            var state = WindowState; if (state == FormWindowState.Minimized) state = FormWindowState.Normal;
            WindowState = FormWindowState.Normal;
            var size = ClientSize; var loc = Location; string links = txtLinks.Text;
            loading = true;
            SuspendLayout();
            lv.Items.Clear();
            var old = new List<Control>(); foreach (Control c in Controls) old.Add(c);
            Controls.Clear(); tip.RemoveAll();
            foreach (var c in old) c.Dispose();
            Build(); LoadSettings();
            txtLinks.Text = links;
            ClientSize = size; Location = loc;
            ResumeLayout(true);
            WindowState = state;
            loading = false; lastDoc = tDoc.Checked; lastCookies = GetOpts().Cookies;
            ApplyMode(); debounce.Stop(); SyncItems();
            foreach (var it in items) SetItemStatus(it, it.Status, it.StatusKind, it.Action);
            UpdateToolsLabel(); UpdateSummary(); ApplyWindowTheme(); StartClipboardWatch();
            Invalidate(true);
            SetStatus("Skin: " + name + ".", T.Muted);
        }

        // ---------------- about
        void ShowAbout()
        {
            using (var d = new Form())
            {
                d.Text = "About Dawnloader"; d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent; d.BackColor = T.Solid(T.Bg); d.Font = T.Body; d.ClientSize = new Size(T.P(500), T.P(430));
                if (Icon != null) d.Icon = Icon;
                int M = T.P(22), W = d.ClientSize.Width - 2 * M;
                var logo = new Panel(); logo.SetBounds(M, T.P(20), T.P(56), T.P(56)); logo.BackColor = d.BackColor;
                logo.Paint += (a, b) =>
                {
                    b.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = T.Round(new RectangleF(0, 0, logo.Width - 1, logo.Height - 1), T.P(14))) using (var br = new LinearGradientBrush(logo.ClientRectangle, T.Cur.HeadA.A < 255 ? T.Accent : T.Cur.HeadA, T.Cur.HeadB.A < 255 ? T.Cur.AccentDark : T.Cur.HeadB, 90f)) b.Graphics.FillPath(br, p);
                    Icons.DrawLogo(b.Graphics, new RectangleF(T.P(8), T.P(8), logo.Width - T.P(16), logo.Height - T.P(16)), Color.White, T.AccentDark);
                };
                d.Controls.Add(logo);
                var name = L("Dawnloader", T.Header, T.Text); name.Location = new Point(logo.Right + T.P(14), T.P(22)); d.Controls.Add(name);
                var ver = L("Version " + AppVersion + "  \u00B7  free and open source (MIT)", T.Small, T.Muted); ver.Location = new Point(logo.Right + T.P(16), name.Bottom + T.P(4)); d.Controls.Add(ver);
                int y = logo.Bottom + T.P(18);
                Func<string, Font, Color, Label> para = (text, f, c) =>
                {
                    var l = L(text, f, c); l.AutoSize = false; l.Location = new Point(M, y);
                    int h = TextRenderer.MeasureText(text, f, new Size(W, 0), TextFormatFlags.WordBreak).Height;
                    l.Size = new Size(W, h + T.P(2)); d.Controls.Add(l); y = l.Bottom + T.P(10); return l;
                };
                para("Downloads video, audio and documents from YouTube, TikTok, Instagram, Facebook, X, Google Drive and Docs, PDF links and 1000+ other sites. Everything runs on this PC - nothing is uploaded anywhere.", T.Body, T.Text);
                para("Use it responsibly", T.BodyBold, T.Text);
                para("Only download content you own, that is licensed for reuse, or that you have permission to save. You are responsible for how you use what you download. Streaming services that use copy protection (DRM), such as Netflix or Spotify, can't be downloaded.", T.Body, T.Text);
                para("Built on yt-dlp (Unlicense), FFmpeg (LGPL/GPL), Deno (MIT) and qpdf (Apache 2.0). Not affiliated with any of the sites it supports.", T.Small, T.Muted);
                var ok = new FlatButton("OK", "", 0); ok.Size = new Size(T.P(110), T.P(38));
                ok.Location = new Point(d.ClientSize.Width - M - ok.Width, Math.Max(y + T.P(4), d.ClientSize.Height - M - ok.Height));
                d.ClientSize = new Size(d.ClientSize.Width, ok.Bottom + M);
                ok.Click += (a, b) => d.Close(); d.Controls.Add(ok);
                d.ShowDialog(this);
            }
        }
        const string AppVersion = "2026.11";

        // ---------------- misc actions
        void PasteLinks()
        {
            try
            {
                string t = Clipboard.GetText();
                if (string.IsNullOrEmpty(t)) { SetStatus("Clipboard is empty - copy a video link first.", T.Warn); return; }
                string cur = txtLinks.Text.TrimEnd();
                txtLinks.Text = (cur.Length > 0 ? cur + Environment.NewLine : "") + t.Trim() + Environment.NewLine;
                txtLinks.SelectionStart = txtLinks.TextLength;
                debounce.Stop(); SyncItems();
            }
            catch (Exception ex) { SetStatus("Couldn't read the clipboard: " + ex.Message, T.Warn); }
        }

        void Browse()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.SelectedPath = txtOut.Text; d.Description = "Where should downloads be saved?";
                if (d.ShowDialog(this) == DialogResult.OK) txtOut.Text = d.SelectedPath;
            }
        }
        void OpenFolder()
        {
            try { Directory.CreateDirectory(txtOut.Text.Trim()); Process.Start(txtOut.Text.Trim()); }
            catch (Exception ex) { SetStatus("Can't open folder: " + ex.Message, T.Warn); }
        }

        void SetStatus(string s, Color c) { lblStatus.Text = s; lblStatus.ForeColor = c; tip.SetToolTip(lblStatus, s); }

        void Log(string s)
        {
            log.AppendLine(s);
            if (logBox != null && !logBox.IsDisposed) logBox.AppendText(s + Environment.NewLine);
        }
        void ShowLog()
        {
            if (logForm == null || logForm.IsDisposed)
            {
                logForm = new Form(); logForm.Text = "Dawnloader - details"; logForm.Size = new Size(T.P(900), T.P(480)); logForm.StartPosition = FormStartPosition.CenterParent;
                if (Icon != null) logForm.Icon = Icon;
                logBox = new TextBox(); logBox.Multiline = true; logBox.ReadOnly = true; logBox.ScrollBars = ScrollBars.Both; logBox.WordWrap = false;
                logBox.Dock = DockStyle.Fill; logBox.Font = new Font("Consolas", 9.5f * T.S, GraphicsUnit.Point); logBox.BackColor = Color.White;
                logBox.Font = new Font(Environment.OSVersion.Platform == PlatformID.Win32NT ? "Consolas" : "DejaVu Sans Mono", 12.5f * T.S, GraphicsUnit.Pixel);
                logForm.Controls.Add(logBox);
                logBox.Text = log.ToString(); logBox.SelectionStart = logBox.TextLength; logBox.ScrollToCaret();
            }
            logForm.Show(this); logForm.Activate();
        }

        void CheckVersion()
        {
            if (ytdlp == null) { UpdateToolsLabel(); return; }
            Log("yt-dlp : " + ytdlp); Log("ffmpeg : " + (ffmpeg ?? "NOT FOUND")); Log("deno   : " + (deno ?? "not found"));
            var r = new Runner(this); var outp = new StringBuilder();
            r.Line += s => outp.AppendLine(s);
            r.Exited += code =>
            {
                string v = outp.ToString().Trim();
                Match m = Regex.Match(v, @"^\s*(\d{4})\.(\d{2})\.(\d{2})\S*", RegexOptions.Multiline);
                if (m.Success)
                {
                    ytdlpVersion = m.Value.Trim();
                    var dt = new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
                    int age = (int)(DateTime.Now - dt).TotalDays;
                    Log("yt-dlp version " + ytdlpVersion + " (" + age + " days old)");
                    if (age > 30) { ytdlpVersion += " (old)"; SetStatus("yt-dlp is " + age + " days old - click Update yt-dlp (top right) to avoid download errors.", T.Warn); }
                }
                UpdateToolsLabel();
            };
            try { r.Start(ytdlp, new List<string> { "--version" }, pathEnv); } catch { UpdateToolsLabel(); }
        }

        // ---------------- tool installer (no admin, no winget needed)
        class ToolPkg { public string Name, File, PickDir, SubDir; public long Size; public string[] Urls; public string[] Pick; }
        bool installing; volatile bool installCancel; volatile HttpWebRequest installReq;

        static string ToolsDir
        {
            get
            {
                string la = Environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (string.IsNullOrEmpty(la)) la = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string nu = Path.Combine(Path.Combine(la, "Dawnloader"), "tools");
                try
                {
                    string old = Path.Combine(Path.Combine(la, "YTDownloader"), "tools");
                    if (!Directory.Exists(nu) && Directory.Exists(old))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(nu));
                        Directory.Move(old, nu);
                    }
                }
                catch { }
                return nu;
            }
        }

        static string[] Mirror(params string[] urls)
        {
            string m = Environment.GetEnvironmentVariable("YTD_TOOL_MIRROR");   // for testing only
            if (string.IsNullOrEmpty(m)) return urls;
            var r = new string[urls.Length];
            for (int i = 0; i < urls.Length; i++) r[i] = m.TrimEnd('/') + "/" + urls[i].Substring(urls[i].LastIndexOf('/') + 1);
            return r;
        }

        List<ToolPkg> MissingTools()
        {
            var l = new List<ToolPkg>();
            if (ytdlp == null) l.Add(new ToolPkg { Name = "yt-dlp", File = "yt-dlp.exe", Size = 24,
                Urls = Mirror("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe") });
            if (ffmpeg == null) l.Add(new ToolPkg { Name = "ffmpeg", File = "ffmpeg-essentials.zip", Size = 115, Pick = new[] { "ffmpeg.exe", "ffprobe.exe" },
                Urls = Mirror("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
                              "https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip") });
            if (deno == null) l.Add(new ToolPkg { Name = "deno", File = "deno.zip", Size = 43, Pick = new[] { "deno.exe" },
                Urls = Mirror("https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip") });
            return l;
        }

        void AskInstall(string why)
        {
            if (busy || installing) return;
            var todo = MissingTools(); if (todo.Count == 0) return;
            long mb = 0; var names = new List<string>();
            foreach (var t in todo) { mb += t.Size; names.Add(t.Name); }
            var r = MessageBox.Show(this, why + "\n\nInstall them now?\n\n   " + string.Join(", ", names.ToArray()) + "   (about " + mb + " MB download)\n\n" +
                "They are free, come from their official sites, and go into your own user folder - no admin rights needed. Takes a minute or two.",
                "Dawnloader - first-time setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes) InstallTools();
        }

        bool installingQpdfOnly;
        void InstallTools(List<ToolPkg> only = null)
        {
            if (busy || installing) return;
            var todo = only ?? MissingTools(); if (todo.Count == 0) { UpdateToolsLabel(); return; }
            installingQpdfOnly = only != null;
            installing = true; installCancel = false; SetBusy(true); bar.Value = 0;
            string tdir = ToolsDir;
            Log("Installing tools into " + tdir);
            var th = new Thread(() =>
            {
                var failed = new List<string>();
                try { Directory.CreateDirectory(tdir); }
                catch (Exception ex) { failed.Add("Can't create " + tdir + ": " + ex.Message); }
                for (int i = 0; i < todo.Count && Directory.Exists(tdir); i++)
                {
                    if (installCancel) break;
                    var t = todo[i]; string err = null;
                    foreach (var url in t.Urls)
                    {
                        if (installCancel) break;
                        Ui(() => Log("> download " + url));
                        err = InstallOne(t, url, tdir, i, todo.Count);
                        if (err == null) { Ui(() => Log("  " + t.Name + " installed")); break; }
                        string e2 = err; Ui(() => Log("  failed: " + e2));
                    }
                    if (err != null && !installCancel) failed.Add(t.Name + ": " + err);
                }
                Ui(() => InstallDone(failed));
            });
            th.IsBackground = true; th.Start();
        }

        void Ui(Action a) { try { BeginInvoke((MethodInvoker)delegate { a(); }); } catch { } }

        string InstallOne(ToolPkg t, string url, string tdir, int idx, int count)
        {
            string tmp = Path.Combine(tdir, t.File + ".part");
            try
            {
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.UserAgent = "Dawnloader"; req.AllowAutoRedirect = true; req.Timeout = 30000; req.ReadWriteTimeout = 60000;
                installReq = req;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
                {
                    long total = resp.ContentLength, done = 0; var buf = new byte[81920]; int n;
                    var sw = Stopwatch.StartNew(); long lastTick = -1000;
                    while ((n = rs.Read(buf, 0, buf.Length)) > 0)
                    {
                        if (installCancel) return "cancelled";
                        fs.Write(buf, 0, n); done += n;
                        if (sw.ElapsedMilliseconds - lastTick > 250)
                        {
                            lastTick = sw.ElapsedMilliseconds;
                            double frac = total > 0 ? (double)done / total : 0; long d = done, tt = total;
                            Ui(() =>
                            {
                                bar.Value = (idx + frac) / count;
                                SetStatus("Installing " + t.Name + " (" + (idx + 1) + " of " + count + ")...  " + T.Bytes(d) + (tt > 0 ? " of " + T.Bytes(tt) : ""), T.Text);
                            });
                        }
                    }
                    if (total > 0 && done < total) return "download was cut off";
                }
                installReq = null;
                Ui(() => SetStatus("Installing " + t.Name + " (" + (idx + 1) + " of " + count + ")...  unpacking", T.Text));
                if (t.PickDir != null)
                {
                    string sub = Path.Combine(tdir, t.SubDir ?? ""); Directory.CreateDirectory(sub); int n = 0;
                    using (var z = new ZipArchive(File.OpenRead(tmp), ZipArchiveMode.Read))
                        foreach (var en in z.Entries)
                        {
                            string fn = en.FullName.Replace('\\', '/');
                            if (fn.IndexOf(t.PickDir, StringComparison.OrdinalIgnoreCase) < 0 || en.Name == "") continue;
                            if (!en.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !en.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                            string x = Path.Combine(sub, en.Name + ".new");
                            using (var es = en.Open()) using (var fo = new FileStream(x, FileMode.Create, FileAccess.Write)) es.CopyTo(fo);
                            PlaceExe(x, Path.Combine(sub, en.Name)); n++;
                        }
                    File.Delete(tmp);
                    if (n == 0) return "nothing usable in the download";
                }
                else if (t.Pick == null) PlaceExe(tmp, Path.Combine(tdir, t.File));
                else
                {
                    using (var z = new ZipArchive(File.OpenRead(tmp), ZipArchiveMode.Read))
                        foreach (var want in t.Pick)
                        {
                            ZipArchiveEntry hit = null;
                            foreach (var en in z.Entries) if (string.Equals(en.Name, want, StringComparison.OrdinalIgnoreCase)) { hit = en; break; }
                            if (hit == null) return want + " not found in the download";
                            string x = Path.Combine(tdir, want + ".new");
                            using (var es = hit.Open()) using (var fo = new FileStream(x, FileMode.Create, FileAccess.Write)) es.CopyTo(fo);
                            PlaceExe(x, Path.Combine(tdir, want));
                        }
                    File.Delete(tmp);
                }
                return null;
            }
            catch (Exception ex)
            {
                if (installCancel) return "cancelled";
                var we = ex as WebException;
                if (we != null && we.Response is HttpWebResponse) return "server said HTTP " + (int)((HttpWebResponse)we.Response).StatusCode;
                return ex.Message;
            }
            finally { installReq = null; try { if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
        }

        static void PlaceExe(string from, string to)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var head = new byte[2];
                using (var f = File.OpenRead(from)) f.Read(head, 0, 2);
                if (head[0] != 'M' || head[1] != 'Z') { File.Delete(from); throw new Exception("the download is not a program (maybe blocked by a network filter)"); }
            }
            if (File.Exists(to)) File.Delete(to);
            File.Move(from, to);
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                try { Process.Start("chmod", "+x \"" + to + "\"").WaitForExit(); } catch { }
        }

        void InstallDone(List<string> failed)
        {
            installing = false; SetBusy(false); btnDownload.Enabled = true;
            FindTools(); ytdlpVersion = "";
            UpdateToolsLabel(); CheckVersion();
            if (installCancel) { resumeAfterQpdf = false; SetStatus("Setup cancelled. Click 'Install missing tools' to try again.", T.Warn); bar.Value = 0; return; }
            if (installingQpdfOnly)
            {
                installingQpdfOnly = false;
                if (qpdf != null)
                {
                    bar.Value = 0; SetStatus("\u2713 PDF tool installed.", T.Ok);
                    foreach (var it in items) if (it.Action == "qpdf") Recompute(it);
                    if (resumeAfterQpdf) { resumeAfterQpdf = false; StartDownloads(null); }
                }
                else { resumeAfterQpdf = false; bar.Value = 0; foreach (var f in failed) Log("INSTALL FAILED - " + f); SetStatus("The PDF tool could not be installed - see Details.", T.Bad); }
                return;
            }
            if (failed.Count == 0 && ytdlp != null && ffmpeg != null)
            {
                bar.Value = 1; SetStatus("\u2713 Tools installed - you're ready. Paste a link and press Download.", T.Ok);
                if (items.Count > 0) { foreach (var it in items) if (it.State == 3) { it.State = 0; SetItemStatus(it, "Checking...", ""); } FetchNext(); }
                return;
            }
            bar.Value = 0;
            SetStatus("Some tools could not be installed - see Details.", T.Bad);
            foreach (var f in failed) Log("INSTALL FAILED - " + f);
            MessageBox.Show(this, "Setup could not finish:\n\n" + string.Join("\n", failed.ToArray()) +
                "\n\nCheck the internet connection and click 'Install missing tools' again.\n\nOr install them yourself - open PowerShell and run:\n" +
                "  winget install yt-dlp.yt-dlp\n  winget install Gyan.FFmpeg\n  winget install DenoLand.Deno\nthen reopen this app.", "Dawnloader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        bool retryAfterUpdate;
        void UpdateYtDlp()
        {
            if (ytdlp == null || busy) return;
            bool viaWinget = ytdlp.IndexOf("WinGet", StringComparison.OrdinalIgnoreCase) >= 0;
            string exe = viaWinget ? "winget" : ytdlp;
            var args = viaWinget ? new List<string> { "upgrade", "--id", "yt-dlp.yt-dlp", "--accept-source-agreements", "--accept-package-agreements", "--disable-interactivity" }
                                 : new List<string> { "-U" };
            SetBusy(true); btnCancel.Visible = false; btnDownload.Visible = true; btnDownload.Enabled = false;
            SetStatus("Updating yt-dlp...", T.Text); Log("> " + exe + " " + string.Join(" ", args.ToArray()));
            var r = new Runner(this);
            r.Line += s => { if (s.Trim().Length > 2) Log(s); };
            r.Exited += code =>
            {
                SetBusy(false); btnDownload.Enabled = true;
                SetStatus(code == 0 ? "\u2713 yt-dlp is up to date." : "Update finished with code " + code + " - see Details. (winget says code -1978335189 when already up to date.)", code == 0 ? T.Ok : T.Warn);
                if (code == -1978335189) SetStatus("\u2713 yt-dlp is already the newest version.", T.Ok);
                CheckVersion();
                if (retryAfterUpdate)
                {
                    retryAfterUpdate = false;
                    foreach (var it in items) if (it.Action == "update") { if (it.State == 3) ResetItem(it); else { it.Action = ""; Recompute(it); } }
                    UpdateSummary(); FetchNext();
                }
            };
            try { r.Start(exe, args, pathEnv); }
            catch (Exception ex) { SetBusy(false); btnDownload.Enabled = true; SetStatus("Couldn't start the update: " + ex.Message, T.Bad); }
        }
    }
}
