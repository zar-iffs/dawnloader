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
                    case "cut":
                        g.DrawEllipse(pen, R(3.5f, 14.5f, 5.5f, 5.5f)); g.DrawEllipse(pen, R(15, 14.5f, 5.5f, 5.5f));
                        g.DrawLine(pen, P(8.2f, 15.5f), P(18, 3.5f)); g.DrawLine(pen, P(15.8f, 15.5f), P(6, 3.5f));
                        break;
                    case "file":
                        g.DrawLines(pen, new[] { P(14, 3), P(6, 3), P(6, 21), P(18, 21), P(18, 7), P(14, 3), P(14, 7), P(18, 7) });
                        g.DrawLine(pen, P(9, 12), P(15, 12)); g.DrawLine(pen, P(9, 16), P(15, 16));
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
            psi.EnvironmentVariables["PYTHONUTF8"] = "1";
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
        public bool NoSubs;               // subtitles failed (site limit): this one is downloaded without them
        public bool FastClip; public string TmpDir = "";   // clip: whole video is downloaded to TmpDir first, then cut
        public bool PlForce;              // user asked (right-click) for the whole playlist of a video link
        public double ClipStart = -1, ClipEnd = -1;
        public string Action = "";        // fix-it button shown in the Status column
        public string DonePath, ArchiveKey = "";
        public bool Archived, ForceAgain;
        public int PlHave;                // playlist videos already downloaded before
        public List<string> PlKeys;
        public bool HasClip { get { return ClipStart >= 0 && ClipEnd > ClipStart; } }
    }

    public class Opts
    {
        public bool Video = true; public int Height; public string Container = "mp4";
        public string AFmt = "mp3"; public string ABr = "0"; public bool Playlist, Subs, Thumb = true, H264First, ClipExact; public string Cookies = ""; public string CookieFile = "";
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

    // Dawnloader does not work with adult (porn) sites: they are refused when a link is added, when it is checked, when it is
    // downloaded, and are left out of the supported-sites list. Matching is by site name / web address, plus the "age_limit" a site reports.
    public static class Adult
    {
        public const string Short = "Blocked: adult site", Why = "Dawnloader doesn't support adult sites, so this link was not checked or downloaded.";
        // pieces of a host name (web address) that belong to adult sites
        static readonly string[] HostTokens = {
            "porn", "xxx", "hentai", "jizz", "sextube", "sexvid", "sexcam", "livesex", "nudevista", "xvideo", "xnxx", "xhamster", "xtube", "redtube", "youporn",
            "spankbang", "spankwire", "chaturbate", "stripchat", "bongacams", "cam4.", "camsoda", "cammodels", "livejasmin", "eporner", "tube8", "tnaflix", "empflix",
            "beeg.", "txxx", "nuvid", "drtuber", "4tube", "thisvid", "rule34", "redgifs", "scrolller", "manyvids", "myvidster", "peekvids", "sunporno", "slutload",
            "eroprofile", "erocast", "erome.", "toypics", "behindkink", "murrtube", "nonktube", "zenporn", "brazzers", "onlyfans", "fansly", "motherless", "fapello",
            "nhentai", "hanime", "vporn", "tubegalore", "faphouse", "clips4sale", "keezmovies", "extremetube", "mofosex", "camwhores", "fux.com", "xmovies" };
        // pieces of a yt-dlp extractor name (lower case)
        static readonly string[] KeyTokens = {
            "porn", "xxx", "sexu", "hentai", "jizz", "redtube", "xvideos", "xnxx", "xhamster", "xtube", "spankbang", "spankwire", "chaturbate", "stripchat", "bongacams",
            "cam4", "camsoda", "cammodels", "eporner", "tube8", "tnaflix", "empflix", "beeg", "txxx", "nuvid", "drtuber", "fourtube", "thisvid", "rule34", "redgifs",
            "scrolller", "manyvids", "myvidster", "peekvids", "sunporno", "slutload", "eroprofile", "erocast", "toypics", "behindkink", "murrtube", "nonktube", "zenporn",
            "youporn", "noodlemagazine", "nubiles", "keezmovies", "extremetube", "mofosex", "motherless", "camwhores", "fux" };
        public static bool Host(string host)
        {
            if (string.IsNullOrEmpty(host)) return false;
            host = host.ToLowerInvariant() + ".";
            foreach (var t in HostTokens) if (host.Contains(t)) return true;
            return Regex.IsMatch(host, @"(^|[.-])sex[.-]");     // sex.com, sex-tube.net ... (not "essex", "sussex")
        }
        public static bool Url(string url)
        {
            // the whole address part, including anything before an "@" (so name@host tricks are caught too)
            var m = Regex.Match(url ?? "", @"^[a-z][a-z0-9+.-]*://([^/?#]+)", RegexOptions.IgnoreCase);
            return m.Success && Host(m.Groups[1].Value);
        }
        public static bool Extractor(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            key = key.ToLowerInvariant();
            foreach (var t in KeyTokens) if (key.Contains(t)) return true;
            return false;
        }
    }

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

        public static string HostOf(string url)
        {
            var m = Regex.Match(url ?? "", @"^[a-z][a-z0-9+.-]*://(?:[^/@]*@)?([^/:?#]+)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : url;
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

    // ------------------------------------------------------------------ small picture helpers
    public static class Pics
    {
        // neutral rounded badge with a letter - shown when the real icon isn't available
        public static void Letter(Graphics g, Rectangle r, string letter, Color bg, Color fg)
        {
            var sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = T.Round(new RectangleF(r.X, r.Y, r.Width - 1, r.Height - 1), r.Width * 0.28f)) using (var b = new SolidBrush(bg)) g.FillPath(b, p);
            using (var f = new Font(T.FontName, r.Height * 0.52f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(fg))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(letter, f, b, new RectangleF(r.X, r.Y + 0.5f, r.Width, r.Height), sf);
            g.SmoothingMode = sm;
        }

        // first existing file among <base>\<rel> for the usual install folders
        public static string FindExe(params string[] rels)
        {
            var bases = new List<string>();
            foreach (var v in new[] { "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432", "LOCALAPPDATA" })
            { string b = Environment.GetEnvironmentVariable(v); if (!string.IsNullOrEmpty(b) && !bases.Contains(b)) bases.Add(b); }
            foreach (var b in bases) foreach (var r in rels) { try { string f = Path.Combine(b, r); if (File.Exists(f)) return f; } catch { } }
            return null;
        }
        public static string AppPath(string exeName)
        {
            foreach (var hive in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
            {
                try
                {
                    using (var k = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exeName))
                    {
                        string v = k == null ? null : k.GetValue("") as string;
                        if (!string.IsNullOrEmpty(v)) { v = v.Trim('"'); if (File.Exists(v)) return v; }
                    }
                }
                catch { }
            }
            return null;
        }
        public static bool HasDir(string envVar, string rel)
        {
            try { string b = Environment.GetEnvironmentVariable(envVar); return !string.IsNullOrEmpty(b) && Directory.Exists(Path.Combine(b, rel)); } catch { return false; }
        }
        public static Bitmap ExeIcon(string path)
        {
            if (path == null) return null;
            try { using (var ic = Icon.ExtractAssociatedIcon(path)) return ic == null ? null : ic.ToBitmap(); } catch { return null; }
        }

        // A site's own icon (favicon), downloaded once and cached. 'done' runs on a worker thread.
        public static void Favicon(string key, string url, string cacheDir, Action<Bitmap> done)
        {
            string f = Path.Combine(cacheDir, key + ".png");
            try
            {
                if (File.Exists(f) && (DateTime.Now - File.GetLastWriteTime(f)).TotalDays < 60)
                    using (var img = Image.FromFile(f)) { done(new Bitmap(img)); return; }
            }
            catch { }
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    byte[] data;
                    using (var wc = new WebClient()) { wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Dawnloader"; data = wc.DownloadData(url); }
                    Bitmap bmp = null;
                    using (var ms = new MemoryStream(data))
                    {
                        try { using (var ic = new Icon(ms, 32, 32)) bmp = ic.ToBitmap(); } catch { bmp = null; }
                        if (bmp == null) { ms.Position = 0; using (var img = Image.FromStream(ms)) bmp = new Bitmap(img); }
                    }
                    try { Directory.CreateDirectory(cacheDir); bmp.Save(f, System.Drawing.Imaging.ImageFormat.Png); } catch { }
                    done(bmp);
                }
                catch { }
            });
        }
    }

    // ------------------------------------------------------------------ header: popular site icons + "1000+ more"
    public class SiteStrip : Control
    {
        public class SiteInfo { public string Name, Letter; public Bitmap Img; }
        public List<SiteInfo> Sites = new List<SiteInfo>();
        public string MoreText = "+ 1000 more sites";
        public event EventHandler MoreClicked;
        int hot = -2; ToolTip tip; Font under;
        public SiteStrip(ToolTip t)
        {
            tip = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent;
            Height = T.P(22); under = new Font(T.HeaderSub, FontStyle.Underline);
        }
        int Ic { get { return T.P(20); } }
        int Gap { get { return T.P(6); } }
        Rectangle IconRect(int i) { return new Rectangle(i * (Ic + Gap), (Height - Ic) / 2, Ic, Ic); }
        Rectangle MoreRect()
        {
            var sz = TextRenderer.MeasureText(MoreText, T.HeaderSub, Size.Empty, TextFormatFlags.NoPadding);
            return new Rectangle(Sites.Count * (Ic + Gap) + T.P(4), (Height - sz.Height) / 2, sz.Width, sz.Height);
        }
        public int PreferredW() { return MoreRect().Right + T.P(2); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur; T.PaintUnder(g, this);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            for (int i = 0; i < Sites.Count; i++)
            {
                var r = IconRect(i); var s = Sites[i];
                // white plate so every site icon reads on any header colour
                using (var p = T.Round(new RectangleF(r.X, r.Y, r.Width - 1, r.Height - 1), T.P(5)))
                using (var b = new SolidBrush(Color.FromArgb(hot == i ? 255 : 238, 255, 255, 255))) g.FillPath(b, p);
                if (s.Img != null) g.DrawImage(s.Img, Rectangle.Inflate(r, -T.P(3), -T.P(3)));
                else Pics.Letter(g, Rectangle.Inflate(r, -T.P(2), -T.P(2)), s.Letter, Color.Transparent, Color.FromArgb(60, 66, 80));
            }
            var mr = MoreRect();
            TextRenderer.DrawText(g, MoreText, hot == -1 ? under : T.HeaderSub, mr.Location, hot == -1 ? k.HeadText : k.HeadSub, TextFormatFlags.NoPadding);
        }
        int HitTest(Point pt)
        {
            if (Rectangle.Inflate(MoreRect(), T.P(3), T.P(3)).Contains(pt)) return -1;
            for (int i = 0; i < Sites.Count; i++) if (IconRect(i).Contains(pt)) return i;
            return -2;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != hot)
            {
                hot = h; Cursor = h == -2 ? Cursors.Default : Cursors.Hand;
                tip.SetToolTip(this, h >= 0 ? Sites[h].Name + "  -  click for the full list" : h == -1 ? "See every site Dawnloader can download from" : "");
                Invalidate();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { hot = -2; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && HitTest(e.Location) != -2 && MoreClicked != null) MoreClicked(this, EventArgs.Empty);
            base.OnMouseUp(e);
        }
    }

    // ------------------------------------------------------------------ row of small icon choices (sign-in browser picker)
    public class ChoiceRow : Control
    {
        public class Choice { public int Value; public string Text, Tip, Letter, IconName; public Bitmap Img; public bool Show = true; }
        public List<Choice> Items = new List<Choice>();   // in display order
        int sel, hot = -1; ToolTip tip;
        public event EventHandler SelectedIndexChanged;
        public ChoiceRow(ToolTip t)
        {
            tip = t;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true); BackColor = Color.Transparent;
            Height = T.P(32);
        }
        public int SelectedIndex
        {
            get { return sel; }
            set { if (sel == value) return; sel = value; Invalidate(); if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty); }
        }
        public Choice Selected { get { foreach (var c in Items) if (c.Value == sel) return c; return null; } }
        List<KeyValuePair<Choice, Rectangle>> Chips()
        {
            var l = new List<KeyValuePair<Choice, Rectangle>>(); int x = 0;
            foreach (var c in Items)
            {
                if (!c.Show && c.Value != sel) continue;
                int w = c.Text != null ? TextRenderer.MeasureText(c.Text, T.Body).Width + T.P(20) : Height;
                l.Add(new KeyValuePair<Choice, Rectangle>(c, new Rectangle(x, 0, w, Height))); x += w + T.P(6);
            }
            return l;
        }
        public int PreferredW() { var l = Chips(); return l.Count == 0 ? 0 : l[l.Count - 1].Value.Right + 1; }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur; T.PaintUnder(g, this);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            foreach (var kv in Chips())
            {
                var c = kv.Key; var r = kv.Value; bool on = c.Value == sel, h = c.Value == hot && Enabled;
                Color bg = on ? k.AccentTint : h ? k.Hover : k.Input, bd = on ? k.Accent : h ? k.BorderHover : k.Border;
                if (!Enabled) bd = k.Border;
                using (var p = T.Round(new RectangleF(r.X + 1, r.Y + 1, r.Width - 2.5f, r.Height - 2.5f), T.P(8)))
                {
                    using (var b = new SolidBrush(bg)) g.FillPath(b, p);
                    using (var pen = new Pen(bd, on ? 2f * Math.Max(1f, T.S) : 1f)) g.DrawPath(pen, p);
                }
                var ir = Rectangle.Inflate(r, -T.P(6), -T.P(6));
                Color fg = !Enabled ? k.TextOff : on ? k.Accent : k.Text;
                if (c.Text != null) TextRenderer.DrawText(g, c.Text, on ? T.BodyBold : T.Body, r, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                else if (c.Img != null)
                {
                    if (Enabled) g.DrawImage(c.Img, ir);
                    else
                    {
                        var cm = new System.Drawing.Imaging.ColorMatrix(new float[][] {
                            new float[] { .3f, .3f, .3f, 0, 0 }, new float[] { .59f, .59f, .59f, 0, 0 }, new float[] { .11f, .11f, .11f, 0, 0 },
                            new float[] { 0, 0, 0, .45f, 0 }, new float[] { 0, 0, 0, 0, 1 } });
                        using (var ia = new System.Drawing.Imaging.ImageAttributes())
                        {
                            ia.SetColorMatrix(cm);
                            g.DrawImage(c.Img, ir, 0, 0, c.Img.Width, c.Img.Height, GraphicsUnit.Pixel, ia);
                        }
                    }
                }
                else if (c.IconName != null) Icons.Draw(g, c.IconName, ir, fg);
                else Pics.Letter(g, ir, c.Letter, k.Soft, fg);
            }
            if (Focused && ShowFocusCues) foreach (var kv in Chips()) if (kv.Key.Value == sel)
                using (var p = T.Round(Rectangle.Inflate(kv.Value, -1, -1), T.P(8))) using (var pen = new Pen(Color.FromArgb(120, k.Accent), 2f)) g.DrawPath(pen, p);
        }
        int HitTest(Point pt) { foreach (var kv in Chips()) if (kv.Value.Contains(pt)) return kv.Key.Value; return -1; }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (h != hot)
            {
                hot = h; Cursor = h >= 0 && Enabled ? Cursors.Hand : Cursors.Default; Invalidate();
                string t = ""; foreach (var c in Items) if (c.Value == h) t = c.Tip;
                tip.SetToolTip(this, t);
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { hot = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { Focus(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            int h = HitTest(e.Location);
            if (e.Button == MouseButtons.Left && h >= 0 && Enabled)
            {
                if (h == sel && Picked != null) Picked(this, EventArgs.Empty);   // e.g. pick another cookies file
                SelectedIndex = h;
            }
            base.OnMouseUp(e);
        }
        public event EventHandler Picked;   // the already-selected choice was clicked again
        protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var l = Chips(); int i = l.FindIndex(kv => kv.Key.Value == sel);
            if (e.KeyCode == Keys.Left && i > 0) SelectedIndex = l[i - 1].Key.Value;
            if (e.KeyCode == Keys.Right && i >= 0 && i < l.Count - 1) SelectedIndex = l[i + 1].Key.Value;
            base.OnKeyDown(e);
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    }

    // ------------------------------------------------------------------ clip range slider (two handles)
    public class ClipBar : Control
    {
        public double Duration, Start, End;
        int drag; double grab;
        public event Action<bool> Changed;   // true = user let go
        public ClipBar()
        {
           
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent;
            Height = T.P(44);
        }
        bool Live { get { return Enabled && Duration > 0; } }
        float X0 { get { return T.P(10); } }
        float X1 { get { return Width - T.P(10); } }
        float Ty { get { return T.P(12); } }
        float Px(double t) { return Duration <= 0 ? X0 : X0 + (float)(Math.Max(0, Math.Min(Duration, t)) / Duration) * (X1 - X0); }
        double Tm(float x) { return Duration <= 0 ? 0 : Math.Max(0, Math.Min(Duration, (x - X0) / (X1 - X0) * Duration)); }
        public void Set(double dur, double s, double e) { Duration = dur; Start = s; End = e; Invalidate(); }
        static string Clock(double sec) { return sec < 0.5 ? "0:00" : T.Dur(sec); }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; var k = T.Cur; T.PaintUnder(g, this); g.SmoothingMode = SmoothingMode.AntiAlias;
            float th = T.P(6), r = T.P(8);
            using (var p = T.Round(new RectangleF(X0, Ty - th / 2, X1 - X0, th), th / 2)) using (var b = new SolidBrush(k.Track)) g.FillPath(b, p);
            if (!Live) return;
            float a = Px(Start), z = Px(End);
            using (var p = T.Round(new RectangleF(a, Ty - th / 2, Math.Max(th, z - a), th), th / 2)) using (var b = new SolidBrush(k.Accent)) g.FillPath(b, p);
            foreach (var x in new[] { a, z })
            {
                using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, x - r, Ty - r, 2 * r, 2 * r);
                using (var pen = new Pen(k.Accent, 2f * Math.Max(1f, T.S))) g.DrawEllipse(pen, x - r, Ty - r, 2 * r, 2 * r);
            }
            // times under the handles, kept apart and inside the control
            string sa = Clock(Start), sz = Clock(End);
            int wa = TextRenderer.MeasureText(sa, T.Small).Width, wz = TextRenderer.MeasureText(sz, T.Small).Width;
            float la = Math.Max(0, Math.Min(Width - wa, a - wa / 2f)), lz = Math.Max(0, Math.Min(Width - wz, z - wz / 2f));
            float gap = T.P(6);
            if (lz < la + wa + gap)
            {
                float mid = (a + z) / 2f; la = mid - wa - gap / 2; lz = mid + gap / 2;
                if (la < 0) { lz -= la; la = 0; }
                if (lz + wz > Width) { la -= lz + wz - Width; lz = Width - wz; }
                la = Math.Max(0, la);
            }
            int ty = (int)(Ty + r + T.P(4));
            TextRenderer.DrawText(g, sa, T.Small, new Point((int)la, ty), k.Muted, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, sz, T.Small, new Point((int)lz, ty), k.Muted, TextFormatFlags.NoPadding);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!Live || e.Button != MouseButtons.Left) return;
            float a = Px(Start), z = Px(End), x = e.X;
            if (Math.Abs(x - a) <= T.P(12) || Math.Abs(x - z) <= T.P(12)) drag = Math.Abs(x - a) < Math.Abs(x - z) || (Math.Abs(x - a) == Math.Abs(x - z) && x < a) ? 1 : 2;
            else if (x > a && x < z) { drag = 3; grab = Tm(x) - Start; }
            else { drag = x < a ? 1 : 2; MoveTo(x); }
            Capture = true;
        }
        void MoveTo(float x)
        {
            double t = Tm(x), minGap = Math.Min(1.0, Duration / 2);
            if (drag == 1) Start = Math.Min(t, End - minGap);
            else if (drag == 2) End = Math.Max(t, Start + minGap);
            else if (drag == 3) { double len = End - Start; Start = Math.Max(0, Math.Min(Duration - len, t - grab)); End = Start + len; }
            Start = Math.Round(Start); End = Math.Round(End); if (End > Duration) End = Duration;
            Invalidate(); if (Changed != null) Changed(false);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (drag != 0) MoveTo(e.X);
            else if (Live) { float a = Px(Start), z = Px(End); Cursor = Math.Abs(e.X - a) <= T.P(12) || Math.Abs(e.X - z) <= T.P(12) ? Cursors.SizeWE : e.X > a && e.X < z ? Cursors.Hand : Cursors.Default; }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (drag != 0) { drag = 0; Capture = false; if (Changed != null) Changed(true); }
            base.OnMouseUp(e);
        }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
    }

    // ------------------------------------------------------------------ the clip box in card 2
    public class ClipBox : Panel, ISurface
    {
        public string Sub = "";
        public ClipBox()
        {
           
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent;
        }
        public void PaintSurface(Graphics g)
        {
            T.PaintUnder(g, this);
            var sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = T.Round(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), T.P(10)))
            {
                using (var b = new SolidBrush(T.Cur.ColHead)) g.FillPath(b, p);
                using (var pen = new Pen(T.Border)) g.DrawPath(pen, p);
            }
            g.SmoothingMode = sm;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; PaintSurface(g);
            float d = T.P(18);
            Icons.Draw(g, "cut", new RectangleF(T.P(14), T.P(13), d, d), T.Accent);
            int x = T.P(14) + (int)d + T.P(8);
            TextRenderer.DrawText(g, "Clip a part", T.Title, new Point(x, T.P(13) + ((int)d - T.Title.Height) / 2), T.Text, TextFormatFlags.NoPadding);
            int w = TextRenderer.MeasureText("Clip a part", T.Title, Size.Empty, TextFormatFlags.NoPadding).Width;
            TextRenderer.DrawText(g, "optional", T.Small, new Point(x + w + T.P(8), T.P(13) + ((int)d - T.Small.Height) / 2 + T.P(1)), T.Hint, TextFormatFlags.NoPadding);
        }
    }

    // ------------------------------------------------------------------ main window
    public class MainForm : Form, ISurface
    {
        string dir, ytdlp, ffmpeg, deno, pathEnv, settingsFile, ytdlpVersion = "";
        HeaderPanel header; Panel footer; Label lblTitle, lblTools; FlatButton btnUpdate, btnSkin, btnAbout;
        Card cLinks, cOpts, cQueue;
        TextBox txtLinks; Label lblHint; FlatButton btnPaste, btnClear; CheckBox ckCatch;
        Tile tVideo, tAudio; Panel pVideo, pAudio, pMore;
        ComboBox cbQuality, cbContainer, cbAFmt, cbABr; ChoiceRow cbCookies; CheckBox ckSubs, ckThumb; ContextMenuStrip rowMenu, skinMenu; Label lblVideoHint;
        Label lblAudioHint, lblCookieInfo; Panel pSign; SiteStrip sites;
        ClipBox clipBox; ClipBar clipBar; TextBox tClipS, tClipE; Label lblClipItem, lblClipRes, lblClipHelp; FlatButton btnWhole; CheckBox ckExact; Item clipItem; bool clipSync; int ffLines;
        Panel lvBox, lvHead; ListView lv; ImageList thumbs; Label lblSummary, lblEmpty; TextBox txtOut; FlatButton btnBrowse, btnOpen;
        Bar bar; Label lblStatus; FlatButton btnDownload, btnCancel, btnLog;
        ToolTip tip = new ToolTip();
        Form logForm; TextBox logBox; StringBuilder log = new StringBuilder();
        List<Item> items = new List<Item>();
        System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();
        Runner fetchRunner, dlRunner; Item fetching; StringBuilder fetchOut = new StringBuilder(); int fetchStage;
        bool busy, cancelAll, loading = true; string lastCookies = "";
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
            loading = false; lastCookies = GetOpts().Cookies;
            ApplyMode();
            debounce.Interval = 700; debounce.Tick += (s, e) => { debounce.Stop(); SyncItems(); };
            clipTimer.Interval = 1000; clipTimer.Tick += (s, e) => { if (!winClip) CatchClipboard(); };
            Shown += (s, e) =>
            {
                ApplyWindowTheme();
                CheckVersion(); UpdateToolsLabel(); StartClipboardWatch(); CleanOldTemp();
                if (ytdlp == null || ffmpeg == null)
                    BeginInvoke((MethodInvoker)delegate { AskInstall("This app needs a few free helper tools before it can download."); });
            };
            FormClosing += (s, e) =>
            {
                SaveSettings(); if (dlRunner != null) dlRunner.Kill(); if (fetchRunner != null) fetchRunner.Kill();
                try { foreach (var f in Directory.GetFiles(Path.GetTempPath(), "ytd-cookies-*.txt")) File.Delete(f); } catch { }   // temporary sign-in copies
            };
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
            parts.Add(ToolsDir);
            var sb = new StringBuilder(); sb.Append(dir);
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) { sb.Append(Path.PathSeparator); sb.Append(p); }
            pathEnv = sb.ToString();
            ytdlp = Find("yt-dlp"); ffmpeg = Find("ffmpeg"); deno = Find("deno");
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
            int W = T.P(940), H = T.P(840), M = T.P(16);
            ClientSize = new Size(W, H);
            MinimumSize = new Size(T.P(860), T.P(680));
            BackColor = T.Solid(T.Bg);
            SuspendLayout();

            // header
            header = new HeaderPanel(); header.Size = new Size(W, T.P(72)); header.Dock = DockStyle.Top;
            header.Resize += (s, e) => PlaceTools();
            lblTitle = L("Dawnloader", T.Header, T.Cur.HeadText); lblTitle.Location = new Point(T.P(72), T.P(10));
            sites = new SiteStrip(tip); sites.Location = new Point(T.P(73), T.P(43));
            string iconDir = Path.Combine(DataDir, "icons");
            foreach (var spec in new[] {
                "YouTube|Y|youtube|https://www.youtube.com/favicon.ico", "TikTok|T|tiktok|https://www.tiktok.com/favicon.ico",
                "Instagram|I|instagram|https://www.instagram.com/favicon.ico", "Facebook|f|facebook|https://www.facebook.com/favicon.ico",
                "X (Twitter)|X|x|https://x.com/favicon.ico", "Google Drive|D|drive|https://ssl.gstatic.com/images/branding/product/1x/drive_2020q4_32dp.png" })
            {
                var sp = spec.Split('|'); var site = new SiteStrip.SiteInfo(); site.Name = sp[0]; site.Letter = sp[1]; sites.Sites.Add(site);
                var st = sites;
                Pics.Favicon(sp[2], sp[3], iconDir, bmp =>
                {
                    if (!st.IsHandleCreated) { site.Img = bmp; return; }
                    try { st.BeginInvoke((MethodInvoker)delegate { site.Img = bmp; st.Invalidate(); }); } catch { }
                });
            }
            sites.Size = new Size(sites.PreferredW(), T.P(22));
            sites.MoreClicked += (s, e) => ShowSites();
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
            header.Controls.AddRange(new Control[] { lblTitle, sites, btnUpdate, btnSkin, btnAbout, lblTools });

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
            lblHint = L("One link per line - YouTube, TikTok, Instagram, Facebook, X, Google Drive, direct .mp4 links ...", T.Body, T.Hint);
            lblHint.BackColor = T.Input; lblHint.Location = new Point(T.P(4), T.P(4)); lblHint.Cursor = Cursors.IBeam;
            lblHint.Click += (s, e) => txtLinks.Focus();
            txtLinks.Controls.Add(lblHint);
            ckCatch = new CheckBox(); ckCatch.Text = "Catch copied links"; ckCatch.Font = T.Body; ckCatch.ForeColor = T.Text; ckCatch.AutoSize = true; ckCatch.BackColor = Color.Transparent;
            ckCatch.Size = ckCatch.PreferredSize; ckCatch.Location = new Point(btnPaste.Right - ckCatch.Width, T.P(14) + (T.P(24) - ckCatch.Height) / 2);
            ckCatch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ckCatch.CheckedChanged += (s, e) => { if (loading) return; lastClip = SafeClipboardText(); StartClipboardWatch(); SetStatus(ckCatch.Checked ? "Copy a video or Google Drive link anywhere - it will appear here automatically." : "Catching copied links is off.", T.Muted); };
            tip.SetToolTip(ckCatch, "When on, links you copy (Ctrl+C) in your browser or any app are added here automatically.\nOnly video and Google Drive links are picked up.");
            cLinks.Controls.AddRange(new Control[] { txtLinks, btnPaste, btnClear, ckCatch });
            y = cLinks.Bottom + T.P(10);

            // card 2: options (left) + clip box (right)
            cOpts = new Card("2", "Choose what you want"); cOpts.SetBounds(M, y, CW, T.P(240)); cOpts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            int clipW = T.P(356), LW = CW - 2 * M - clipW - T.P(18);
            tVideo = new Tile("Video", "Picture + sound", "video"); tAudio = new Tile("Audio only", "Music, podcasts", "audio");
            int th = T.P(52);
            foreach (var t in new[] { tVideo, tAudio }) t.Height = th;
            int tw = (LW - T.P(10)) / 2;
            tVideo.SetBounds(M, cOpts.ContentTop, tw, th); tAudio.SetBounds(tVideo.Right + T.P(10), cOpts.ContentTop, tw, th);
            tVideo.Picked += (s, e) => PickMode(tVideo); tAudio.Picked += (s, e) => PickMode(tAudio);
            int oy = tVideo.Bottom + T.P(8), rh = T.P(34), hh = T.P(17);
            pVideo = Clear(new Panel()); pVideo.SetBounds(M, oy, LW, rh + hh);
            pAudio = Clear(new Panel()); pAudio.Bounds = pVideo.Bounds;
            cbQuality = Combo(new[] { "Best available", "2160p (4K)", "1440p (2K)", "1080p (Full HD)", "720p (HD)", "480p", "360p" }, 156);
            cbContainer = Combo(new[] { "MP4", "MKV", "WEBM" }, 86);
            lblVideoHint = Hint();
            Row(pVideo, 0, 0, rh, L("Quality", T.Body, T.Muted), cbQuality, 10, L("Format", T.Body, T.Muted), cbContainer);
            PlaceHint(pVideo, lblVideoHint, rh);
            cbAFmt = Combo(new[] { "MP3", "M4A", "OPUS", "FLAC", "WAV", "Original (no conversion)" }, 186);
            cbABr = Combo(new[] { "Best", "320 kbps", "192 kbps", "128 kbps" }, 104);
            lblAudioHint = Hint();
            Row(pAudio, 0, 0, rh, L("Format", T.Body, T.Muted), cbAFmt, 10, L("Quality", T.Body, T.Muted), cbABr);
            PlaceHint(pAudio, lblAudioHint, rh);
            pMore = Clear(new Panel()); pMore.SetBounds(M, pVideo.Bottom + T.P(2), LW, T.P(28));
            ckSubs = Check("English subtitles"); ckThumb = Check("Cover art + title tags");
            Row(pMore, 0, 0, T.P(28), ckSubs, 14, ckThumb);
            pSign = Clear(new Panel()); pSign.SetBounds(M, pMore.Bottom + T.P(4), LW, T.P(34));
            cbCookies = BuildSignIn();
            cbCookies.SelectedIndexChanged += (s, e) => { OptionsChanged(); if (!loading && cbCookies.SelectedIndex == 5) PickCookiesFile(); };
            cbCookies.Picked += (s, e) => { if (!loading && cbCookies.SelectedIndex == 5) PickCookiesFile(); };
            lblCookieInfo = new Label(); lblCookieInfo.Size = new Size(T.P(22), T.P(22)); lblCookieInfo.BackColor = Color.Transparent; lblCookieInfo.Cursor = Cursors.Help;
            lblCookieInfo.Paint += (s, e) => Icons.Draw(e.Graphics, "info", new RectangleF(1, 1, lblCookieInfo.Width - 2, lblCookieInfo.Width - 2), T.Muted);
            int sx = Row(pSign, 0, 0, T.P(34), L("Sign in with", T.Body, T.Muted), lblCookieInfo);
            cbCookies.Location = new Point(sx + T.P(2), (pSign.Height - cbCookies.Height) / 2); cbCookies.Width = Math.Max(T.P(60), LW - cbCookies.Left);
            pSign.Controls.Add(cbCookies);
            tip.SetToolTip(lblCookieInfo, "Most links work without signing in - leave this Off.\n\n" +
                "Pick your browser only for private or age-restricted videos,\nor when a site asks you to sign in.\n" +
                "Dawnloader uses the login already saved in that browser.\n\nFirefox works best.");
            cOpts.Height = pSign.Bottom + M - T.P(2);

            clipBox = new ClipBox(); clipBox.SetBounds(M + LW + T.P(18), T.P(12), CW - M - (M + LW + T.P(18)), cOpts.Height - T.P(12) - M + T.P(2));
            clipBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            int cw = clipBox.Width, cp = T.P(14);
            btnWhole = new FlatButton("Whole video", "", 4); btnWhole.Font = T.Small; btnWhole.Size = new Size(btnWhole.PreferredWidth(T.P(26)), T.P(26));
            btnWhole.Location = new Point(cw - T.P(10) - btnWhole.Width, T.P(9)); btnWhole.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnWhole.Visible = false;
            tip.SetToolTip(btnWhole, "Undo the clip - download everything");
            btnWhole.Click += (s, e) => { if (clipItem == null) return; clipItem.ClipStart = clipItem.ClipEnd = -1; Recompute(clipItem); UpdateSummary(); lv.Invalidate(); };
            lblClipItem = L("", T.Small, T.Muted); lblClipItem.AutoSize = false; lblClipItem.AutoEllipsis = true;
            lblClipItem.SetBounds(cp, T.P(38), cw - 2 * cp, T.P(20)); lblClipItem.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clipBar = new ClipBar(); clipBar.SetBounds(cp - T.P(6), T.P(62), cw - 2 * cp + T.P(12), T.P(44)); clipBar.Anchor = lblClipItem.Anchor;
            clipBar.Changed += fin =>
            {
                if (clipSync || clipItem == null) return;
                clipSync = true; tClipS.Text = Clock(clipBar.Start); tClipE.Text = Clock(clipBar.End); clipSync = false;
                if (fin) ApplyClip(clipBar.Start, clipBar.End);
                else { lblClipRes.Text = "Clip: " + T.Dur(Math.Max(1, clipBar.End - clipBar.Start)); lblClipRes.ForeColor = T.Accent; }
            };
            tClipS = Box(); tClipS.Width = T.P(80); tClipE = Box(); tClipE.Width = T.P(80);
            tClipS.TextChanged += (s, e) => ClipTyped(); tClipE.TextChanged += (s, e) => ClipTyped();
            ckExact = Check("Exact"); ckExact.Checked = false;
            Row(clipBox, cp, T.P(108), T.P(32), L("Start", T.Body, T.Muted), tClipS, 10, L("End", T.Body, T.Muted), tClipE, 12, ckExact);
            tip.SetToolTip(ckExact, "On: cuts exactly at your times. The clip is re-encoded, which takes longer.\n" +
                "Off (default): instant and keeps the original quality, but the start can be\na second or two early (it snaps to the nearest keyframe).");
            lblClipRes = L("", T.BodyBold, T.Text); lblClipRes.AutoSize = false; lblClipRes.AutoEllipsis = true;
            lblClipRes.SetBounds(cp, T.P(146), cw - 2 * cp, T.P(22)); lblClipRes.Anchor = lblClipItem.Anchor;
            lblClipHelp = L("", T.Small, T.Hint); lblClipHelp.AutoSize = false; lblClipHelp.AutoEllipsis = true;
            lblClipHelp.SetBounds(cp, T.P(168), cw - 2 * cp, T.P(20)); lblClipHelp.Anchor = lblClipItem.Anchor;
            clipBox.Controls.AddRange(new Control[] { btnWhole, lblClipItem, clipBar, lblClipRes, lblClipHelp });
            cOpts.Controls.AddRange(new Control[] { tVideo, tAudio, pVideo, pAudio, pMore, pSign, clipBox });
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
            SetImage("ph", Placeholder("play"));
            lvBox = new Panel(); lvBox.BackColor = T.Line; lvBox.Padding = new Padding(1);
            lvBox.SetBounds(M, cQueue.ContentTop, CW - 2 * M, btnOpen.Top - T.P(12) - cQueue.ContentTop);
            lvBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lvHead = new Panel(); lvHead.Height = T.P(28); lvHead.Dock = DockStyle.Top; lvHead.BackColor = T.Cur.ColHead;
            lvHead.Paint += (s, e) => PaintListHeader(e.Graphics);
            lv = new ListView(); lv.View = View.Details; lv.FullRowSelect = true; lv.HeaderStyle = ColumnHeaderStyle.None; lv.BorderStyle = BorderStyle.None;
            lv.Font = T.Body; lv.SmallImageList = thumbs; lv.MultiSelect = false; lv.ShowItemToolTips = true; lv.Dock = DockStyle.Fill;
            lv.BackColor = T.Input; lv.ForeColor = T.Text; lv.OwnerDraw = true;
            lv.Columns.Add("Title", T.P(366)); lv.Columns.Add("Length", T.P(76), HorizontalAlignment.Right);
            lv.Columns.Add("Size (est.)", T.P(92), HorizontalAlignment.Right); lv.Columns.Add("Status", T.P(236));
            lv.Columns.Add("", IconColW);
            lv.DrawColumnHeader += (s, e) => { e.DrawDefault = true; };
            lv.DrawItem += (s, e) => { };
            lv.DrawSubItem += (s, e) => DrawCell(e);
            lv.MouseUp += (s, e) => { if (e.Button == MouseButtons.Left) ListClick(e.Location); };
            lv.MouseLeave += (s, e) => { SetHotIcon(null, null); if (hotRow != null && hotRow.ListView == lv) lv.Invalidate(hotRow.Bounds); hotRow = null; };
            lv.MouseMove += (s, e) =>
            {
                Item ii; string icon = IconAt(e.Location, out ii);
                SetHotIcon(ii, icon);
                lv.Cursor = PillAt(e.Location) != null || icon != null ? Cursors.Hand : Cursors.Default;
                var h = lv.GetItemAt(T.P(4), e.Y);      // owner-drawn rows: repaint fully when the hot row changes (WinForms hover glitch)
                if (h != hotRow) { if (hotRow != null && hotRow.ListView == lv) lv.Invalidate(hotRow.Bounds); if (h != null) lv.Invalidate(h.Bounds); hotRow = h; }
            };
            lv.Resize += (s, e) => FitColumns();
            lv.SelectedIndexChanged += (s, e) => SyncClip();
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
            // shrink to the screen only now, after every control is placed, so anchored controls shrink with the window
            var wa = Screen.FromControl(this).WorkingArea;
            if (Height > wa.Height) { Height = wa.Height; Top = wa.Top; }
            if (Width > wa.Width) { Width = wa.Width; Left = wa.Left; }
            if (MinimumSize.Height > wa.Height) MinimumSize = new Size(MinimumSize.Width, wa.Height);
        }

        Label Hint() { var l = L("", T.Small, T.Muted); l.AutoSize = false; l.AutoEllipsis = true; l.TextAlign = ContentAlignment.MiddleLeft; return l; }
        void PlaceHint(Panel p, Label l, int top)
        {
            l.SetBounds(T.P(2), top, p.Width - T.P(2), p.Height - top); l.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
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
                float s = b.Height * 0.45f; Icons.Draw(g, icon, new RectangleF((b.Width - T.P(6) - s) / 2, (b.Height - s) / 2, s, s), T.Cur.Hint);
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
            int fixedW = lv.Columns[1].Width + lv.Columns[2].Width + lv.Columns[3].Width + (lv.Columns.Count > 4 ? lv.Columns[4].Width : 0);
            lv.Columns[0].Width = Math.Max(T.P(160), lv.Width - SystemInformation.VerticalScrollBarWidth - fixedW - T.P(8));
            if (lvHead != null) lvHead.Invalidate();
        }
        void PaintListHeader(Graphics g)
        {
            using (var b = new SolidBrush(T.Cur.ColHead)) g.FillRectangle(b, lvHead.ClientRectangle);
            using (var pen = new Pen(T.Line)) g.DrawLine(pen, 0, lvHead.Height - 1, lvHead.Width, lvHead.Height - 1);
            string[] names = { "Title", "Length", "Size (est.)", "Status" };
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
            tVideo.Checked = mode != "Audio"; tAudio.Checked = mode == "Audio";
            SetIdx(cbQuality, d, "Quality", 3); SetIdx(cbContainer, d, "Container", 0); SetIdx(cbAFmt, d, "AFmt", 0);
            SetIdx(cbABr, d, "ABr", 0);
            if (d.TryGetValue("CookieFile", out v)) cookiesFilePath = v;
            SetIdx(cbCookies, d, "Cookies", 0);
            if (cbCookies.SelectedIndex == 5 && !File.Exists(cookiesFilePath)) cbCookies.SelectedIndex = 0;
            ckSubs.Checked = d.TryGetValue("Subs", out v) && v == "1"; ckThumb.Checked = !d.TryGetValue("Thumb", out v) || v == "1";
            ckCatch.Checked = d.TryGetValue("Catch", out v) && v == "1";
            ckExact.Checked = false;      // Exact is never remembered: it starts off every run
        }
        void SetIdx(ChoiceRow c, Dictionary<string, string> d, string key, int def)
        {
            string v; int i;
            c.SelectedIndex = d.TryGetValue(key, out v) && int.TryParse(v, out i) && i >= 0 && i <= 5 ? i : def;
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
                    "OutDir=" + txtOut.Text, "Mode=" + (tAudio.Checked ? "Audio" : "Video"), "Quality=" + cbQuality.SelectedIndex,
                    "Container=" + cbContainer.SelectedIndex, "AFmt=" + cbAFmt.SelectedIndex, "ABr=" + cbABr.SelectedIndex,
                    "Cookies=" + cbCookies.SelectedIndex, "CookieFile=" + cookiesFilePath,
                    "Subs=" + (ckSubs.Checked ? 1 : 0), "Thumb=" + (ckThumb.Checked ? 1 : 0), "Catch=" + (ckCatch.Checked ? 1 : 0), "Skin=" + skinName });
            }
            catch { }
        }

        // ---------------- options
        Opts GetOpts()
        {
            var o = new Opts();
            o.Video = tVideo.Checked; o.Height = Heights[Math.Max(0, cbQuality.SelectedIndex)];
            o.Container = new[] { "mp4", "mkv", "webm" }[Math.Max(0, cbContainer.SelectedIndex)];
            o.AFmt = new[] { "mp3", "m4a", "opus", "flac", "wav", "best" }[Math.Max(0, cbAFmt.SelectedIndex)];
            o.ABr = new[] { "0", "320K", "192K", "128K" }[Math.Max(0, cbABr.SelectedIndex)];
            o.Subs = ckSubs.Checked; o.Thumb = ckThumb.Checked; o.ClipExact = ckExact != null && ckExact.Checked;
            o.Cookies = new[] { "", "firefox", "edge", "chrome", "brave", "file" }[Math.Max(0, Math.Min(5, cbCookies.SelectedIndex))];
            o.CookieFile = cookiesFilePath;
            return o;
        }

        void PickMode(Tile t)
        {
            if (busy) return;
            tVideo.Checked = t == tVideo; tAudio.Checked = t == tAudio;
            ApplyMode(); OptionsChanged();
        }

        void ApplyMode()
        {
            bool v = tVideo.Checked;
            pVideo.Visible = v; pAudio.Visible = !v;
            ckSubs.Enabled = v && !busy; ckThumb.Enabled = !busy;
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
            // playlist checkbox changes what a "video in a playlist" link means -> re-check those links
            foreach (var it in items)
            {
                bool want = WantsPlaylist(it);
                if (want != it.IsPlaylist && it.Kind != "drive") { it.IsPlaylist = want; it.State = 0; it.Formats = null; it.Est = -1; SetItemStatus(it, "Checking...", ""); }
            }
            RecomputeAll(); FetchNext();
        }

        void ResetItem(Item it)
        {
            it.State = 0; it.Formats = null; it.Est = -1; it.Error = null; it.Probed = false; it.ProbeSize = -1; it.Archived = false; it.Done = false;
            if (it.Lvi != null) { it.Lvi.SubItems[1].Text = ""; it.Lvi.SubItems[2].Text = ""; }
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
            if (Regex.IsMatch(u, @"^https?://([\w-]+\.)?(youtube\.com|youtu\.be|youtube-nocookie\.com)/", RegexOptions.IgnoreCase)) return "youtube";
            if (Web.DriveId(u) != null) return "drive";
            return "other";
        }
        static string Badge(Item it)
        {
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
            var urls = CurrentUrls();
            var keep = new List<Item>(); int added = 0;
            if (busy) keep.AddRange(items); // while downloading, the list can only grow: the running queue keeps its rows
            foreach (var u in urls)
            {
                if (busy) { bool have = false; foreach (var x in items) if (x.Url == u) { have = true; break; } if (have) continue; }
                Item found = null;
                foreach (var it in items) if (it.Url == u) { found = it; break; }
                if (found == null)
                {
                    found = new Item(); found.Url = u; found.Title = u; found.IsPlaylist = IsPurePlaylist(u);
                    found.Kind = KindOf(u); if (found.Kind == "drive") { found.DriveId = Web.DriveId(u); found.IsPlaylist = false; found.Title = "Google Drive file"; }
                    found.Lvi = new ListViewItem(new[] { found.Kind == "drive" ? "Google Drive file" : u, "", "", "Checking...", "" }); found.Lvi.ImageKey = "ph"; found.Lvi.UseItemStyleForSubItems = false;
                    found.Lvi.Tag = found; added++;
                    if (Adult.Url(u)) { found.State = 3; found.Error = "adult"; found.Lvi.Text = Web.HostOf(u); }
                }
                keep.Add(found);
            }
            if (busy && added > 0) SetStatus("Added " + (added == 1 ? "1 link" : added + " links") + " - press Download when the current ones finish.", T.Ok);
            foreach (var it in items) if (!keep.Contains(it) && it == fetching && fetchRunner != null) { fetchRunner.Kill(); }
            items = keep;
            lv.BeginUpdate(); lv.Items.Clear(); foreach (var it in items) lv.Items.Add(it.Lvi); lv.EndUpdate();
            foreach (var x in items) if (x.State == 3 && x.Error == "adult" && x.Status != Adult.Short) { SetItemStatus(x, Adult.Short, "bad"); x.Lvi.ToolTipText = Adult.Why; }
            PlaceQueueLabels(); UpdateSummary(); FetchNext();
        }

        // ---------------- fetching info (one link at a time, in the background)
        void FetchNext()
        {
            if (fetchRunner != null || driveFetching) return;
            Item next = null;
            foreach (var it in items) if (it.State == 0 && (ytdlp != null || it.Kind == "drive")) { next = it; break; }
            if (next == null) { UpdateSummary(); return; }
            fetching = next; next.State = 1; SetItemStatus(next, "Checking...", "");
            if (next.Kind == "drive") { FetchDrive(next); return; }
            fetchStage = next.IsPlaylist ? 1 : 2;
            RunFetch(next, next.Url);
        }

        void RunFetch(Item it, string url)
        {
            var o = GetOpts();
            var a = new List<string> { "--no-warnings", "--ignore-config", "--encoding", "utf-8" };
            if (it.UseCookies) CookieArgs(o, a);
            if (fetchStage == 1) { a.Add("--yes-playlist"); a.Add("--flat-playlist"); a.Add("-J"); }
            else
            {
                a.Add("--no-playlist");
                a.Add("--print"); a.Add("%(.{id,title,channel,uploader,duration,extractor_key,playlist_count,thumbnail,webpage_url_domain,age_limit})j");
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
                    if (Adult.Extractor(Str(d, "extractor_key", "")) || Adult.Host(Str(d, "webpage_url_domain", "")) || Adult.Url(Str(d, "webpage_url", ""))) throw new Exception(Adult.Why);
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
                {
                    string xk = Str(md, "extractor_key", ""); double age = Num(md, "age_limit");
                    // known adult sites by name or address; a link on any unknown site (generic extractor) that the page itself marks 18+
                    if (Adult.Extractor(xk) || Adult.Host(Str(md, "webpage_url_domain", "")) || (age >= 18 && (xk == "Generic" || xk == ""))) throw new Exception(Adult.Why);
                }
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
                if (ex.Message == Adult.Why)
                {
                    it.State = 3; it.Error = "adult"; it.Formats = null;
                    SetItemStatus(it, Adult.Short, "bad", ""); it.Lvi.ToolTipText = Adult.Why; it.Lvi.SubItems[1].Text = ""; it.Lvi.SubItems[2].Text = "";
                    Log("Blocked (adult site): " + it.Url);
                    UpdateSummary(); FetchNext(); return;
                }
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
            if (Regex.IsMatch(s, "Unsupported URL|No video formats found|no video in this post|There is no video|No video could be found", RegexOptions.IgnoreCase)) return "";
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
            if (it.Kind == "drive")
            {
                it.Est = it.ProbeSize;
                string ext = Path.GetExtension(it.FileName ?? "").TrimStart('.').ToUpperInvariant();
                if (o.Video) { desc = "original " + (ext != "" ? ext : "file"); it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : T.Bytes(it.Est); }
                else { desc = "original \u2192 " + (o.AFmt == "best" ? "audio" : o.AFmt.ToUpperInvariant()); it.Lvi.SubItems[2].Text = it.Est < 0 ? "?" : "\u2264 " + T.Bytes(it.Est); }
                it.Lvi.ToolTipText = (it.FileName ?? "") + "\nGoogle Drive original file" + (it.Est >= 0 ? ", " + T.Bytes(it.Est) : "") +
                    (o.Video ? "" : "\nAudio is extracted after download, so the final file is smaller.");
                if (!InRun(it) && !it.Done) SetItemStatus(it, desc + (it.UseCookies ? " \u00B7 signed in" : ""), "", it.DonePath != null ? "show" : "");
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
            if (!InRun(it) && !it.Done)
            {
                bool allHave = it.IsPlaylist && it.Count > 0 && it.PlHave >= it.Count && !it.ForceAgain;
                if (it.Archived && !it.HasClip) { it.Lvi.SubItems[2].Text = "0 B"; SetItemStatus(it, "Already downloaded", "", it.DonePath != null && File.Exists(it.DonePath) ? "show" : "again"); }
                else if (allHave) { it.Lvi.SubItems[2].Text = "0 B"; SetItemStatus(it, "All " + it.Count + " already downloaded", "", "again"); }
                else SetItemStatus(it, (desc == "" ? "Ready" : desc) + extra + (it.UseCookies ? " \u00B7 signed in" : "") +
                    (InPlaylist(it) && !it.IsPlaylist ? " \u00B7 in a playlist" : ""), "", it.DonePath != null && File.Exists(it.DonePath) ? "show" : "");
            }
            it.Lvi.ToolTipText = Regex.Split(it.Lvi.ToolTipText ?? "", "\n\n(?:Right-click|Select it to trim|Use the list icon|Subtitles:)")[0] +
                (it.IsPlaylist && it.PlHave > 0 ? "\n" + it.PlHave + " of " + it.Count + " videos were downloaded before and will be skipped." : "") +
                (InPlaylist(it) ? (it.IsPlaylist ? "\n\nUse the list icon on the right to download only this video." : "\n\nUse the list icon on the right to download the whole playlist.") : "");
            if (it.Est < 0 && it.Kind == "other" && !it.Probed && !it.IsPlaylist) ProbeDirect(it);
        }

        static string Clock(double sec) { return sec <= 0 ? "0:00" : T.Dur(sec); }
        static string Stamp(double sec)     // file-name safe: 1m05s, 1h02m03s
        {
            var t = TimeSpan.FromSeconds(Math.Round(Math.Max(0, sec)));
            return t.TotalHours >= 1 ? string.Format("{0}h{1:00}m{2:00}s", (int)t.TotalHours, t.Minutes, t.Seconds) : string.Format("{0}m{1:00}s", t.Minutes, t.Seconds);
        }

        void RecomputeAll() { foreach (var it in items) Recompute(it); UpdateSummary(); lv.Invalidate(); }

        void UpdateSummary()
        {
            SyncClip();
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
                case "firefox": return "Use " + BrowserNames[BestBrowser()] + " sign-in";
                case "retry": return "Retry";
                case "show": return "Show in folder";
                case "again": return "Download again";
            }
            return null;
        }

        Rectangle CellRect(ListViewItem lvi, int col)
        {
            var b = lvi.Bounds; int x = b.X;
            for (int i = 0; i < col; i++) x += lv.Columns[i].Width;
            return new Rectangle(x, b.Y, lv.Columns[col].Width, b.Height);
        }
        // shortens text with "..." so it fits the width (doesn't rely on the renderer clipping)
        static string FitText(string t, Font f, int w)
        {
            if (string.IsNullOrEmpty(t) || w <= 0) return w <= 0 ? "" : t;
            var fl = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            if (TextRenderer.MeasureText(t, f, Size.Empty, fl).Width <= w) return t;
            int lo = 0, hi = t.Length;
            while (lo < hi) { int mid = (lo + hi + 1) / 2; if (TextRenderer.MeasureText(t.Substring(0, mid).TrimEnd() + "\u2026", f, Size.Empty, fl).Width <= w) lo = mid; else hi = mid - 1; }
            return lo == 0 ? "" : t.Substring(0, lo).TrimEnd() + "\u2026";
        }
        Rectangle PillRect(Item it, Rectangle cell)
        {
            if (it.Action == "show" || it.Action == "again") return Rectangle.Empty;   // these are row icons now
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
        void ListClick(Point p)
        {
            Item ii; string icon = IconAt(p, out ii);
            if (icon != null) { RowIcon(ii, icon, p); return; }
            var it = PillAt(p); if (it != null) DoAction(it);
        }

        // ---------------- row icons (right end of each row): clip, playlist choice, download again, show in folder, remove
        static int IconColW { get { return T.P(8) + 5 * T.P(28); } }
        ToolTip iconTip; Item hotIconItem; string hotIcon; bool iconMenuOpen;
        // icons that apply to this row, left to right. "Show in folder" is always visible; the rest appear on hover or when the row is selected.
        List<string> RowIcons(Item it)
        {
            var l = new List<string>();
            if (it == null) return l;
            bool free = !busy;
            if (free && InPlaylist(it)) l.Add("list");
            if (free && !it.IsPlaylist && it.Kind != "drive" && it.State == 2) l.Add("cut");
            if (free && !it.ForceAgain && (it.Archived || it.Done || (it.IsPlaylist && it.PlHave > 0))) l.Add("refresh");
            if (it.DonePath != null && File.Exists(it.DonePath)) l.Add("folder");
            if (free) l.Add("clear");
            return l;
        }
        static string IconTip(Item it, string icon)
        {
            switch (icon)
            {
                case "list": return it.IsPlaylist ? "Playlist: download all, or only this video" : "In a playlist: download only this video, or the whole list";
                case "cut": return it.HasClip ? "Change the clip (" + Stamp(it.ClipStart) + " - " + Stamp(it.ClipEnd) + ")" : "Download only a part (clip)";
                case "refresh": return "Download again";
                case "folder": return "Show in folder";
                case "clear": return "Remove from list";
            }
            return "";
        }
        bool IconsShown(ListViewItem lvi, string icon) { return icon == "folder" || lvi.Selected || lvi == hotRow; }
        List<KeyValuePair<string, Rectangle>> IconRects(Item it, Rectangle cell)
        {
            var res = new List<KeyValuePair<string, Rectangle>>();
            var icons = RowIcons(it); int sz = T.P(26), gap = T.P(2);
            int x = cell.Right - T.P(6) - icons.Count * (sz + gap) + gap;
            foreach (var n in icons) { res.Add(new KeyValuePair<string, Rectangle>(n, new Rectangle(x, cell.Y + (cell.Height - sz) / 2, sz, sz))); x += sz + gap; }
            return res;
        }
        string IconAt(Point p, out Item item)
        {
            item = null;
            if (lv.Columns.Count < 5) return null;
            var hit = lv.GetItemAt(T.P(4), p.Y); if (hit == null) return null;
            var it = hit.Tag as Item; if (it == null) return null;
            foreach (var kv in IconRects(it, CellRect(hit, 4)))
                if (kv.Value.Contains(p) && IconsShown(hit, kv.Key)) { item = it; return kv.Key; }
            return null;
        }
        void SetHotIcon(Item it, string icon)
        {
            if (iconMenuOpen) { it = null; icon = null; }
            if (it == hotIconItem && icon == hotIcon) return;
            if (hotIconItem != null && hotIconItem.Lvi.ListView == lv) lv.Invalidate(hotIconItem.Lvi.Bounds);
            hotIconItem = it; hotIcon = icon;
            if (iconTip == null) { iconTip = new ToolTip(); iconTip.ShowAlways = true; }
            if (it == null) { iconTip.Hide(lv); lv.ShowItemToolTips = true; return; }
            lv.Invalidate(it.Lvi.Bounds);
            lv.ShowItemToolTips = false;   // the row's own tooltip would cover the icon's
            foreach (var kv in IconRects(it, CellRect(it.Lvi, 4)))
                if (kv.Key == icon) { iconTip.Show(IconTip(it, icon), lv, kv.Value.Left, kv.Value.Bottom + T.P(4), 4000); break; }
        }
        void RowIcon(Item it, string icon, Point p)
        {
            SetHotIcon(null, null);
            switch (icon)
            {
                case "cut": it.Lvi.Selected = true; FocusClip(it); break;
                case "folder": ShowInFolder(it.DonePath); break;
                case "refresh": it.Action = "again"; DoAction(it); break;
                case "clear": RemoveLink(it); break;
                case "list":
                    {
                        it.Lvi.Selected = true;
                        var m = new ContextMenuStrip(); m.Font = T.Body; m.Renderer = new SkinRenderer();
                        bool whole = it.IsPlaylist;
                        var a = new ToolStripMenuItem("Only this video", null, (s, e) => SetPlaylist(it, false)); a.Checked = !whole;
                        var b = new ToolStripMenuItem(IsMix(it.Url) ? "The whole YouTube Mix (auto-generated, can be long)" : "The whole playlist", null, (s, e) => SetPlaylist(it, true)); b.Checked = whole;
                        m.Items.Add(a); m.Items.Add(b);
                        m.Closed += (s, e) => { iconMenuOpen = false; BeginInvoke((MethodInvoker)(() => m.Dispose())); };
                        iconMenuOpen = true;
                        foreach (var kv in IconRects(it, CellRect(it.Lvi, 4))) if (kv.Key == "list") { m.Show(lv, new Point(kv.Value.Left, kv.Value.Bottom + T.P(2))); break; }
                    }
                    break;
            }
        }
        void DrawIcons(Graphics g, Item it, ListViewItem lvi, Rectangle cell)
        {
            var k = T.Cur;
            foreach (var kv in IconRects(it, cell))
            {
                if (!IconsShown(lvi, kv.Key)) continue;
                var r = kv.Value; bool hot = it == hotIconItem && kv.Key == hotIcon;
                bool on = (kv.Key == "cut" && it.HasClip) || (kv.Key == "list" && it.IsPlaylist);
                if (hot || on)
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = T.Round(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), T.P(6)))
                    using (var b = new SolidBrush(hot ? T.Solid(k.Soft) : T.Solid(k.AccentTint))) g.FillPath(b, p);
                }
                Color c = hot || on || kv.Key == "folder" ? k.Accent : k.Muted;
                if (kv.Key == "clear" && hot) c = k.Bad;
                int pad = T.P(5);
                Icons.Draw(g, kv.Key, new RectangleF(r.X + pad, r.Y + pad, r.Width - 2 * pad, r.Width - 2 * pad), c);
            }
        }

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
            else if (e.ColumnIndex == 4) { if (it != null) DrawIcons(g, it, e.Item, r); }
            else if (e.ColumnIndex == 3)
            {
                var pr = it != null ? PillRect(it, r) : Rectangle.Empty;
                int right = pr.IsEmpty ? r.Right - pad : pr.Left - T.P(6);
                int sw = Math.Max(0, right - r.X - pad);
                TextRenderer.DrawText(g, FitText(e.SubItem.Text, lv.Font, sw), lv.Font, new Rectangle(r.X + pad, r.Y, sw, r.Height), e.SubItem.ForeColor, flags | TextFormatFlags.EndEllipsis);
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
                    {
                        int bb = BestBrowser(); string bn = BrowserNames[bb];
                        cbCookies.SelectedIndex = bb;    // OptionsChanged re-checks the failed links signed in
                        SetStatus("Sign-in set to " + bn + " - be signed in to the site in " + bn + ". Checking again...", T.Muted);
                    }
                    break;
                case "update": if (!busy) { foreach (var x in items) if (x.Action == "update") retryAfterUpdate = true; UpdateYtDlp(); } break;
                case "retry":
                    if (busy) return;
                    if (it.State == 3) { ResetItem(it); UpdateSummary(); FetchNext(); }
                    else StartDownloads(new List<Item> { it });
                    break;
                case "again":
                    it.ForceAgain = true; it.Archived = false; it.Done = false; Recompute(it); UpdateSummary();
                    SetStatus("This one will be downloaded again.", T.Muted); break;
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
                if (!it.IsPlaylist && it.Kind != "drive" && it.State == 2)
                {
                    rowMenu.Items.Add(it.HasClip ? "Change the clip" : "Download only a part (clip)", null, (a, b) => FocusClip(it));
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
            var a = new List<string> { "--newline", "--ignore-config", "--encoding", "utf-8", "-N", "4" };
            if (ffmpeg != null) { a.Add("--ffmpeg-location"); a.Add(Path.GetDirectoryName(ffmpeg)); }
            string outDir = txtOut.Text.Trim().TrimEnd('\\', '/');
            string clipTag = it.HasClip && !it.IsPlaylist ? " [clip " + Stamp(it.ClipStart) + "-" + Stamp(it.ClipEnd) + "]" : "";
            // A clip is never fetched as a "section": that goes through ffmpeg on one connection and YouTube serves it slowly.
            // The whole video comes down at full speed (parallel connections) into a temporary folder and is cut on this PC afterwards.
            it.FastClip = clipTag != "";
            if (it.FastClip) it.TmpDir = Path.Combine(outDir, ".dawnloader-tmp-" + Regex.Replace(it.Id != "" ? it.Id : Math.Abs(it.Url.GetHashCode()).ToString(), @"[^\w-]", "_"));
            a.Add("-P"); a.Add(it.FastClip ? it.TmpDir : outDir);
            if (it.IsPlaylist) { a.Add("--yes-playlist"); a.Add("-o"); a.Add("%(playlist_title|Single videos).80B/%(playlist_index&{} - |)s%(title).90B [%(id)s].%(ext)s"); }
            else { a.Add("--no-playlist"); a.Add("-o"); a.Add(it.Kind == "youtube" ? "%(title).150B [%(id)s]" + clipTag + ".%(ext)s" : (it.Extractor == "" || it.Extractor == "Generic") ? "%(title).150B" + clipTag + ".%(ext)s" : "%(title).90B [%(id)s]" + clipTag + ".%(ext)s"); }
            if (clipTag != "") { }
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
                if (o.Subs && !it.NoSubs && clipTag != "") a.AddRange(new[] { "--write-subs", "--write-auto-subs", "--sub-langs", "en.*,en", "--convert-subs", "srt" }); // clips: subtitles are cut to the clip and embedded afterwards (SubsForClip)
                else if (o.Subs && !it.NoSubs) a.AddRange(new[] { "--write-subs", "--write-auto-subs", "--sub-langs", "en.*,en", "--embed-subs", "--compat-options", "no-keep-subs" });
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

        // true while this item is part of the download that is running now (links added meanwhile are not)
        bool InRun(Item it) { return busy && (queue == null || queue.Contains(it)); }

        void SetBusy(bool b)
        {
            busy = b;
            btnDownload.Visible = !b; btnCancel.Visible = b; btnUpdate.Enabled = !b;
            txtLinks.ReadOnly = b; btnClear.Enabled = !b; // Paste stays on: links can be added (not removed) while downloading
            tVideo.Enabled = !b; tAudio.Enabled = !b;
            foreach (Control c in new Control[] { cbQuality, cbContainer, cbAFmt, cbABr, ckSubs, ckThumb, cbCookies, txtOut, btnBrowse }) c.Enabled = !b;
            if (!b) ApplyMode();
            RefreshClip();
            lv.Invalidate();
        }

        void StartDownloads(List<Item> only)
        {
            debounce.Stop(); if (only == null) SyncItems();
            bool needYt = false; foreach (var it in only ?? items) if (it.Kind != "drive") needYt = true;
            if (needYt && (ytdlp == null || ffmpeg == null)) { AskInstall("Downloading needs a few free helper tools that aren't installed yet."); return; }
            if (items.Count == 0) { SetStatus("Paste a link first.", T.Warn); txtLinks.Focus(); return; }
            string outDir = txtOut.Text.Trim();
            try { Directory.CreateDirectory(outDir); }
            catch (Exception ex) { MessageBox.Show(this, "Can't use that folder:\n" + ex.Message, "Dawnloader"); return; }
            queue = new List<Item>();
            foreach (var it in only ?? items)
            {
                if (it.State == 3 && it.Error == "adult") continue;
                if (it.State == 3) { string was = Regex.Replace(it.Status ?? "", "^Skipped: ", ""); SetItemStatus(it, "Skipped: " + (was != "" ? was : Friendly(it.Error ?? "", 22)), "bad", it.Action); continue; }
                if (it.State < 2) { SetItemStatus(it, "Skipped: still checking", "bad"); continue; }
                if (only == null && it.Archived && !it.HasClip) { SetItemStatus(it, "Skipped - already downloaded", "", it.DonePath != null ? "show" : "again"); continue; }
                if (only == null && it.IsPlaylist && it.Count > 0 && it.PlHave >= it.Count && !it.ForceAgain) { SetItemStatus(it, "Skipped - all already downloaded", "", "again"); continue; }
                it.Done = false; it.NoSubs = false; SetItemStatus(it, "Waiting", ""); queue.Add(it);
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
            ffLines = 0;
            if (Adult.Url(it.Url) || Adult.Extractor(it.Extractor))
            {
                it.State = 3; it.Error = "adult"; SetItemStatus(it, Adult.Short, "bad", ""); Log("Blocked (adult site): " + it.Url);
                dlIndex++; NextDownload(); return;
            }
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
            // clips are fetched by ffmpeg: show its progress and speed instead of logging every line
            Match fp = Regex.Match(line, @"^(?:frame=|size=).*?time=(\d+):(\d+):(\d+(?:\.\d+)?).*?speed=\s*([\d.]+)x");
            if (fp.Success && it.HasClip && !it.FastClip)
            {
                double t = int.Parse(fp.Groups[1].Value) * 3600 + int.Parse(fp.Groups[2].Value) * 60 + double.Parse(fp.Groups[3].Value, CultureInfo.InvariantCulture);
                double len = Math.Max(1, (it.Duration > 0 ? Math.Min(it.ClipEnd, it.Duration) : it.ClipEnd) - it.ClipStart);
                double pc = Math.Min(1, t / len); string speed = fp.Groups[4].Value;
                SetItemStatus(it, "Cutting clip " + (int)(pc * 100) + "%", "run");
                bar.Value = (dlIndex + pc) / queue.Count;
                SetStatus("Downloading " + (dlIndex + 1) + " of " + queue.Count + ": " + it.Title + "  \u00B7  " + speed + "\u00D7 real time", T.Text);
                if (ffLines++ % 20 == 0) Log(line);
                return;
            }
            Match m = Regex.Match(line, @"^\[download\]\s+([\d.]+)%(.*)");
            if (m.Success)
            {
                double pct = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                string rest = m.Groups[2].Value;
                Match sp = Regex.Match(rest, @"at\s+(\S+)\s+ETA\s+(\S+)");
                string extra = sp.Success ? "  \u00B7  " + sp.Groups[1].Value + "  \u00B7  " + sp.Groups[2].Value + " left" : "";
                string pl = plCount > 0 ? " (" + plIdx + "/" + plCount + ")" : "";
                string rowEta = sp.Success && Regex.IsMatch(sp.Groups[2].Value, @"\d") ? "  \u00B7  " + sp.Groups[2].Value + " left" : "";
                SetItemStatus(it, (it.FastClip ? "Full video " : "Downloading ") + (int)pct + "%" + pl + rowEta, "run");
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
            if (code == 0 && it.FastClip && lastDest != null && ffmpeg != null)
            {
                string full = Path.IsPathRooted(lastDest) ? lastDest : Path.Combine(it.TmpDir, lastDest);
                var o = GetOpts(); double cs = it.ClipStart, ce = it.Duration > 0 ? Math.Min(it.ClipEnd, it.Duration) : it.ClipEnd;
                string outDir = txtOut.Text.Trim().TrimEnd('\\', '/'), tmp = it.TmpDir; bool wantSubs = o.Video && o.Subs && !it.NoSubs;
                cutting = true; cutCancel = false;
                SetItemStatus(it, o.ClipExact && o.Video ? "Cutting the clip (exact, a moment)..." : "Cutting the clip...", "run");
                SetStatus("Downloading " + (dlIndex + 1) + " of " + queue.Count + ": " + it.Title + "  \u00B7  cutting your clip", T.Text);
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    string final = null, res = CutClip(it, full, outDir, cs, ce, o.Video, o.ClipExact, out final), subNote = null;
                    if (res == null && wantSubs) subNote = SubsForClip(final, tmp, cs, ce);
                    if (res == null && !cutCancel) { try { Directory.Delete(tmp, true); } catch { } }
                    try { BeginInvoke((MethodInvoker)(() =>
                    {
                        cutting = false;
                        if (cutCancel || cancelAll) { SetItemStatus(it, "Cancelled", "bad", "retry"); FinishAll(); return; }
                        if (subNote != null) Log("Subtitles for the clip: " + subNote);
                        if (res != null) { lastErr = res; OnDlExitDone(it, 1); return; }
                        lastDest = final; OnDlExitDone(it, 0);
                    })); } catch { }
                });
                return;
            }
            OnDlExitDone(it, code);
        }

        // yt-dlp downloads subtitles for the WHOLE video and embeds them without shifting the timing, so in a clip they show at the wrong
        // moments. Cut each .srt to the clip, embed the result, and remove the loose files. Returns null on success, else a short reason.
        string SubsForClip(string video, string subsDir, double start, double end)
        {
            try
            {
                if (!File.Exists(video)) return "video file not found";
                string dir = Path.GetDirectoryName(video), stem = Path.GetFileNameWithoutExtension(video), ext = Path.GetExtension(video).ToLowerInvariant();
                var subs = new List<string>();
                foreach (string f in Directory.GetFiles(subsDir ?? dir, "*.srt"))
                {
                    string n = Path.GetFileName(f);
                    if (n.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase)) subs.Add(f);
                }
                if (subs.Count == 0) return "no English subtitles exist for this video";
                subs.Sort(); if (subs.Count > 3) subs.RemoveRange(3, subs.Count - 3);
                var cut = new List<string>();
                foreach (string f in subs)
                {
                    string o = f + ".cut.srt";
                    if (CutSrt(f, o, start, end)) cut.Add(o);
                }
                if (cut.Count == 0) { foreach (string f in subs) TryDel(f); return "none of the subtitles fall inside the clip"; }
                string tmp = Path.Combine(dir, stem + ".subs-tmp" + ext);
                string codec = ext == ".mp4" || ext == ".m4v" || ext == ".mov" ? "mov_text" : ext == ".webm" ? "webvtt" : "srt";
                var a = new List<string> { "-y", "-v", "error", "-i", video };
                foreach (string c in cut) { a.Add("-i"); a.Add(c); }
                a.AddRange(new[] { "-map", "0" });
                for (int i = 0; i < cut.Count; i++) { a.Add("-map"); a.Add((i + 1) + ":0"); }
                a.AddRange(new[] { "-c", "copy", "-c:s", codec });
                for (int i = 0; i < cut.Count; i++) { a.Add("-metadata:s:s:" + i); a.Add("language=eng"); }
                a.Add(tmp);
                int rc = RunFf(a.ToArray(), null);
                if (rc == 0 && File.Exists(tmp) && new FileInfo(tmp).Length > 0)
                {
                    File.Delete(video); File.Move(tmp, video);
                    foreach (string f in subs) TryDel(f); foreach (string c in cut) TryDel(c);
                    return null;
                }
                TryDel(tmp); foreach (string c in cut) TryDel(c);
                return "could not embed them (the .srt file(s) were kept next to the video)";
            }
            catch (Exception ex) { return ex.Message; }
        }
        // keeps only the subtitle lines that overlap [start,end] and moves them so the clip begins at 0:00
        static bool CutSrt(string src, string dst, double start, double end)
        {
            string text = File.ReadAllText(src, Encoding.UTF8).Replace("\r\n", "\n").Replace('\r', '\n');
            var sb = new StringBuilder(); int n = 0;
            foreach (string block in Regex.Split(text.Trim('\uFEFF', '\n'), "\n\n+"))
            {
                Match m = Regex.Match(block, @"(\d+):(\d+):(\d+)[,.](\d+)\s*-->\s*(\d+):(\d+):(\d+)[,.](\d+)[^\n]*\n([\s\S]+)$");
                if (!m.Success) continue;
                double a = SrtSec(m, 1) - start, b = SrtSec(m, 5) - start, len = end - start;
                if (b <= 0 || a >= len) continue;
                if (a < 0) a = 0; if (b > len) b = len;
                sb.Append(++n).Append('\n').Append(SrtStamp(a)).Append(" --> ").Append(SrtStamp(b)).Append('\n').Append(m.Groups[9].Value.TrimEnd()).Append("\n\n");
            }
            if (n == 0) return false;
            File.WriteAllText(dst, sb.ToString(), new UTF8Encoding(false));
            return true;
        }
        static double SrtSec(Match m, int g)
        {
            string ms = m.Groups[g + 3].Value.PadRight(3, '0').Substring(0, 3);
            return int.Parse(m.Groups[g].Value) * 3600 + int.Parse(m.Groups[g + 1].Value) * 60 + int.Parse(m.Groups[g + 2].Value) + int.Parse(ms) / 1000.0;
        }
        static string SrtStamp(double t)
        {
            long ms = (long)Math.Round(t * 1000); long h = ms / 3600000; ms -= h * 3600000; long mi = ms / 60000; ms -= mi * 60000; long se = ms / 1000; ms -= se * 1000;
            return h.ToString("00") + ":" + mi.ToString("00") + ":" + se.ToString("00") + "," + ms.ToString("000");
        }
        volatile bool cutting, cutCancel; volatile Process cutProc;
        // stops a process and everything it started (a launcher "shim" such as chocolatey's ffmpeg.exe starts the real ffmpeg as a child)
        static void KillTree(Process p)
        {
            try
            {
                if (p == null || p.HasExited) return;
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var k = new ProcessStartInfo("taskkill", "/PID " + p.Id + " /T /F") { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(k).WaitForExit(5000);
                }
                else p.Kill();
            }
            catch { }
        }
        // a cancelled or failed clip keeps its downloaded video so Retry can carry on; folders older than a day are removed at startup
        void CleanOldTemp()
        {
            string od = txtOut.Text.Trim().TrimEnd('\\', '/');
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (!Directory.Exists(od)) return;
                    foreach (string d in Directory.GetDirectories(od, ".dawnloader-tmp-*"))
                        if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(d) > TimeSpan.FromDays(1)) { try { Directory.Delete(d, true); } catch { } }
                }
                catch { }
            });
        }
        // cuts [start,end] out of the downloaded full file into outDir. Exact = re-encode (frame-accurate); otherwise copy (instant, keyframe start).
        string CutClip(Item it, string full, string outDir, double start, double end, bool video, bool exact, out string final)
        {
            final = null;
            try
            {
                if (!File.Exists(full)) return "the downloaded video wasn't found";
                Directory.CreateDirectory(outDir);
                final = Path.Combine(outDir, Path.GetFileName(full));
                string ext = Path.GetExtension(full).ToLowerInvariant();
                var a = new List<string> { "-y", "-v", "error", "-ss", start.ToString("0.###", CultureInfo.InvariantCulture) };
                if (end > start) { a.Add("-to"); a.Add(end.ToString("0.###", CultureInfo.InvariantCulture)); }
                a.Add("-i"); a.Add(full); a.AddRange(new[] { "-map_chapters", "-1" });
                if (video && exact && ext != ".webm") a.AddRange(new[] { "-map", "0:v:0", "-map", "0:a?", "-c:v", "libx264", "-preset", "veryfast", "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k" });
                else if (video) a.AddRange(new[] { "-map", "0:v", "-map", "0:a?", "-c", "copy", "-avoid_negative_ts", "make_zero" });
                else a.AddRange(new[] { "-map", "0", "-c", "copy" });
                a.Add(final);
                Action<double> onTime = null;
                if (video && exact && ext != ".webm")
                {
                    a.Insert(0, "pipe:1"); a.Insert(0, "-progress"); a.Insert(2, "-nostats");
                    double len = Math.Max(1, end - start); var sw = Stopwatch.StartNew(); long lastUi = 0;
                    onTime = t =>
                    {
                        if (sw.ElapsedMilliseconds - lastUi < 500) return; lastUi = sw.ElapsedMilliseconds;
                        double pc = Math.Max(0.01, Math.Min(0.99, t / len)), left = sw.Elapsed.TotalSeconds * (1 - pc) / pc;
                        try { BeginInvoke((MethodInvoker)(() => { if (cutting) { SetItemStatus(it, "Cutting " + (int)(pc * 100) + "%  \u00B7  " + Eta(left) + " left", "run"); bar.Value = (dlIndex + pc) / queue.Count; } })); } catch { }
                    };
                }
                int rc = RunFf(a.ToArray(), onTime);
                if (cutCancel) { TryDel(final); return null; }
                if (rc != 0 || !File.Exists(final) || new FileInfo(final).Length == 0) { TryDel(final); return "ffmpeg couldn't cut the clip (see Details)"; }
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }
        static void TryDel(string f) { try { File.Delete(f); } catch { } }
        static string Eta(double sec) { int s = (int)Math.Ceiling(sec); return s < 60 ? s + "s" : (s / 60) + ":" + (s % 60).ToString("00"); }
        int RunFf(string[] args, Action<double> onTime)
        {
            var psi = new ProcessStartInfo(ffmpeg) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            var sb = new StringBuilder(); foreach (string x in args) sb.Append(Runner.Quote(x)).Append(' ');
            psi.Arguments = sb.ToString().Trim();
            using (var p = Process.Start(psi))
            {
                cutProc = p;
                var errSb = new StringBuilder();
                p.ErrorDataReceived += (se, ev) => { if (ev.Data != null) lock (errSb) errSb.AppendLine(ev.Data); };
                p.BeginErrorReadLine();
                string ln;
                while ((ln = p.StandardOutput.ReadLine()) != null)
                {
                    if (onTime == null) continue;
                    var tm = Regex.Match(ln, @"^out_time_(?:us|ms)=(\d+)");
                    if (tm.Success) onTime(double.Parse(tm.Groups[1].Value, CultureInfo.InvariantCulture) / 1e6);
                }
                p.WaitForExit(); cutProc = null; string err; lock (errSb) err = errSb.ToString();
                if (p.ExitCode != 0 && err.Length > 0) { string e2 = err.Trim(); try { BeginInvoke((MethodInvoker)(() => Log("ffmpeg: " + e2))); } catch { } }
                return p.ExitCode;
            }
        }

        void OnDlExitDone(Item it, int code)
        {
            if (code == 0)
            {
                it.Done = true;
                string full = lastDest == null ? null : (Path.IsPathRooted(lastDest) ? lastDest : Path.Combine(txtOut.Text.Trim(), lastDest));
                if (full != null && File.Exists(full)) { it.DonePath = full; if (it.ArchiveKey != "" && !it.HasClip) AddHistory(it.ArchiveKey, full); }
                string msg = "\u2713 Done";
                if (it.NoSubs && GetOpts().Subs)
                {
                    msg += " \u00B7 no subtitles (site limit)";
                    it.Lvi.ToolTipText = Regex.Split(it.Lvi.ToolTipText ?? "", "\n\nSubtitles:")[0] + "\n\nSubtitles: the site limited subtitle requests, so this video was saved without them." +
                        (GetOpts().Cookies == "" ? "\nTo get them, set 'Sign in with' to your browser (signed in to the site there), then click the \u21BB icon on this row." : "\nTo get them, wait about an hour, then click the \u21BB icon on this row.");
                }
                if (it.IsPlaylist && plSkipped > 0) msg += " \u00B7 " + plSkipped + " already had";
                else if (!it.IsPlaylist && plSkipped > 0) msg = "\u2713 Already downloaded before";
                SetItemStatus(it, msg, "ok", it.DonePath != null ? "show" : "");
                if (it.ArchiveKey != "" && !it.HasClip) { it.Archived = true; archiveCache = null; }
                it.ForceAgain = false;
                dlIndex++; NextDownload(); return;
            }
            if (!it.NoSubs && GetOpts().Video && GetOpts().Subs && lastErr != null && Regex.IsMatch(lastErr, "subtitles?", RegexOptions.IgnoreCase) && Regex.IsMatch(lastErr, "HTTP Error|Unable to download", RegexOptions.IgnoreCase))
            {
                it.NoSubs = true; attempt = 0;
                Log("--- The site refused the subtitles (" + Friendly(lastErr, 40) + "). Downloading the video without them ---");
                StartItem(it, null); return;
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
            else if (failed == 0 && queue.Count == items.Count) { bar.Value = 1; SetStatus("\u2713 All done - " + ok + (ok == 1 ? " download" : " downloads") + " saved. Click the folder icon on a row to find it.", T.Ok); }
            else
            {
                bar.Value = 1;
                int skipped = 0, waiting = 0;
                foreach (var x in items) if (!queue.Contains(x)) { if ((x.Status ?? "").StartsWith("Skipped") || x.Error == "adult") skipped++; else waiting++; }
                string msg = ok + " done" + (failed > 0 ? ", " + failed + " failed" : "") + (skipped > 0 ? ", " + skipped + " skipped" : "") + (waiting > 0 ? ", " + waiting + " waiting - press Download" : "") + ".";
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
            if (cutting) { cutCancel = true; SetStatus("Cancelling...", T.Warn); KillTree(cutProc); return; }
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
            if (!o.Video && ffmpeg == null) { DriveFinished(it, false, "ffmpeg is needed to extract audio", "other"); return; }
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
                        if (error == null && !o.Video) ConvertAudio(it, saved, o);
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

        // ---------------- clip (download only a part)
        static bool Clippable(Item it) { return it != null && !it.IsPlaylist && it.Kind != "drive" && it.State == 2; }

        // which item the clip box shows: the selected row, else the one shown before, else the only link
        void SyncClip()
        {
            if (clipBox == null || lv == null) return;
            Item want = lv.SelectedItems.Count > 0 ? lv.SelectedItems[0].Tag as Item : null;
            if (want == null && clipItem != null && items.Contains(clipItem)) want = clipItem;
            if (want == null && items.Count == 1) want = items[0];
            clipItem = want; RefreshClip();
        }

        void RefreshClip()
        {
            if (clipBox == null) return;
            var it = clipItem; bool ok = Clippable(it) && !busy;
            clipSync = true;
            string help = "Drag the handles, or type times like 1:30.";
            if (it == null)
            {
                lblClipItem.Text = items.Count == 0 ? "Add a link, then trim it here to download only a part." : "Click a video in the list below to trim it.";
                lblClipItem.ForeColor = T.Muted;
                clipBar.Set(0, 0, 0); tClipS.Text = ""; tClipE.Text = ""; lblClipRes.Text = ""; help = "";
            }
            else
            {
                lblClipItem.Text = it.Title; lblClipItem.ForeColor = T.Text; tip.SetToolTip(lblClipItem, it.Title);
                if (!Clippable(it))
                {
                    clipBar.Set(0, 0, 0); tClipS.Text = ""; tClipE.Text = "";
                    lblClipRes.Text = it.IsPlaylist ? "Playlists can't be trimmed." : it.Kind == "drive" ? "Google Drive files can't be trimmed." : it.State == 3 ? "This link didn't load." : "Checking the link...";
                    lblClipRes.ForeColor = T.Muted;
                    help = it.IsPlaylist ? "Pick a single video to trim it." : "";
                }
                else
                {
                    double dur = it.Duration, a = it.HasClip ? it.ClipStart : 0, z = it.HasClip ? it.ClipEnd : dur;
                    clipBar.Set(dur, a, z > 0 ? z : 0);
                    if (!tClipS.Focused) tClipS.Text = Clock(a);
                    if (!tClipE.Focused) tClipE.Text = z > 0 ? Clock(z) : "";
                    ClipResult();
                    if (dur <= 0) help = "Length unknown - type the start and end.";
                    else if (it.HasClip)
                    {
                        string dd; double full = Est.ForItem(it, GetOpts(), out dd);
                        help = "Fetches the whole video" + (full > 0 ? " (" + T.Bytes(full) + ")" : "") + " at full speed, then cuts your clip.";
                    }
                }
            }
            lblClipHelp.Text = help;
            btnWhole.Visible = ok && it.HasClip;
            clipBar.Enabled = ok; tClipS.Enabled = ok; tClipE.Enabled = ok;
            clipSync = false;
        }

        void ClipResult()
        {
            var it = clipItem; if (it == null) return;
            string size = it.Lvi != null && it.Lvi.SubItems.Count > 2 ? it.Lvi.SubItems[2].Text : "";
            if (it.HasClip)
            {
                double end = it.Duration > 0 ? Math.Min(it.ClipEnd, it.Duration) : it.ClipEnd;
                lblClipRes.Text = "Clip: " + T.Dur(Math.Max(1, end - it.ClipStart)) + (it.Duration > 0 ? " of " + T.Dur(it.Duration) : "") + (size != "" && size != "?" ? "   \u00B7   " + size : "");
                lblClipRes.ForeColor = T.Accent;
            }
            else
            {
                lblClipRes.Text = "Whole " + (tAudio.Checked ? "audio" : "video") + (it.Duration > 0 ? "  \u00B7  " + T.Dur(it.Duration) : "");
                lblClipRes.ForeColor = T.Muted;
            }
        }

        void ClipTyped()
        {
            if (clipSync || !Clippable(clipItem) || busy) return;
            var it = clipItem; double dur = it.Duration;
            double a = T.ParseTime(tClipS.Text), z = tClipE.Text.Trim() == "" ? (dur > 0 ? dur : -2) : T.ParseTime(tClipE.Text);
            string err = null;
            if (a < 0) err = "Start time isn't valid - try 1:30.";
            else if (z == -2) err = "Type an end time.";
            else if (z < 0) err = "End time isn't valid - try 2:45.";
            else if (dur > 0 && a >= dur) err = "Start is after the end.";
            else if (z <= a) err = "End must be after the start.";
            if (err != null) { lblClipRes.Text = err; lblClipRes.ForeColor = T.Bad; return; }
            if (dur > 0 && z > dur) z = dur;
            ApplyClip(a, z);
        }

        void ApplyClip(double a, double z)
        {
            var it = clipItem; if (it == null) return;
            if (a <= 0.5 && it.Duration > 0 && z >= it.Duration - 0.5) it.ClipStart = it.ClipEnd = -1;
            else { it.ClipStart = a; it.ClipEnd = z; }
            Recompute(it); UpdateSummary(); lv.Invalidate();
        }

        void FocusClip(Item it)
        {
            if (it.Lvi != null) { lv.SelectedItems.Clear(); it.Lvi.Selected = true; it.Lvi.EnsureVisible(); }
            clipItem = it; RefreshClip();
            if (tClipS.Enabled) { tClipS.Focus(); tClipS.SelectAll(); }
        }

        // [value] -> browser seen on this PC (1 Firefox, 2 Edge, 3 Chrome, 4 Brave)
        static bool[] BrowserFound = new bool[6];
        static readonly string[] BrowserNames = { "", "Firefox", "Edge", "Chrome", "Brave", "" };
        static bool BrowserProfile(int v)
        {
            switch (v)
            {
                case 1: return Pics.HasDir("APPDATA", @"Mozilla\Firefox\Profiles");
                case 2: return Pics.HasDir("LOCALAPPDATA", @"Microsoft\Edge\User Data");
                case 3: return Pics.HasDir("LOCALAPPDATA", @"Google\Chrome\User Data");
                case 4: return Pics.HasDir("LOCALAPPDATA", @"BraveSoftware\Brave-Browser\User Data");
            }
            return false;
        }
        // the browser a "Use ... sign-in" button picks: Firefox works best, then the others that are installed
        static int BestBrowser() { foreach (int v in new[] { 1, 2, 3, 4 }) if (BrowserFound[v]) return v; return 1; }

        ChoiceRow BuildSignIn()
        {
            var c = new ChoiceRow(tip);
            bool win = Environment.OSVersion.Platform == PlatformID.Win32NT;
            Func<int, string, string, string, string, ChoiceRow.Choice> browser = (v, name, letter, note, rel) =>
            {
                string exeName = Path.GetFileName(rel);
                string exe = Pics.FindExe(rel) ?? Pics.AppPath(exeName);
                bool profile = BrowserProfile(v);
                var ch = new ChoiceRow.Choice(); ch.Value = v; ch.Letter = letter; ch.Img = Pics.ExeIcon(exe);
                ch.Show = exe != null || profile || !win;    // only browsers that are on this PC
                BrowserFound[v] = ch.Show;
                ch.Tip = "Use your " + name + " sign-in" + note; return ch;
            };
            var off = new ChoiceRow.Choice(); off.Value = 0; off.Text = "Off"; off.Tip = "No sign-in - fine for most links"; c.Items.Add(off);
            c.Items.Add(browser(1, "Firefox", "F", " (works best)", @"Mozilla Firefox\firefox.exe"));
            c.Items.Add(browser(3, "Chrome", "C", "\nIf it fails, close Chrome and try again.", @"Google\Chrome\Application\chrome.exe"));
            c.Items.Add(browser(2, "Edge", "E", "\nIf it fails, close Edge and try again.", @"Microsoft\Edge\Application\msedge.exe"));
            c.Items.Add(browser(4, "Brave", "B", "\nIf it fails, close Brave and try again.", @"BraveSoftware\Brave-Browser\Application\brave.exe"));
            var file = new ChoiceRow.Choice(); file.Value = 5; file.IconName = "file"; file.Tip = "Use a cookies.txt file (advanced)"; c.Items.Add(file);
            return c;
        }

        // ---------------- supported sites list
        void ShowSites()
        {
            using (var d = new Form())
            {
                d.Text = "Supported sites"; d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent; d.BackColor = T.Solid(T.Bg); d.Font = T.Body; d.ClientSize = new Size(T.P(460), T.P(540));
                if (Icon != null) d.Icon = Icon;
                int M = T.P(20), W = d.ClientSize.Width - 2 * M, y = T.P(18);
                Func<string, Font, Color, Label> para = (text, f, c) =>
                {
                    var l = L(text, f, c); l.AutoSize = false; l.Location = new Point(M, y);
                    int h = TextRenderer.MeasureText(text, f, new Size(W, 0), TextFormatFlags.WordBreak).Height;
                    l.Size = new Size(W, h + T.P(2)); d.Controls.Add(l); y = l.Bottom + T.P(8); return l;
                };
                para("Most used", T.BodyBold, T.Text);
                para("YouTube \u00B7 TikTok \u00B7 Instagram \u00B7 Facebook \u00B7 X (Twitter) \u00B7 Google Drive \u00B7 Vimeo \u00B7 Reddit \u00B7 Twitch \u00B7 SoundCloud \u00B7 Dailymotion \u00B7 Bilibili", T.Body, T.Text);
                y += T.P(6);
                var count = para("Loading the full list from yt-dlp...", T.BodyBold, T.Text);
                var search = Box(); search.SetBounds(M, y, W, T.P(28)); d.Controls.Add(search); y = search.Bottom + T.P(8);
                var hint = L("Search...", T.Body, T.Hint); hint.BackColor = T.Input; hint.Location = new Point(T.P(4), T.P(4)); hint.Cursor = Cursors.IBeam;
                hint.Click += (a, b) => search.Focus(); search.Controls.Add(hint);
                var list = new ListBox(); list.Font = T.Body; list.BorderStyle = BorderStyle.FixedSingle; list.BackColor = T.Solid(T.Input); list.ForeColor = T.Text;
                list.IntegralHeight = false; list.SetBounds(M, y, W, T.P(250)); d.Controls.Add(list); y = list.Bottom + T.P(8);
                para("Sites change often. If one stops working, click Update yt-dlp at the top.", T.Small, T.Muted);
                var ok = new FlatButton("OK", "", 0); ok.Size = new Size(T.P(110), T.P(38));
                ok.Location = new Point(d.ClientSize.Width - M - ok.Width, y + T.P(4));
                d.ClientSize = new Size(d.ClientSize.Width, ok.Bottom + M);
                ok.Click += (a, b) => d.Close(); d.Controls.Add(ok);
                var all = new List<string>();
                Action fill = () =>
                {
                    string q = search.Text.Trim(); hint.Visible = q == "";
                    list.BeginUpdate(); list.Items.Clear();
                    foreach (var n in all) if (q == "" || n.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) list.Items.Add(n);
                    list.EndUpdate();
                };
                search.TextChanged += (a, b) => fill();
                string exe = ytdlp, ver = ytdlpVersion;
                d.Shown += (a, b) =>
                {
                    if (exe == null) { count.Text = "Install the helper tools first to see the full list."; return; }
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        var names = new List<string>();
                        try
                        {
                            var psi = new ProcessStartInfo(exe, "--ignore-config --list-extractors");
                            psi.UseShellExecute = false; psi.RedirectStandardOutput = true; psi.CreateNoWindow = true; psi.StandardOutputEncoding = Encoding.UTF8;
                            string outp; using (var pr = Process.Start(psi)) { outp = pr.StandardOutput.ReadToEnd(); pr.WaitForExit(20000); }
                            var seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                            foreach (var line in outp.Split('\n'))
                            {
                                string t = line.Trim(); if (t == "" || t.Contains("CURRENTLY BROKEN")) continue;
                                string n = t.Split(':')[0].Trim();
                                if (n == "" || n.Equals("generic", StringComparison.OrdinalIgnoreCase) || seen.ContainsKey(n) || Adult.Extractor(n)) continue;
                                seen[n] = true;
                                names.Add(n);
                            }
                            names.Sort(StringComparer.OrdinalIgnoreCase);
                        }
                        catch { }
                        try
                        {
                            if (d.IsDisposed || !d.IsHandleCreated) return;
                            d.BeginInvoke((MethodInvoker)delegate
                            {
                                all = names;
                                count.Text = names.Count == 0 ? "Couldn't read the list from yt-dlp." :
                                    names.Count.ToString("N0", CultureInfo.InvariantCulture) + " sites and services" + (ver != "" ? " (yt-dlp " + ver + ")" : "");
                                fill();
                            });
                        }
                        catch { }
                    });
                };
                d.ShowDialog(this);
            }
        }

        // simple text window for Terms of use / Privacy (lines starting with # are headings)
        void ShowText(string title, string[] paras)
        {
            using (var d = new Form())
            {
                d.Text = title; d.FormBorderStyle = FormBorderStyle.FixedDialog; d.MaximizeBox = false; d.MinimizeBox = false; d.ShowInTaskbar = false;
                d.StartPosition = FormStartPosition.CenterParent; d.BackColor = T.Solid(T.Bg); d.Font = T.Body; d.ClientSize = new Size(T.P(520), T.P(200));
                if (Icon != null) d.Icon = Icon;
                int M = T.P(22), W = d.ClientSize.Width - 2 * M, y = T.P(18);
                foreach (var raw in paras)
                {
                    bool head = raw.StartsWith("#"); string text = head ? raw.Substring(1).Trim() : raw;
                    var f = head ? T.BodyBold : T.Body;
                    var l = L(text, f, T.Text); l.AutoSize = false; l.Location = new Point(M, y);
                    int h = TextRenderer.MeasureText(text, f, new Size(W, 0), TextFormatFlags.WordBreak).Height;
                    l.Size = new Size(W, h + T.P(2)); d.Controls.Add(l); y = l.Bottom + (head ? T.P(4) : T.P(10));
                }
                var ok = new FlatButton("OK", "", 0); ok.Size = new Size(T.P(110), T.P(38));
                ok.Location = new Point(d.ClientSize.Width - M - ok.Width, y + T.P(6));
                d.ClientSize = new Size(d.ClientSize.Width, ok.Bottom + M);
                ok.Click += (a, b) => d.Close(); d.Controls.Add(ok);
                d.ShowDialog(this);
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
            if (Adult.Url(u)) return false;
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
            string cur = txtLinks.Text.TrimEnd();
            txtLinks.Text = (cur.Length > 0 ? cur + Environment.NewLine : "") + string.Join(Environment.NewLine, add.ToArray()) + Environment.NewLine;
            txtLinks.SelectionStart = txtLinks.TextLength;
            debounce.Stop(); SyncItems();
            if (!busy) SetStatus("Caught " + (add.Count == 1 ? "a link" : add.Count + " links") + " from the clipboard.", T.Ok);
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
            loading = false; lastCookies = GetOpts().Cookies;
            ApplyMode(); debounce.Stop(); SyncItems();
            foreach (var it in items) SetItemStatus(it, it.Status, it.StatusKind, it.Action);
            UpdateToolsLabel(); UpdateSummary(); ApplyWindowTheme(); StartClipboardWatch();
            Invalidate(true);
            SetStatus("Skin: " + name + ".", T.Muted);
        }

        // ---------------- about
        // ---------------- updating Dawnloader itself (About > Check for updates)
        static string UpdateBase { get { return Environment.GetEnvironmentVariable("YTD_UPDATE_BASE") ?? "https://raw.githubusercontent.com/zar-iffs/dawnloader/main/"; } }
        static bool NewerVersion(string a, string b)
        {
            var x = a.Split('.'); var y = b.Split('.');
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                int p = 0, q = 0;
                if (i < x.Length) int.TryParse(x[i], out p);
                if (i < y.Length) int.TryParse(y[i], out q);
                if (p != q) return p > q;
            }
            return false;
        }
        static byte[] UpdateBytes(string file)
        {
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "Dawnloader/" + AppVersion;
                wc.Headers[HttpRequestHeader.CacheControl] = "no-cache";
                return wc.DownloadData(UpdateBase + file + "?t=" + DateTime.UtcNow.Ticks);
            }
        }
        static string UpdateText(string file) { return Encoding.UTF8.GetString(UpdateBytes(file)); }
        // downloads the new Dawnloader.ps1, checks it, keeps the old one as Dawnloader.ps1.bak and swaps it in. Returns null on success, else a reason.
        string InstallUpdate(string version)
        {
            string target = Path.Combine(dir, "Dawnloader.ps1"), tmp = target + ".new", bak = target + ".bak";
            try
            {
                if (!File.Exists(target)) return "Dawnloader.ps1 wasn't found next to the app.";
                byte[] data = UpdateBytes("dist/Dawnloader.ps1");
                string text = Encoding.UTF8.GetString(data);
                if (data.Length < 100000 || !text.Contains("[YTD.Program]::Run") || !text.Contains("AppVersion = \"" + version + "\""))
                    return "the downloaded file didn't look right.";
                File.WriteAllBytes(tmp, data);
                File.Copy(target, bak, true);
                try { File.Copy(tmp, target, true); }
                catch { try { File.Copy(bak, target, true); } catch { } throw; }
                TryDel(tmp);
                return null;
            }
            catch (Exception ex) { TryDel(tmp); return (ex is UnauthorizedAccessException ? "the folder is write-protected (move Dawnloader to a normal folder such as Documents)." : ex.Message); }
        }

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
                para("Downloads video and audio from YouTube, TikTok, Instagram, Facebook, X, Google Drive and 1000+ other sites. Everything runs on this PC - nothing is uploaded anywhere.", T.Body, T.Text);
                para("Use it responsibly", T.BodyBold, T.Text);
                para("Only download content you own, that is licensed for reuse, or that you have permission to save. You are responsible for how you use what you download. Streaming services that use copy protection (DRM), such as Netflix or Spotify, can't be downloaded.", T.Body, T.Text);
                para("Built on yt-dlp (Unlicense), FFmpeg (LGPL/GPL) and Deno (MIT). Not affiliated with any of the sites it supports.", T.Small, T.Muted);
                Func<string, Action, LinkLabel> link = (text, act) =>
                {
                    var l = new LinkLabel(); l.Text = text; l.Font = T.Body; l.AutoSize = true; l.BackColor = Color.Transparent;
                    l.LinkColor = T.Accent; l.ActiveLinkColor = T.Cur.AccentDark; l.VisitedLinkColor = T.Accent; l.LinkBehavior = LinkBehavior.HoverUnderline;
                    l.LinkClicked += (a, b) => act(); return l;
                };
                Action<string> open = url => { try { Process.Start(url); } catch { } };
                Row(d, M, y, T.P(24), L("Made by", T.Body, T.Muted), -4, link("zar-iffs on GitHub", () => open(GitHubUrl)));
                y += T.P(30);
                Row(d, M, y, T.P(24), link("Terms of use", ShowTerms), 6, L("\u00B7", T.Body, T.Muted), 6, link("Privacy", ShowPrivacy), 6, L("\u00B7", T.Body, T.Muted), 6, link("Source code", () => open(GitHubUrl + "/dawnloader")));
                y += T.P(34);
                var updMsg = L("", T.Small, T.Muted); updMsg.AutoSize = false; updMsg.Location = new Point(M, y + T.P(2)); updMsg.Size = new Size(W, T.P(34)); d.Controls.Add(updMsg);
                y += T.P(38);
                var upd = new FlatButton("Check for updates", "refresh", 1); upd.Size = new Size(T.P(190), T.P(38)); upd.Location = new Point(M, y + T.P(4)); d.Controls.Add(upd);
                int updState = 0; string newVer = null;
                Action<string, Color> say = (t, c) => { updMsg.Text = t; updMsg.ForeColor = c; };
                upd.Click += (a, b) =>
                {
                    if (updState == 0)
                    {
                        if (busy) { say("Finish the current downloads first, then check again.", T.Warn); return; }
                        upd.Enabled = false; say("Checking GitHub...", T.Muted);
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            string v = null, err = null;
                            try { v = UpdateText("VERSION").Trim(); if (!Regex.IsMatch(v, @"^\d+(\.\d+){1,3}$")) { err = "unexpected answer"; v = null; } } catch (Exception ex) { err = ex.Message; }
                            try { d.BeginInvoke((MethodInvoker)(() =>
                            {
                                upd.Enabled = true;
                                if (v == null) say(err == "unexpected answer" ? "Couldn't check: GitHub gave an unexpected answer. Try again later." : "Couldn't reach GitHub. Check your internet connection and try again.", T.Bad);
                                else if (NewerVersion(v, AppVersion)) { newVer = v; updState = 1; upd.Text = "Update now"; say("Version " + v + " is available (you have " + AppVersion + ").", T.Ok); }
                                else say("You have the latest version (" + AppVersion + ").", T.Ok);
                            })); } catch { }
                        });
                    }
                    else if (updState == 1)
                    {
                        if (busy) { say("Finish the current downloads first, then update.", T.Warn); return; }
                        upd.Enabled = false; say("Downloading version " + newVer + "...", T.Muted); string nv = newVer;
                        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                        {
                            string err = InstallUpdate(nv);
                            try { d.BeginInvoke((MethodInvoker)(() =>
                            {
                                upd.Enabled = true;
                                if (err != null) say("Couldn't update: " + err + " Your current version is unchanged.", T.Bad);
                                else
                                {
                                    updState = 2; say("Updated to " + nv + ". Restart Dawnloader to use it (the first start takes a few seconds longer).", T.Ok);
                                    upd.Text = File.Exists(Path.Combine(dir, "Start Dawnloader.bat")) ? "Restart now" : "Close Dawnloader";
                                }
                            })); } catch { }
                        });
                    }
                    else
                    {
                        string bat = Path.Combine(dir, "Start Dawnloader.bat");
                        try { if (File.Exists(bat)) { var psi = new ProcessStartInfo(bat) { UseShellExecute = true, WorkingDirectory = dir, WindowStyle = ProcessWindowStyle.Hidden }; Process.Start(psi); } } catch { }
                        d.Close(); Close();
                    }
                };
                y += T.P(40);
                var ok = new FlatButton("OK", "", 0); ok.Size = new Size(T.P(110), T.P(38));
                ok.Location = new Point(d.ClientSize.Width - M - ok.Width, Math.Max(y - T.P(36), d.ClientSize.Height - M - ok.Height));
                d.ClientSize = new Size(d.ClientSize.Width, ok.Bottom + M);
                ok.Click += (a, b) => d.Close(); d.Controls.Add(ok);
                d.ShowDialog(this);
            }
        }
        const string AppVersion = "2026.11.6";
        const string GitHubUrl = "https://github.com/zar-iffs";

        void ShowTerms()
        {
            ShowText("Terms of use", new[] {
                "# Terms of use",
                "Dawnloader is free, open-source software under the MIT licence. It is provided \"as is\", without any warranty.",
                "You are responsible for what you download. Only download content you own, that is licensed for reuse, or that you have permission to save, and follow the rules of the sites you use.",
                "Adult (porn) sites are blocked: Dawnloader will not check or download links from them. Dawnloader does not get around copy protection (DRM). It is not made by, affiliated with or endorsed by YouTube, Google, TikTok, Meta, X or any other site. Site names and icons belong to their owners.",
                "The helper tools it uses (yt-dlp, FFmpeg, Deno) are separate projects with their own licences." });
        }

        void ShowPrivacy()
        {
            ShowText("Privacy", new[] {
                "# Privacy",
                "Dawnloader has no account, no ads and no tracking. It does not collect or send any information about you.",
                "# What it connects to",
                "Only the sites of the links you add (to check, preview and download them), those sites' icons for the header, and GitHub or gyan.dev when you install or update the helper tools. 'Check for updates' in About asks this project's GitHub page for a small version file; 'Update now' downloads the new Dawnloader.ps1 from there. Neither happens unless you click.",
                "# Sign-in",
                "If you pick a browser under Sign in with, the login saved in that browser is used on this PC and sent only to the site you are downloading from. A temporary copy is kept in your Temp folder and deleted when Dawnloader closes.",
                "# Files on your PC",
                "Settings are saved next to Dawnloader.ps1. Download history (to skip what you already have) and cached site icons are in %LOCALAPPDATA%\\Dawnloader - delete that folder to clear them." });
        }

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
        class ToolPkg { public string Name, File; public long Size; public string[] Urls; public string[] Pick; }
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

        void InstallTools()
        {
            if (busy || installing) return;
            var todo = MissingTools(); if (todo.Count == 0) { UpdateToolsLabel(); return; }
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
                if (t.Pick == null) PlaceExe(tmp, Path.Combine(tdir, t.File));
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
            if (installCancel) { SetStatus("Setup cancelled. Click 'Install missing tools' to try again.", T.Warn); bar.Value = 0; return; }
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
