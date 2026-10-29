using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyCenter
{
    /// <summary>界面明暗模式。</summary>
    internal enum SurfaceRole
    {
        Bg,       // 窗口底色
        Sidebar,  // 侧边栏
        Surface   // 卡片/浮层表面（白色或深灰）
    }

    internal enum LabelTone
    {
        Custom,
        Text,
        Dim,
        Muted,
        Accent,
        Danger,
        Success
    }

    /// <summary>一套配色。</summary>
    internal sealed class Palette
    {
        public bool IsDark;
        public Color Bg;
        public Color Sidebar;
        public Color PanelDark;
        public Color Card;
        public Color CardHover;
        public Color Field;
        public Color Border;
        public Color BorderHi;
        public Color Text;
        public Color TextDim;
        public Color Muted;
        public Color Accent;
        public Color AccentHover;
        public Color AccentDeep;
        public Color AccentSoft;
        public Color OnAccent;
        public Color Danger;
        public Color DangerSoft;
        public Color Success;
        public Color ScrollTrack;
        public Color ScrollThumb;
        public Color ScrollThumbHover;
        public Color GlassTint;

        public static Palette CreateDark()
        {
            Palette p = new Palette();
            p.IsDark = true;
            p.Bg = Color.FromArgb(14, 17, 23);
            p.Sidebar = Color.FromArgb(16, 20, 27);
            p.PanelDark = Color.FromArgb(20, 25, 34);
            p.Card = Color.FromArgb(23, 28, 38);
            p.CardHover = Color.FromArgb(29, 36, 49);
            p.Field = Color.FromArgb(13, 17, 23);
            p.Border = Color.FromArgb(36, 44, 58);
            p.BorderHi = Color.FromArgb(60, 74, 96);
            p.Text = Color.FromArgb(232, 236, 244);
            p.TextDim = Color.FromArgb(169, 178, 196);
            p.Muted = Color.FromArgb(125, 135, 155);
            p.Accent = Color.FromArgb(91, 157, 255);
            p.AccentHover = Color.FromArgb(112, 173, 255);
            p.AccentDeep = Color.FromArgb(59, 130, 246);
            p.AccentSoft = Color.FromArgb(30, 42, 64);
            p.OnAccent = Color.FromArgb(10, 16, 26);
            p.Danger = Color.FromArgb(255, 107, 107);
            p.DangerSoft = Color.FromArgb(58, 30, 34);
            p.Success = Color.FromArgb(61, 220, 151);
            p.ScrollTrack = Color.FromArgb(27, 33, 44);
            p.ScrollThumb = Color.FromArgb(50, 61, 80);
            p.ScrollThumbHover = Color.FromArgb(74, 90, 116);
            p.GlassTint = Color.FromArgb(184, 12, 16, 23);
            return p;
        }

        public static Palette CreateLight()
        {
            Palette p = new Palette();
            p.IsDark = false;
            p.Bg = Color.FromArgb(244, 246, 250);
            p.Sidebar = Color.FromArgb(236, 239, 245);
            p.PanelDark = Color.FromArgb(255, 255, 255);
            p.Card = Color.FromArgb(255, 255, 255);
            p.CardHover = Color.FromArgb(245, 248, 253);
            p.Field = Color.FromArgb(241, 244, 249);
            p.Border = Color.FromArgb(219, 225, 235);
            p.BorderHi = Color.FromArgb(176, 187, 204);
            p.Text = Color.FromArgb(26, 31, 40);
            p.TextDim = Color.FromArgb(71, 81, 97);
            p.Muted = Color.FromArgb(124, 134, 150);
            p.Accent = Color.FromArgb(37, 99, 235);
            p.AccentHover = Color.FromArgb(59, 130, 246);
            p.AccentDeep = Color.FromArgb(29, 78, 216);
            p.AccentSoft = Color.FromArgb(226, 237, 255);
            p.OnAccent = Color.FromArgb(255, 255, 255);
            p.Danger = Color.FromArgb(220, 38, 38);
            p.DangerSoft = Color.FromArgb(253, 232, 232);
            p.Success = Color.FromArgb(5, 150, 105);
            p.ScrollTrack = Color.FromArgb(232, 236, 243);
            p.ScrollThumb = Color.FromArgb(193, 202, 216);
            p.ScrollThumbHover = Color.FromArgb(160, 171, 190);
            p.GlassTint = Color.FromArgb(40, 246, 248, 252);
            return p;
        }
    }

    /// <summary>主题感知控件：切换主题时会被重新应用颜色。</summary>
    internal interface IThemed
    {
        void ApplyTheme();
    }

    /// <summary>全局主题：配色、字体、DPI 缩放、绘图辅助。</summary>
    internal static class T
    {
        public static float Scale = 1f;
        public static bool Inited;

        private static Palette palette = Palette.CreateDark();

        // ---------- 动态配色 ----------
        public static bool IsDark { get { return palette.IsDark; } }
        public static Color Bg { get { return palette.Bg; } }
        public static Color Sidebar { get { return palette.Sidebar; } }
        public static Color PanelDark { get { return palette.PanelDark; } }
        public static Color Card { get { return palette.Card; } }
        public static Color CardHover { get { return palette.CardHover; } }
        public static Color Field { get { return palette.Field; } }
        public static Color Border { get { return palette.Border; } }
        public static Color BorderHi { get { return palette.BorderHi; } }
        public static Color Text { get { return palette.Text; } }
        public static Color TextDim { get { return palette.TextDim; } }
        public static Color Muted { get { return palette.Muted; } }
        public static Color Accent { get { return palette.Accent; } }
        public static Color AccentHover { get { return palette.AccentHover; } }
        public static Color AccentDeep { get { return palette.AccentDeep; } }
        public static Color AccentSoft { get { return palette.AccentSoft; } }
        public static Color OnAccent { get { return palette.OnAccent; } }
        public static Color Danger { get { return palette.Danger; } }
        public static Color DangerSoft { get { return palette.DangerSoft; } }
        public static Color Success { get { return palette.Success; } }
        public static Color ScrollTrack { get { return palette.ScrollTrack; } }
        public static Color ScrollThumb { get { return palette.ScrollThumb; } }
        public static Color ScrollThumbHover { get { return palette.ScrollThumbHover; } }
        public static Color GlassTint { get { return palette.GlassTint; } }

        // ---------- 字体 ----------
        public static string UiFamily;
        public static string MonoFamily;
        public static string IconFamily;

        public static Font FontUi;
        public static Font FontSmall;
        public static Font FontBold;
        public static Font FontSection;
        public static Font FontTitle;
        public static Font FontH1;
        public static Font FontMono;
        public static Font FontMonoSm;
        public static Font FontIcon;
        public static Font FontIconSm;
        public static ToolTip Tip;

        public static event EventHandler ThemeChanged;

        public static void Init()
        {
            if (Inited) return;
            Inited = true;

            InstalledFontCollection col = new InstalledFontCollection();
            List<string> names = new List<string>();
            FontFamily[] fams = col.Families;
            for (int i = 0; i < fams.Length; i++) names.Add(fams[i].Name);

            UiFamily = Pick(names, "Microsoft YaHei UI", "Microsoft YaHei", "Segoe UI", "Arial");
            MonoFamily = Pick(names, "Cascadia Mono", "Cascadia Code", "Consolas", "Courier New");
            IconFamily = Pick(names, "Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol", "Segoe UI");

            FontUi = new Font(UiFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            FontSmall = new Font(UiFamily, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            FontBold = new Font(UiFamily, 10.5f, FontStyle.Bold, GraphicsUnit.Point);
            FontSection = new Font(UiFamily, 12f, FontStyle.Bold, GraphicsUnit.Point);
            FontTitle = new Font(UiFamily, 14f, FontStyle.Bold, GraphicsUnit.Point);
            FontH1 = new Font(UiFamily, 17f, FontStyle.Bold, GraphicsUnit.Point);
            FontMono = new Font(MonoFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
            FontMonoSm = new Font(MonoFamily, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
            FontIcon = new Font(IconFamily, 10.5f, FontStyle.Regular, GraphicsUnit.Point);
            FontIconSm = new Font(IconFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);

            Tip = new ToolTip();
            Tip.InitialDelay = 500;
            Tip.ReshowDelay = 100;
            Tip.AutoPopDelay = 8000;
        }

        public static void SetTheme(bool dark, bool notify)
        {
            palette = dark ? Palette.CreateDark() : Palette.CreateLight();
            if (notify && ThemeChanged != null) ThemeChanged(null, EventArgs.Empty);
        }

        public static Color RoleColor(SurfaceRole role)
        {
            switch (role)
            {
                case SurfaceRole.Sidebar: return Sidebar;
                case SurfaceRole.Surface: return PanelDark;
                default: return Bg;
            }
        }

        public static Color ToneColor(LabelTone tone)
        {
            switch (tone)
            {
                case LabelTone.Text: return Text;
                case LabelTone.Dim: return TextDim;
                case LabelTone.Muted: return Muted;
                case LabelTone.Accent: return Accent;
                case LabelTone.Danger: return Danger;
                case LabelTone.Success: return Success;
                default: return Text;
            }
        }

        public static bool TryGetTone(Color c, out LabelTone tone)
        {
            int argb = c.ToArgb();
            if (argb == Text.ToArgb()) { tone = LabelTone.Text; return true; }
            if (argb == TextDim.ToArgb()) { tone = LabelTone.Dim; return true; }
            if (argb == Muted.ToArgb()) { tone = LabelTone.Muted; return true; }
            if (argb == Accent.ToArgb()) { tone = LabelTone.Accent; return true; }
            if (argb == Danger.ToArgb()) { tone = LabelTone.Danger; return true; }
            if (argb == Success.ToArgb()) { tone = LabelTone.Success; return true; }
            tone = LabelTone.Custom;
            return false;
        }

        public static bool TryGetSurface(Color c, out SurfaceRole role)
        {
            int argb = c.ToArgb();
            if (argb == Bg.ToArgb()) { role = SurfaceRole.Bg; return true; }
            if (argb == Sidebar.ToArgb()) { role = SurfaceRole.Sidebar; return true; }
            if (argb == PanelDark.ToArgb()) { role = SurfaceRole.Surface; return true; }
            role = SurfaceRole.Bg;
            return false;
        }

        /// <summary>递归把当前主题应用到控件树（实现了 IThemed 的控件会更新颜色）。</summary>
        public static void ApplyTo(Control root)
        {
            if (root == null) return;
            IThemed themed = root as IThemed;
            if (themed != null) themed.ApplyTheme();
            for (int i = 0; i < root.Controls.Count; i++) ApplyTo(root.Controls[i]);
            if (themeBatchDepth == 0) root.Invalidate();
        }

        private static int themeBatchDepth;

        /// <summary>整树批量换主题：子控件只改状态，不逐个 Invalidate，由调用方统一刷新一次。</summary>
        public static void ApplyTree(Control root)
        {
            themeBatchDepth++;
            try { ApplyTo(root); }
            finally { themeBatchDepth--; }
        }

        private static string Pick(List<string> installed, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                for (int j = 0; j < installed.Count; j++)
                {
                    if (string.Equals(installed[j], candidates[i], StringComparison.OrdinalIgnoreCase))
                        return installed[j];
                }
            }
            return "Segoe UI";
        }

        public static int Px(int v)
        {
            return (int)Math.Round(v * (double)Scale);
        }

        public static GraphicsPath RoundPath(RectangleF r, float radius)
        {
            GraphicsPath p = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 1f)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Color color, RectangleF r, float radius)
        {
            if (color.A == 0) return;
            using (GraphicsPath p = RoundPath(r, radius))
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPath(b, p);
            }
        }

        public static void DrawRound(Graphics g, Color color, RectangleF r, float radius, float width)
        {
            using (GraphicsPath p = RoundPath(r, radius))
            using (Pen pen = new Pen(color, width))
            {
                pen.Alignment = PenAlignment.Center;
                g.DrawPath(pen, p);
            }
        }

        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }

        public static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        public static void DrawIcon(Graphics g, string glyph, Font font, Rectangle bounds, Color color)
        {
            TextRenderer.DrawText(g, glyph, font, bounds, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }

        public static int MeasureWidth(string text, Font font)
        {
            if (text == null) text = "";
            Size s = TextRenderer.MeasureText(text, font);
            return s.Width;
        }

        // ---------- 窗口标题栏 / 模糊玻璃 ----------
        public static bool AcrylicActive = false;
        public static IntPtr GlassHwnd = IntPtr.Zero;

        public static void ApplyTitleBar(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            try
            {
                int dark = IsDark ? 1 : 0;
                Native.DwmSetWindowAttribute(hwnd, 20, ref dark, 4); // DWMWA_USE_IMMERSIVE_DARK_MODE
                if (AcrylicActive && hwnd == GlassHwnd)
                {
                    // 交给 DWM + Acrylic 绘制玻璃标题栏，恢复默认颜色
                    int def = -1;
                    Native.DwmSetWindowAttribute(hwnd, 35, ref def, 4);
                    Native.DwmSetWindowAttribute(hwnd, 34, ref def, 4);
                    Native.DwmSetWindowAttribute(hwnd, 36, ref def, 4);
                }
                else
                {
                    int caption = Bgr(Bg);
                    Native.DwmSetWindowAttribute(hwnd, 35, ref caption, 4); // DWMWA_CAPTION_COLOR
                    int border = Bgr(Border);
                    Native.DwmSetWindowAttribute(hwnd, 34, ref border, 4); // DWMWA_BORDER_COLOR
                    int txt = Bgr(Text);
                    Native.DwmSetWindowAttribute(hwnd, 36, ref txt, 4); // DWMWA_TEXT_COLOR
                }
                int corner = 2;
                Native.DwmSetWindowAttribute(hwnd, 33, ref corner, 4); // DWMWA_WINDOW_CORNER_PREFERENCE = ROUND
                Native.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                    0x0001 | 0x0002 | 0x0004 | 0x0020); // NOSIZE|NOMOVE|NOZORDER|FRAMECHANGED
            }
            catch (Exception)
            {
                // 老系统不支持这些属性时忽略
            }
        }

        /// <summary>开启 Windows 11 系统背景（Acrylic/Mica 模糊玻璃），失败时静默降级。</summary>
        public static bool EnableAcrylic(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            try
            {
                int backdrop = 3; // DWMSBT_TRANSIENTWINDOW (Acrylic)
                int hr = Native.DwmSetWindowAttribute(hwnd, 38, ref backdrop, 4);
                AcrylicActive = hr == 0;
            }
            catch (Exception)
            {
                AcrylicActive = false;
            }
            return AcrylicActive;
        }

        /// <summary>让内嵌滚动条使用系统深色/浅色主题（而非自绘）。</summary>
        public static void ApplyScrollbarTheme(Control c, bool dark)
        {
            if (c == null || !c.IsHandleCreated) return;
            try
            {
                Native.SetWindowTheme(c.Handle, dark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch (Exception) { }
        }

        /// <summary>把控件裁剪成圆角矩形（用于玻璃外壳）。</summary>
        public static void SetRoundedRegion(Control c, int radiusPx)
        {
            if (c == null || c.Width <= 0 || c.Height <= 0) return;
            try
            {
                int d = Math.Max(2, radiusPx * 2);
                if (d > c.Width) d = c.Width;
                if (d > c.Height) d = c.Height;
                IntPtr hrgn = Native.CreateRoundRectRgn(0, 0, c.Width + 1, c.Height + 1, d, d);
                if (hrgn == IntPtr.Zero) return;
                try
                {
                    Region old = c.Region;
                    c.Region = Region.FromHrgn(hrgn);
                    if (old != null) old.Dispose();
                }
                finally
                {
                    Native.DeleteObject(hrgn);
                }
            }
            catch (Exception) { }
        }


        private static int Bgr(Color c)
        {
            return c.R | (c.G << 8) | (c.B << 16);
        }

        public static int IdleSeconds()
        {
            try
            {
                Native.LASTINPUTINFO li = new Native.LASTINPUTINFO();
                li.cbSize = (uint)Marshal.SizeOf(typeof(Native.LASTINPUTINFO));
                if (!Native.GetLastInputInfo(ref li)) return 0;
                uint tick = unchecked((uint)Environment.TickCount);
                uint idle = tick - li.dwTime;
                return (int)(idle / 1000u);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    /// <summary>主题模式。</summary>
    internal enum ThemeMode
    {
        System = 0,
        Light = 1,
        Dark = 2
    }

    /// <summary>排序方式。</summary>
    internal enum SortMode
    {
        CreatedDesc = 0,
        CreatedAsc = 1,
        UpdatedDesc = 2,
        UpdatedAsc = 3,
        NameAsc = 4,
        NameDesc = 5
    }

    /// <summary>本地配置（主题偏好、窗口位置、侧边栏宽度、排序），不含任何密钥。</summary>
    internal static class AppConfig
    {
        public static ThemeMode ThemeMode = ThemeMode.System;
        public static bool WinHasBounds = false;
        public static int WinX;
        public static int WinY;
        public static int WinLogicalW = 1000;
        public static int WinLogicalH = 700;
        public static bool WinMaximized = false;
        public static int SidebarLogicalWidth = 236;
        public static SortMode Sort = SortMode.CreatedDesc;
        public static bool GlassDisabled = false;
        public static string Language = "zh";

        private static string CfgPath
        {
            get { return Path.Combine(Application.StartupPath, "keycenter.cfg"); }
        }

        public static bool SystemThemeIsDark()
        {
            try
            {
                object v = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme", null);
                if (v is int) return (int)v == 0;
            }
            catch (Exception) { }
            return true;
        }

        public static bool ResolveThemeIsDark()
        {
            if (ThemeMode == ThemeMode.Dark) return true;
            if (ThemeMode == ThemeMode.Light) return false;
            return SystemThemeIsDark();
        }

        public static string ThemeModeName
        {
            get
            {
                if (ThemeMode == ThemeMode.Dark) return L.T("深色");
                if (ThemeMode == ThemeMode.Light) return L.T("浅色");
                return L.T("跟随系统");
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(CfgPath)) return;
                string[] lines = File.ReadAllLines(CfgPath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string s = lines[i].Trim();
                    if (s.Length == 0 || s.StartsWith("#", StringComparison.Ordinal)) continue;
                    int eq = s.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = s.Substring(0, eq).Trim().ToLowerInvariant();
                    string val = s.Substring(eq + 1).Trim();
                    if (key == "theme")
                    {
                        if (val.Equals("dark", StringComparison.OrdinalIgnoreCase)) ThemeMode = ThemeMode.Dark;
                        else if (val.Equals("light", StringComparison.OrdinalIgnoreCase)) ThemeMode = ThemeMode.Light;
                        else ThemeMode = ThemeMode.System;
                    }
                    else if (key == "winx") { int n; if (int.TryParse(val, out n)) { WinX = n; WinHasBounds = true; } }
                    else if (key == "winy") { int n; if (int.TryParse(val, out n)) WinY = n; }
                    else if (key == "winw") { int n; if (int.TryParse(val, out n) && n >= 400) WinLogicalW = n; }
                    else if (key == "winh") { int n; if (int.TryParse(val, out n) && n >= 300) WinLogicalH = n; }
                    else if (key == "winmax") WinMaximized = val == "1";
                    else if (key == "sidebar") { int n; if (int.TryParse(val, out n)) SidebarLogicalWidth = n; }
                    else if (key == "sort") { int n; if (int.TryParse(val, out n) && n >= 0 && n <= 5) Sort = (SortMode)n; }
                    else if (key == "language") { Language = val.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "zh"; }                }
            }
            catch (Exception) { }
        }

        public static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(L.T("# Key Center 配置（不含任何密钥）"));
                string theme = ThemeMode == ThemeMode.Dark ? "dark" : (ThemeMode == ThemeMode.Light ? "light" : "system");
                sb.AppendLine("theme=" + theme);
                if (WinHasBounds)
                {
                    sb.AppendLine("winx=" + WinX);
                    sb.AppendLine("winy=" + WinY);
                    sb.AppendLine("winw=" + WinLogicalW);
                    sb.AppendLine("winh=" + WinLogicalH);
                    sb.AppendLine("winmax=" + (WinMaximized ? "1" : "0"));
                }
                sb.AppendLine("sidebar=" + SidebarLogicalWidth);
                sb.AppendLine("sort=" + ((int)Sort));
                sb.AppendLine("language=" + (Language == "en" ? "en" : "zh"));
                WriteConfigAtomic(CfgPath, sb.ToString());
            }
            catch (Exception) { }
        }

        private static void WriteConfigAtomic(string path, string text)
        {
            string tmp = path + ".tmp";
            string backup = path + ".bak";
            try
            {
                using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    sw.Write(text);
                    sw.Flush();
                    fs.Flush(true);
                }

                if (!File.Exists(path))
                {
                    File.Move(tmp, path);
                    return;
                }

                try
                {
                    File.Replace(tmp, path, null, true);
                }
                catch (Exception)
                {
                    // 配置优先级低，但不能直接覆盖成截断文件；保留旧配置，失败则放弃本次保存。
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(path, backup);
                    try
                    {
                        File.Move(tmp, path);
                        try { File.Delete(backup); }
                        catch (Exception) { }
                    }
                    catch (Exception)
                    {
                        try
                        {
                            if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
                        }
                        catch (Exception) { }
                        throw;
                    }
                }
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); }
                catch (Exception) { }
            }
        }
    }

    /// <summary>MDL2 / Fluent 图标字形。</summary>
    internal static class Ico
    {
        public const string Copy = "\uE8C8";
        public const string View = "\uE890";
        public const string Edit = "\uE70F";
        public const string Delete = "\uE74D";
        public const string Lock = "\uE72E";
        public const string Settings = "\uE713";
        public const string Add = "\uE710";
        public const string Search = "\uE721";
        public const string Close = "\uE711";
        public const string ChevronDown = "\uE70D";
        public const string Check = "\uE73E";
        public const string Warning = "\uE7BA";
        public const string Info = "\uE946";
        public const string Folder = "\uE8B7";
        public const string Sun = "\uE706";
        public const string Moon = "\uE708";
        public const string Export = "\uE896";
        public const string Star = "\uE734";
        public const string StarFill = "\uE735";
        public const string MultiKey = "\uE8F1";
    }

    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }


        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);


        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr obj);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);


        [DllImport("user32.dll")]
        public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
    }
}
