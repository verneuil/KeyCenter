using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>无边框弹窗的独立软阴影窗口，保证上下左右都有阴影且不依赖系统标题栏。</summary>
    internal class ShadowForm : Form
    {
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_NOOWNERZORDER = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int W; public int H; }
        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION { public byte Op; public byte Flags; public byte Alpha; public byte Format; }

        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("user32.dll")] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pSrc, int crKey, ref BLENDFUNCTION pblend, int flags);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);

        private readonly Form target;
        private Bitmap canvas;

        public ShadowForm(Form target)
        {
            this.target = target;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            Opacity = 1;
            SetStyle(ControlStyles.Opaque, true);
        }

        public void Attach()
        {
            if (IsDisposed || target == null || target.IsDisposed) return;
            if (!Visible) Show();
            UpdateForTarget();
        }

        public void UpdateForTarget()
        {
            if (IsDisposed || target == null || target.IsDisposed) return;
            if (!target.Visible)
            {
                if (Visible) Hide();
                return;
            }
            if (!Visible) Show();
            int margin = T.Px(10);
            int offsetY = T.Px(2);
            int w = target.Width + margin * 2;
            int h = target.Height + margin * 2;
            if (w <= 0 || h <= 0) return;
            if (canvas == null || canvas.Width != w || canvas.Height != h)
            {
                if (canvas != null) canvas.Dispose();
                canvas = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            }
            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                for (int i = 0; i < margin; i++)
                {
                    float inflate = margin - i;
                    RectangleF r = new RectangleF(margin - inflate, margin - inflate + offsetY,
                        target.Width + inflate * 2f, target.Height + inflate * 2f);
                    using (GraphicsPath p = T.RoundPath(r, T.Px(12) + inflate * 0.22f))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(2, 0, 0, 0)))
                    {
                        g.FillPath(b, p);
                    }
                }
                // 中心区域保持透明；弹窗本身会覆盖这里，避免阴影参与命中或遮挡。
                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush clear = new SolidBrush(Color.Transparent))
                {
                    g.FillRectangle(clear, margin, margin, target.Width, target.Height);
                }
                g.CompositingMode = CompositingMode.SourceOver;
            }
            Location = new Point(target.Left - margin, target.Top - margin);
            Size = new Size(w, h);
            ApplyShadowRegion(w, h, margin, offsetY);
            bool applied = ApplyLayeredBitmap(canvas);
            if (!applied && Visible) Hide();
            SetWindowPos(Handle, target.Handle, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
        }

        private void ApplyShadowRegion(int w, int h, int margin, int offsetY)
        {
            // 阴影窗口只保留弹窗四周的环带；中间全部挖空，避免任何透明失败时露出矩形底色。
            using (GraphicsPath p = T.RoundPath(new RectangleF(0, 0, w, h), T.Px(12) + margin * 0.35f))
            using (Region outer = new Region(p))
            {
                outer.Exclude(new Rectangle(margin, margin, target.Width, target.Height));
                Region old = Region;
                Region = (Region)outer.Clone();
                if (old != null) old.Dispose();
            }
        }

        private bool ApplyLayeredBitmap(Bitmap bmp)
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
            IntPtr old = SelectObject(memDc, hBitmap);
            bool applied = false;
            try
            {
                POINT dst = new POINT();
                dst.X = Location.X;
                dst.Y = Location.Y;
                SIZE size = new SIZE();
                size.W = bmp.Width;
                size.H = bmp.Height;
                POINT src = new POINT();
                src.X = 0;
                src.Y = 0;
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.Op = 0;
                blend.Flags = 0;
                blend.Alpha = 255;
                blend.Format = 1;
                applied = UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2);
            }
            finally
            {
                SelectObject(memDc, old);
                DeleteObject(hBitmap);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
            return applied;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style = unchecked((int)0x80000000); // WS_POPUP
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 由 UpdateLayeredWindow 负责绘制
        }
    }

    internal class DarkForm : Form
    {
        private readonly List<Panel> dividers = new List<Panel>();
        private Panel headerBar;
        private Label headerTitleLbl;
        private IconButton headerCloseBtn;
        private bool headerBuilt;
        private Point dragOffset;
        private bool draggingHeader;
        private bool fadeClosing;
        private bool allowClose;
        private bool shownOnce;
        private DialogResult pendingDialogResult = DialogResult.None;
        private ShadowForm shadow;

        /// <summary>统一弹窗标题只显示在顶部标题栏中。</summary>
        public DarkForm()
        {
            T.Init();
            AutoScaleMode = AutoScaleMode.None;
            BackColor = T.Bg;
            ForeColor = T.Text;
            Font = T.FontUi;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            DoubleBuffered = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (shownOnce || IsDisposed) return;
            shownOnce = true;
            if (!UseNativeDwmFrame)
            {
                T.SetRoundedRegion(this, T.Px(12));
                EnsureShadow();
            }
            int targetTop = Top;
            Top = targetTop + T.Px(8);
            Motion.Animate(this, 0f, 1f, 160, delegate(float v)
            {
                if (!IsDisposed && !Disposing) Top = targetTop + (int)Math.Round(T.Px(8) * (1f - v));
            });
        }

        private void EnsureShadow()
        {
            if (UseNativeDwmFrame || shadow != null || IsDisposed) return;
            shadow = new ShadowForm(this);
            shadow.Attach();
            LocationChanged += delegate { if (shadow != null) shadow.UpdateForTarget(); };
            SizeChanged += delegate { if (shadow != null) shadow.UpdateForTarget(); };
            VisibleChanged += delegate { if (shadow != null) shadow.UpdateForTarget(); };
            shadow.UpdateForTarget();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (shadow != null)
            {
                try { shadow.Close(); shadow.Dispose(); } catch (Exception) { }
                shadow = null;
            }
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose && !fadeClosing &&
                e.CloseReason != CloseReason.WindowsShutDown &&
                e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                fadeClosing = true;
                pendingDialogResult = DialogResult;
                int startTop = Top;
                Motion.Animate(this, 0f, 1f, 100, delegate(float v)
                {
                    if (!IsDisposed && !Disposing) Top = startTop + (int)Math.Round(T.Px(6) * v);
                }, delegate
                {
                    allowClose = true;
                    DialogResult = pendingDialogResult;
                    if (!IsDisposed && !Disposing) Close();
                });
                return;
            }
            if (fadeClosing && !allowClose)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        private static readonly bool NativeDwmFrame = DetectWindows11();

        private static bool DetectWindows11()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object build = key.GetValue("CurrentBuildNumber");
                        int n;
                        if (build != null && int.TryParse(build.ToString(), out n)) return n >= 22000;
                    }
                }
            }
            catch (Exception) { }
            Version v = Environment.OSVersion.Version;
            return v.Major > 10 || (v.Major == 10 && v.Build >= 22000);
        }

        private static bool UseNativeDwmFrame
        {
            get { return NativeDwmFrame; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (UseNativeDwmFrame)
                {
                    // 用 caption frame 作为 DWM 圆角/阴影提示；不要 WS_THICKFRAME，
                    // 否则失焦时系统会绘制一圈灰色 resize frame 并反复重绘。
                    cp.Style |= 0x00C00000; // WS_CAPTION
                    cp.Style &= ~0x00080000; // WS_SYSMENU
                }
                return cp;
            }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS
        {
            public RECT rgrc0;
            public RECT rgrc1;
            public RECT rgrc2;
            public IntPtr lppos;
        }

        protected override void WndProc(ref Message m)
        {
            if (UseNativeDwmFrame)
            {
                if (m.Msg == 0x0083) // WM_NCCALCSIZE：激活/失焦都返回 0，避免 NC 区域反复重算导致闪烁
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
                if (m.Msg == 0x0084) // WM_NCHITTEST
                {
                    base.WndProc(ref m);
                    int hit = m.Result.ToInt32();
                    if (hit == 10 || hit == 11 || hit == 12 || hit == 13 ||
                        hit == 14 || hit == 15 || hit == 16 || hit == 17 ||
                        hit == 20 || hit == 21)
                    {
                        m.Result = (IntPtr)1; // HTCLIENT，固定弹窗不拖边缩放
                    }
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (UseNativeDwmFrame)
                {
                    int round = 2; // DWMWCP_ROUND
                    DwmSetWindowAttribute(Handle, 33, ref round, 4);
                    int noBorder = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE
                    DwmSetWindowAttribute(Handle, 34, ref noBorder, 4);
                    int nc = 2; // DWMNCRP_ENABLED
                    DwmSetWindowAttribute(Handle, 2, ref nc, 4);
                    MARGINS margins = new MARGINS();
                    margins.Left = 0;
                    margins.Right = 0;
                    margins.Top = 0;
                    margins.Bottom = 1;
                    DwmExtendFrameIntoClientArea(Handle, ref margins);
                }
                else
                {
                    T.SetRoundedRegion(this, T.Px(12));
                }
            }
            catch (Exception) { }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!UseNativeDwmFrame && IsHandleCreated) T.SetRoundedRegion(this, T.Px(12));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            BuildHeader();
        }

        private void BuildHeader()
        {
            if (headerBuilt) return;
            headerBuilt = true;
            int h = T.Px(52);
            // 先把已有内容整体下移，给统一标题栏/关闭按钮留出空间
            for (int i = 0; i < Controls.Count; i++)
            {
                Control c = Controls[i];
                if (c != null) c.Top += h;
            }
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + h);

            headerBar = new Panel();
            headerBar.BackColor = T.Bg;
            headerBar.SetBounds(0, 0, ClientSize.Width, h);
            Controls.Add(headerBar);
            headerBar.BringToFront();

            headerTitleLbl = UiKit.Label(Text, T.FontTitle, T.Text, T.Bg);
            headerTitleLbl.SetBounds(T.Px(28), T.Px(10), ClientSize.Width - T.Px(28) - T.Px(64), T.Px(32));
            headerTitleLbl.Name = "dlgHeaderTitle";
            headerBar.Controls.Add(headerTitleLbl);

            headerCloseBtn = UiKit.Icon(Ico.Close, L.T("关闭"));
            headerCloseBtn.Name = "btnDlgClose";
            headerCloseBtn.SetBounds(ClientSize.Width - T.Px(10) - T.Px(28), (h - T.Px(28)) / 2, T.Px(28), T.Px(28));
            headerCloseBtn.BackColor = T.Bg;
            headerCloseBtn.SurfaceCustom = true;
            headerCloseBtn.Click += delegate { Close(); };
            headerBar.Controls.Add(headerCloseBtn);

            Panel line = new Panel();
            line.BackColor = T.Border;
            line.SetBounds(0, h - 1, ClientSize.Width, 1);
            headerBar.Controls.Add(line);

            headerBar.MouseDown += HeaderMouseDown;
            headerBar.MouseMove += HeaderMouseMove;
            headerBar.MouseUp += HeaderMouseUp;
            if (headerTitleLbl != null)
            {
                headerTitleLbl.MouseDown += HeaderMouseDown;
                headerTitleLbl.MouseMove += HeaderMouseMove;
                headerTitleLbl.MouseUp += HeaderMouseUp;
            }
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            };
        }

        private void HeaderMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            draggingHeader = true;
            Control c = (Control)sender;
            Point p = c.PointToScreen(e.Location);
            dragOffset = new Point(p.X - Left, p.Y - Top);
        }

        private void HeaderMouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingHeader) return;
            Point p = ((Control)sender).PointToScreen(e.Location);
            Location = new Point(p.X - dragOffset.X, p.Y - dragOffset.Y);
        }

        private void HeaderMouseUp(object sender, MouseEventArgs e)
        {
            draggingHeader = false;
        }

        /// <summary>关闭用于“确认后立即重启”的弹窗，跳过关闭动效以保证重启时序。</summary>
        public void CloseImmediately()
        {
            allowClose = true;
            Close();
        }

        /// <summary>把对话框自身也切到当前主题（用于设置里实时切换外观）。</summary>
        public virtual void ApplyTheme()
        {
            BackColor = T.Bg;
            ForeColor = T.Text;
            for (int i = 0; i < dividers.Count; i++)
            {
                if (dividers[i] != null) dividers[i].BackColor = T.Border;
            }
            if (headerBar != null)
            {
                headerBar.BackColor = T.Bg;
                if (headerTitleLbl != null) headerTitleLbl.ForeColor = T.Text;
                if (headerCloseBtn != null) headerCloseBtn.BackColor = T.Bg;
            }
            T.ApplyTree(this);
            Invalidate(true);
        }

        protected Label Caption(string text, int x, int y, int w)
        {
            Label l = UiKit.Label(text, T.FontSmall, T.Muted, BackColor);
            l.SetBounds(x, y, w, T.Px(18));
            Controls.Add(l);
            return l;
        }

        protected Label SectionTitle(string text, int x, int y, int w)
        {
            Label l = UiKit.Label(text, T.FontSection, T.Text, BackColor);
            l.SetBounds(x, y, w, T.Px(26));
            Controls.Add(l);
            return l;
        }

        protected void Divider(int x, int y, int w)
        {
            Panel p = new Panel();
            p.BackColor = T.Border;
            p.SetBounds(x, y, w, 1);
            Controls.Add(p);
            dividers.Add(p);
        }

        protected FlatButton MakeCancel(int x, int y)
        {
            FlatButton b = UiKit.Ghost(L.T("取消"));
            b.SetBounds(x, y, T.Px(88), T.Px(38));
            b.Name = "btnCancel";
            b.DialogResult = DialogResult.Cancel;
            Controls.Add(b);
            return b;
        }
    }

    internal class PromptDialog : DarkForm
    {
        private readonly DarkInput input;
        private readonly Label error;
        public string Result;

        public PromptDialog(string title, string caption, string initial)
        {
            Text = title;
            ClientSize = new Size(T.Px(480), T.Px(238));
            Caption(caption, T.Px(28), T.Px(24), T.Px(424));

            input = new DarkInput(false);
            input.BackColor = T.Bg;
            input.SetBounds(T.Px(28), T.Px(48), T.Px(424), T.Px(42));
            input.Name = "txtPrompt";
            input.Box.Name = "txtPrompt";
            input.SetAccessibleName(L.T("输入"));
            input.SetText(initial ?? "");
            Controls.Add(input);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(28), T.Px(98), T.Px(424), T.Px(20));
            error.Name = "lblError";
            Controls.Add(error);

            FlatButton cancel = MakeCancel(T.Px(268), T.Px(172));
            FlatButton ok = UiKit.Primary(L.T("确定"));
            ok.SetBounds(T.Px(364), T.Px(172), T.Px(88), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                string v = input.Value.Trim();
                if (v.Length == 0)
                {
                    error.Text = L.T("请输入内容");
                    return;
                }
                Result = v;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);
            AcceptButton = ok;

            input.EnterPressed += delegate { ok.PerformClick(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            input.SelectAllInner();
        }

        public static string Ask(IWin32Window owner, string title, string caption, string initial)
        {
            using (PromptDialog d = new PromptDialog(title, caption, initial))
            {
                if (d.ShowDialog(owner) == DialogResult.OK) return d.Result;
            }
            return null;
        }
    }

    internal class PasswordPromptDialog : DarkForm
    {
        private readonly DarkInput input;
        private readonly Label error;
        public string Result;
        public Func<string, bool> ValidatePassword;

        public PasswordPromptDialog(string title, string desc)
        {
            Text = title;
            ClientSize = new Size(T.Px(480), T.Px(250));

            Label d = UiKit.Label(desc, T.FontSmall, T.Muted, T.Bg);
            d.SetBounds(T.Px(28), T.Px(16), T.Px(424), T.Px(56));
            Controls.Add(d);

            input = new DarkInput(false);
            input.Surface = SurfaceRole.Bg;
            input.PasswordMode = true;
            input.Placeholder = L.T("主密码");
            input.Name = "txtExportPassword";
            input.Box.Name = "txtExportPassword";
            input.SetAccessibleName(L.T("主密码"));
            input.SetBounds(T.Px(28), T.Px(82), T.Px(424), T.Px(44));
            Controls.Add(input);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(28), T.Px(134), T.Px(424), T.Px(20));
            error.Name = "lblError";
            Controls.Add(error);

            FlatButton cancel = MakeCancel(T.Px(230), T.Px(184));
            FlatButton ok = UiKit.Primary(L.T("继续导出"));
            ok.SetBounds(T.Px(328), T.Px(184), T.Px(124), T.Px(40));
            ok.Name = "btnOk";
            ok.Click += delegate { Submit(); };
            Controls.Add(ok);
            AcceptButton = ok;

            input.EnterPressed += delegate { Submit(); };
        }

        private void Submit()
        {
            string v = input.Value;
            if (v.Length == 0)
            {
                error.Text = L.T("请输入主密码");
                return;
            }
            if (ValidatePassword != null && !ValidatePassword(v))
            {
                error.Text = L.T("主密码错误，请重试");
                input.SetText("");
                input.FocusInner();
                return;
            }
            Result = v;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            input.FocusInner();
        }

        public static string Ask(IWin32Window owner, string title, string desc, Func<string, bool> validate)
        {
            using (PasswordPromptDialog d = new PasswordPromptDialog(title, desc))
            {
                d.ValidatePassword = validate;
                if (d.ShowDialog(owner) == DialogResult.OK) return d.Result;
            }
            return null;
        }
    }
    internal class ConfirmDialog : DarkForm
    {
        public ConfirmDialog(string title, string message, string okText, bool danger)
        {
            Text = title;
            ClientSize = new Size(T.Px(470), T.Px(236));

            Label msg = UiKit.Label(message, T.FontUi, T.TextDim, T.Bg);
            msg.SetBounds(T.Px(28), T.Px(28), T.Px(414), T.Px(112));
            Controls.Add(msg);

            MakeCancel(T.Px(258), T.Px(170));
            FlatButton ok = danger ? UiKit.DangerButton(okText) : UiKit.Primary(okText);
            ok.SetBounds(T.Px(354), T.Px(170), T.Px(88), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);
            AcceptButton = ok;
        }

        public static bool Ask(IWin32Window owner, string title, string message, string okText, bool danger)
        {
            using (ConfirmDialog d = new ConfirmDialog(title, message, okText, danger))
            {
                return d.ShowDialog(owner) == DialogResult.OK;
            }
        }
    }

    internal class InfoDialog : DarkForm
    {
        public InfoDialog(string title, string message)
        {
            Text = title;
            ClientSize = new Size(T.Px(470), T.Px(214));

            Label msg = UiKit.Label(message, T.FontUi, T.TextDim, T.Bg);
            msg.SetBounds(T.Px(28), T.Px(26), T.Px(414), T.Px(100));
            Controls.Add(msg);

            FlatButton ok = UiKit.Primary(L.T("知道了"));
            ok.SetBounds(T.Px(354), T.Px(148), T.Px(88), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);
            AcceptButton = ok;
        }

        public static void Show(IWin32Window owner, string title, string message)
        {
            using (InfoDialog d = new InfoDialog(title, message))
            {
                d.ShowDialog(owner);
            }
        }
    }

    internal class ChangePasswordDialog : DarkForm
    {
        private readonly DarkInput pw1;
        private readonly DarkInput pw2;
        private readonly Label error;
        public string NewPassword;

        public ChangePasswordDialog()
        {
            Text = L.T("修改主密码");
            ClientSize = new Size(T.Px(500), T.Px(344));

            Label desc = UiKit.Label(L.T("新密码至少 6 位。修改后用新密码才能解锁，请务必牢记。"), T.FontSmall, T.Muted, T.Bg);
            desc.SetBounds(T.Px(30), T.Px(20), T.Px(440), T.Px(36));
            Controls.Add(desc);

            Caption(L.T("新密码"), T.Px(30), T.Px(64), T.Px(440));
            pw1 = new DarkInput(false);
            pw1.BackColor = T.Bg;
            pw1.PasswordMode = true;
            pw1.Placeholder = L.T("新密码");
            pw1.SetBounds(T.Px(30), T.Px(86), T.Px(440), T.Px(42));
            pw1.Name = "txtNewPw1";
            pw1.Box.Name = "txtNewPw1";
            pw1.SetAccessibleName(L.T("新密码"));
            Controls.Add(pw1);

            Caption(L.T("确认新密码"), T.Px(30), T.Px(142), T.Px(440));
            pw2 = new DarkInput(false);
            pw2.BackColor = T.Bg;
            pw2.PasswordMode = true;
            pw2.Placeholder = L.T("再次输入新密码");
            pw2.SetBounds(T.Px(30), T.Px(164), T.Px(440), T.Px(42));
            pw2.Name = "txtNewPw2";
            pw2.Box.Name = "txtNewPw2";
            pw2.SetAccessibleName(L.T("确认新密码"));
            Controls.Add(pw2);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(30), T.Px(214), T.Px(440), T.Px(20));
            Controls.Add(error);

            MakeCancel(T.Px(236), T.Px(274));
            FlatButton ok = UiKit.Primary(L.T("确定修改"));
            ok.SetBounds(T.Px(334), T.Px(274), T.Px(136), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                string a = pw1.Value;
                string b = pw2.Value;
                if (a.Length < 6)
                {
                    error.Text = L.T("新密码至少 6 位");
                    return;
                }
                if (a != b)
                {
                    error.Text = L.T("两次输入的密码不一致");
                    return;
                }
                NewPassword = a;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(ok);
            AcceptButton = ok;

            pw1.EnterPressed += delegate { pw2.FocusInner(); };
            pw2.EnterPressed += delegate { ok.PerformClick(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            pw1.FocusInner();
        }

        public static string Ask(IWin32Window owner)
        {
            using (ChangePasswordDialog d = new ChangePasswordDialog())
            {
                if (d.ShowDialog(owner) == DialogResult.OK) return d.NewPassword;
            }
            return null;
        }
    }

    internal class EntryDialog : DarkForm
    {
        private const int Pad = 30;

        private readonly List<string> cats = new List<string>();
        private readonly List<string> existingNames;
        private readonly KeyItem editing;
        private readonly List<ChipButton> chips = new List<ChipButton>();
        private readonly List<KeyEditorRow> keyRows = new List<KeyEditorRow>();

        private string selectedCategory = "";
        private ThinScrollView scrollView;
        private Panel contentHost;
        private Panel actionBar;
        private FlowLayoutPanel chipHost;
        private DarkInput txtName;
        private DarkInput txtNote;
        private Panel keysHost;
        private FlatButton btnAddKey;
        private Label lblNameCap;
        private Label lblCatCap;
        private Label lblKeyTitle;
        private Label lblKeyHint;
        private Label lblNoteCap;
        private Label error;
        private FlatButton saveBtn;
        private bool recalcBusy;

        public KeyItem Result;
        public List<string> CategoriesAfter = new List<string>();

        public EntryDialog(List<string> categories, KeyItem editing, string defaultCategory, List<string> existingNames)
        {
            this.editing = editing;
            this.existingNames = existingNames ?? new List<string>();
            if (categories != null) cats.AddRange(categories);
            if (editing != null) selectedCategory = editing.Category ?? "";
            else selectedCategory = defaultCategory ?? "";
            if (selectedCategory.Length > 0 && !cats.Contains(selectedCategory)) cats.Add(selectedCategory);

            Text = editing == null ? L.T("添加 Key") : L.T("编辑 Key");
            ClientSize = new Size(T.Px(660), T.Px(700));

            scrollView = new ThinScrollView();
            scrollView.SetBounds(0, 0, ClientSize.Width, ClientSize.Height - T.Px(92));
            Controls.Add(scrollView);
            contentHost = scrollView.Inner;
            contentHost.BackColor = T.Bg;
            scrollView.Resize += delegate { RecalcLayout(); };


            actionBar = new BufferedPanel();
            actionBar.BackColor = T.Bg;
            actionBar.SetBounds(0, ClientSize.Height - T.Px(92), ClientSize.Width, T.Px(92));
            Controls.Add(actionBar);
            actionBar.BringToFront();

            BuildContent();

            FlatButton cancel = UiKit.Ghost(L.T("取消"));
            cancel.SetBounds(T.Px(360), T.Px(26), T.Px(88), T.Px(40));
            cancel.Name = "btnCancel";
            cancel.DialogResult = DialogResult.Cancel;
            actionBar.Controls.Add(cancel);

            saveBtn = UiKit.Primary(L.T("保存"));
            saveBtn.SetBounds(T.Px(462), T.Px(26), T.Px(84), T.Px(40));
            saveBtn.Name = "btnSave";
            saveBtn.Click += delegate { OnSave(); };
            actionBar.Controls.Add(saveBtn);
            AcceptButton = saveBtn;
            CancelButton = cancel;

            if (editing != null)
            {
                txtName.SetText(editing.Name);
                txtNote.SetText(editing.Note);
                if (editing.Keys != null && editing.Keys.Count > 0)
                {
                    for (int i = 0; i < editing.Keys.Count; i++)
                    {
                        SubKey k = editing.Keys[i];
                        AddKeyRow(k.Id, k.Label, k.Value, k.Id == editing.DefaultKeyId, true);
                    }
                }
            }
            if (keyRows.Count == 0) AddKeyRow(Vault.NewId(), "", "", true);

            txtName.EnterPressed += delegate { if (keyRows.Count > 0) keyRows[0].ValueInput.FocusInner(); };
            txtNote.EnterPressed += delegate { OnSave(); };
            RecalcLayout();
        }


        private void BuildContent()
        {
            lblNameCap = UiKit.Label(L.T("名称"), T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblNameCap);

            txtName = new DarkInput(false);
            txtName.Surface = SurfaceRole.Bg;
            txtName.Placeholder = L.T("例如：OpenAI API");
            txtName.Name = "txtName";
            txtName.Box.Name = "txtName";
            txtName.SetAccessibleName(L.T("名称"));
            contentHost.Controls.Add(txtName);

            lblCatCap = UiKit.Label(L.T("分类"), T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblCatCap);

            chipHost = new FlowLayoutPanel();
            chipHost.BackColor = T.Bg;
            chipHost.WrapContents = true;
            chipHost.FlowDirection = FlowDirection.LeftToRight;
            contentHost.Controls.Add(chipHost);
            BuildChips();

            lblKeyTitle = UiKit.Label(L.T("Key 列表"), T.FontSection, T.Text, T.Bg);
            contentHost.Controls.Add(lblKeyTitle);

            lblKeyHint = UiKit.Label(L.T("可添加多条；点 ☆ 设为默认，列表只显示默认 Key"), T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblKeyHint);

            keysHost = new BufferedPanel();
            keysHost.BackColor = T.Bg;
            contentHost.Controls.Add(keysHost);

            btnAddKey = UiKit.Ghost(L.T("＋ 添加一条 Key"));
            btnAddKey.Name = "btnAddKey";
            btnAddKey.Click += delegate { AddKeyRow(Vault.NewId(), "", "", false); };
            contentHost.Controls.Add(btnAddKey);

            lblNoteCap = UiKit.Label(L.T("备注（可选）"), T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblNoteCap);

            txtNote = new DarkInput(false);
            txtNote.Surface = SurfaceRole.Bg;
            txtNote.Placeholder = L.T("这个条目的用途、项目等");
            txtNote.Name = "txtNote";
            txtNote.Box.Name = "txtNote";
            txtNote.SetAccessibleName(L.T("备注"));
            contentHost.Controls.Add(txtNote);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.Name = "lblError";
            contentHost.Controls.Add(error);
        }

        private void RecalcLayout()
        {
            if (contentHost == null || recalcBusy) return;
            recalcBusy = true;
            contentHost.SuspendLayout();
            try
            {
                int w = scrollView.ClientSize.Width - T.Px(12) - T.Px(Pad) * 2;
                if (w < T.Px(300)) w = T.Px(300);

                int rowH = T.Px(98);
                int gap = T.Px(8);
                int keysH = keyRows.Count * rowH + Math.Max(0, keyRows.Count - 1) * gap + T.Px(2);
                for (int i = 0; i < keyRows.Count; i++)
                {
                    keyRows[i].SetBounds(0, i * (rowH + gap), w, rowH);
                }
                keysHost.SetBounds(T.Px(Pad), 0, w, keysH);

                int y = T.Px(20);
                lblNameCap.SetBounds(T.Px(Pad), y, w, T.Px(18)); y += T.Px(22);
                txtName.SetBounds(T.Px(Pad), y, w, T.Px(42)); y += T.Px(56);
                lblCatCap.SetBounds(T.Px(Pad), y, w, T.Px(18)); y += T.Px(22);
                chipHost.SetBounds(T.Px(Pad), y, w, T.Px(76)); y += T.Px(76) + T.Px(12);
                lblKeyTitle.SetBounds(T.Px(Pad), y, T.Px(110), T.Px(26));
                lblKeyHint.SetBounds(T.Px(Pad) + T.Px(112), y + T.Px(4), w - T.Px(112), T.Px(20));
                y += T.Px(30);
                keysHost.Top = y; y += keysH + T.Px(8);
                btnAddKey.SetBounds(T.Px(Pad), y, T.Px(160), T.Px(34)); y += T.Px(34) + T.Px(16);
                lblNoteCap.SetBounds(T.Px(Pad), y, w, T.Px(18)); y += T.Px(22);
                txtNote.SetBounds(T.Px(Pad), y, w, T.Px(42)); y += T.Px(52);
                error.SetBounds(T.Px(Pad), y, w, T.Px(20)); y += T.Px(28);

                contentHost.Height = y;
                scrollView.SetContentHeight(y);
            }
            finally
            {
                contentHost.ResumeLayout(false);
                recalcBusy = false;
            }
        }

        private void AddKeyRow(string keyId, string label, string value, bool makeDefault)
        {
            AddKeyRow(keyId, label, value, makeDefault, false);
        }

        private void AddKeyRow(string keyId, string label, string value, bool makeDefault, bool deferLayout)
        {
            KeyEditorRow row = new KeyEditorRow(keyId, label, value);
            row.DefaultRequested = delegate(KeyEditorRow r) { SetDefaultRow(r); };
            row.DeleteRequested = delegate(KeyEditorRow r) { DeleteKeyRow(r); };
            keyRows.Add(row);
            keysHost.Controls.Add(row);
            if (keyRows.Count == 1) makeDefault = true;
            if (makeDefault) SetDefaultRow(row);
            if (!deferLayout) RecalcLayout();
        }

        private void SetDefaultRow(KeyEditorRow target)
        {
            for (int i = 0; i < keyRows.Count; i++)
            {
                keyRows[i].SetDefault(keyRows[i] == target);
            }
            if (target != null) target.Invalidate();
        }

        private void DeleteKeyRow(KeyEditorRow row)
        {
            if (keyRows.Count <= 1)
            {
                error.Text = L.T("至少保留一条 Key");
                return;
            }
            bool wasDefault = row.IsDefault;
            keyRows.Remove(row);
            keysHost.Controls.Remove(row);
            row.Dispose();
            if (wasDefault && keyRows.Count > 0) SetDefaultRow(keyRows[0]);
            RecalcLayout();
        }

        private void BuildChips()
        {
            chipHost.SuspendLayout();
            for (int i = 0; i < chips.Count; i++)
            {
                chipHost.Controls.Remove(chips[i]);
                chips[i].Dispose();
            }
            chips.Clear();
            AddChip(L.T("未分类"), "", false);
            for (int i = 0; i < cats.Count; i++)
            {
                string cat = cats[i];
                AddChip(cat, cat, cat == selectedCategory);
            }
            ChipButton add = new ChipButton(L.T("＋ 新建分类"), null, false);
            add.IsAction = true;
            add.Name = "chipNew";
            add.BackColor = T.Bg;
            add.Margin = new Padding(0, 0, T.Px(8), T.Px(8));
            add.Click += delegate { CreateCategory(); };
            chipHost.Controls.Add(add);
            chips.Add(add);
            chipHost.ResumeLayout();
        }

        private void AddChip(string text, string value, bool selected)
        {
            ChipButton c = new ChipButton(text, value, selected);
            c.Name = "chip_" + (value == "" ? "__NONE" : value);
            c.BackColor = T.Bg;
            c.Margin = new Padding(0, 0, T.Px(8), T.Px(8));
            c.Click += delegate
            {
                selectedCategory = c.ChipValue;
                UpdateChips();
            };
            chipHost.Controls.Add(c);
            chips.Add(c);
        }

        private void UpdateChips()
        {
            for (int i = 0; i < chips.Count; i++)
            {
                ChipButton c = chips[i];
                if (!c.IsAction)
                {
                    c.IsSelected = (c.ChipValue == selectedCategory);
                    c.Invalidate();
                }
            }
        }

        private void CreateCategory()
        {
            string name = PromptDialog.Ask(this, L.T("新建分类"), L.T("分类名称"), "");
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;
            if (!cats.Contains(name)) cats.Add(name);
            selectedCategory = name;
            BuildChips();
        }

        private void OnSave()
        {
            string name = txtName.Value.Trim();
            if (name.Length == 0)
            {
                error.Text = L.T("请输入名称");
                txtName.FocusInner();
                return;
            }
            for (int i = 0; i < existingNames.Count; i++)
            {
                string n = existingNames[i];
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                {
                    bool self = editing != null && string.Equals(editing.Name, name, StringComparison.OrdinalIgnoreCase);
                    if (!self)
                    {
                        error.Text = L.T("已存在同名 Key，请换一个名称");
                        txtName.FocusInner();
                        return;
                    }
                }
            }

            List<SubKey> keys = new List<SubKey>();
            string defaultId = null;
            for (int i = 0; i < keyRows.Count; i++)
            {
                KeyEditorRow row = keyRows[i];
                string value = row.KeyValue;
                if (value.Length == 0) continue;
                SubKey k = new SubKey();
                k.Id = row.KeyId;
                k.Label = row.LabelValue;
                k.Value = value;
                keys.Add(k);
                if (row.IsDefault) defaultId = k.Id;
            }
            if (keys.Count == 0)
            {
                error.Text = L.T("至少填写一条 Key 内容");
                if (keyRows.Count > 0) keyRows[0].ValueInput.FocusInner();
                return;
            }
            bool defaultValid = false;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Id == defaultId) { defaultValid = true; break; }
            }
            if (!defaultValid) defaultId = keys[0].Id;

            KeyItem it;
            if (editing == null)
            {
                it = new KeyItem();
                it.Id = Vault.NewId();
                it.Created = Vault.NowMs();
            }
            else
            {
                it = new KeyItem();
                it.Id = editing.Id;
                it.Created = editing.Created;
            }
            it.Name = name;
            it.Category = selectedCategory;
            it.Value = "";
            it.Note = txtNote.Value.Trim();
            it.Updated = Vault.NowMs();
            it.Keys = keys;
            it.DefaultKeyId = defaultId;
            Result = it;
            CategoriesAfter = new List<string>(cats);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtName.FocusInner();
        }

    }

    internal class SettingsDialog : DarkForm
    {
        private readonly VaultData vault;
        private readonly string dataPath;
        private readonly ThinScrollView catScroll;
        private readonly Panel catHost;
        private readonly List<CategoryRow> catRows = new List<CategoryRow>();
        private string selectedCategoryName;
        private readonly List<ChipButton> lockChips = new List<ChipButton>();
        private readonly List<ChipButton> themeChips = new List<ChipButton>();
        private readonly List<ChipButton> langChips = new List<ChipButton>();
        private readonly Label pwState;
        private readonly Label pathLbl;
        private readonly Action<ThemeMode> onThemeMode;
        private readonly Action onResetWindow;
        private ThemeMode themeMode;

        public bool Changed;
        public bool PasswordChanged;
        public bool LanguageChanged;
        public bool RestartRequested;
        public string NewPassword;

        private static readonly int[] LockValues = new int[] { 0, 1, 5, 15, 30 };
        private static readonly string[] LockTexts = new string[] { L.T("关闭"), L.T("1 分钟"), L.T("5 分钟"), L.T("15 分钟"), L.T("30 分钟") };

        public SettingsDialog(VaultData vault, string dataPath, ThemeMode themeMode, Action<ThemeMode> onThemeMode, Action onResetWindow)
        {
            this.vault = vault;
            this.dataPath = dataPath;
            this.themeMode = themeMode;
            this.onThemeMode = onThemeMode;
            this.onResetWindow = onResetWindow;
            Text = L.T("设置");
            ClientSize = new Size(T.Px(660), T.Px(740));

            int left = T.Px(30);
            int width = ClientSize.Width - left * 2;

            int langX = ClientSize.Width - T.Px(30) - T.Px(170);
            Label sub = UiKit.Label(L.T("分类、安全与数据存储"), T.FontSmall, T.Muted, T.Bg);
            sub.SetBounds(left + T.Px(1), T.Px(52), Math.Max(T.Px(200), langX - left - T.Px(14)), T.Px(20));
            Controls.Add(sub);

            ChipButton zhChip = new ChipButton(L.T("中文"), "zh", AppConfig.Language != "en");
            zhChip.SetBounds(langX, T.Px(20), T.Px(78), T.Px(32));
            zhChip.Name = "chipLangZh";
            zhChip.Click += delegate { SetLanguage("zh"); };
            Controls.Add(zhChip);
            langChips.Add(zhChip);
            ChipButton enChip = new ChipButton("English", "en", AppConfig.Language == "en");
            enChip.SetBounds(langX + T.Px(84), T.Px(20), T.Px(86), T.Px(32));
            enChip.Name = "chipLangEn";
            enChip.Click += delegate { SetLanguage("en"); };
            Controls.Add(enChip);
            langChips.Add(enChip);
            Label langHint = UiKit.Label(L.T("重启后生效"), T.FontSmall, T.Muted, T.Bg);
            langHint.SetBounds(langX, T.Px(56), T.Px(170), T.Px(18));
            langHint.Name = "lblLangHint";
            Controls.Add(langHint);

            SectionTitle(L.T("分类管理"), left, T.Px(88), width);

            Label catHint = UiKit.Label(L.T("点击 ↑ ↓ 调整顺序"), T.FontSmall, T.Muted, T.Bg);
            catHint.SetBounds(left + width - T.Px(170), T.Px(91), T.Px(170), T.Px(20));
            catHint.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(catHint);

            catScroll = new ThinScrollView();
            catScroll.BackColor = T.Bg;
            catScroll.Name = "catScroll";
            catScroll.SetBounds(left, T.Px(116), T.Px(390), T.Px(152));
            catScroll.Resize += delegate { LayoutCatRows(); };
            Controls.Add(catScroll);
            catHost = catScroll.Inner;
            catHost.BackColor = T.Bg;
            catHost.Name = "catHost";

            FlatButton bNew = UiKit.Ghost(L.T("＋ 新建分类"));
            bNew.SetBounds(T.Px(438), T.Px(118), T.Px(192), T.Px(36));
            bNew.Name = "btnNewCategory";
            bNew.Click += delegate { NewCategory(); };
            Controls.Add(bNew);

            FlatButton bRename = UiKit.Ghost(L.T("重命名"));
            bRename.SetBounds(T.Px(438), T.Px(162), T.Px(192), T.Px(36));
            bRename.Name = "btnRenameCategory";
            bRename.Click += delegate { RenameCategory(); };
            Controls.Add(bRename);

            FlatButton bDelete = UiKit.Ghost(L.T("删除分类"));
            bDelete.SetBounds(T.Px(438), T.Px(206), T.Px(192), T.Px(36));
            bDelete.TextColor = T.Danger;
            bDelete.Name = "btnDeleteCategory";
            bDelete.Click += delegate { DeleteCategory(); };
            Controls.Add(bDelete);

            Divider(left, T.Px(278), width);

            SectionTitle(L.T("安全"), left, T.Px(298), width);
            Caption(L.T("自动锁定（无操作超时）"), left, T.Px(326), width);

            int cx = left;
            for (int i = 0; i < LockValues.Length; i++)
            {
                int val = LockValues[i];
                ChipButton chip = new ChipButton(LockTexts[i], val.ToString(), vault.AutoLockMinutes == val);
                chip.Name = "lock_" + val;
                chip.BackColor = T.Bg;
                chip.SetBounds(cx, T.Px(350), chip.Width, T.Px(32));
                chip.Click += delegate
                {
                    vault.AutoLockMinutes = val;
                    Changed = true;
                    RefreshLockChips();
                };
                Controls.Add(chip);
                lockChips.Add(chip);
                cx += chip.Width + T.Px(8);
            }

            FlatButton bPw = UiKit.Ghost(L.T("修改主密码…"));
            bPw.SetBounds(left, T.Px(394), T.Px(180), T.Px(36));
            bPw.Name = "btnChangePassword";
            bPw.Click += delegate { ChangePassword(); };
            Controls.Add(bPw);

            pwState = UiKit.Label("", T.FontSmall, T.Success, T.Bg);
            pwState.SetBounds(left + T.Px(194), T.Px(402), T.Px(400), T.Px(20));
            Controls.Add(pwState);

            Divider(left, T.Px(444), width);

            SectionTitle(L.T("外观"), left, T.Px(462), width);
            ThemeMode[] themeModes = new ThemeMode[] { ThemeMode.System, ThemeMode.Light, ThemeMode.Dark };
            string[] themeTexts = new string[] { L.T("跟随系统"), L.T("浅色"), L.T("深色") };
            int tx = left;
            for (int i = 0; i < themeModes.Length; i++)
            {
                ThemeMode mode = themeModes[i];
                ChipButton chip = new ChipButton(themeTexts[i], ((int)mode).ToString(), mode == themeMode);
                chip.Name = "theme_" + ((int)mode);
                chip.BackColor = T.Bg;
                chip.SetBounds(tx, T.Px(490), chip.Width, T.Px(32));
                chip.Click += delegate
                {
                    themeMode = mode;
                    if (onThemeMode != null) onThemeMode(mode);
                    ApplyTheme();
                    for (int ci = 0; ci < themeChips.Count; ci++)
                    {
                        ChipButton c = themeChips[ci];
                        c.IsSelected = (c.ChipValue == ((int)mode).ToString());
                        c.Invalidate();
                    }
                };
                Controls.Add(chip);
                themeChips.Add(chip);
                tx += chip.Width + T.Px(8);
            }

            FlatButton bResetWindow = UiKit.Ghost(L.T("恢复默认窗口大小"));
            bResetWindow.SetBounds(ClientSize.Width - left - T.Px(190), T.Px(490), T.Px(190), T.Px(32));
            bResetWindow.Name = "btnResetWindow";
            bResetWindow.TextColor = T.TextDim;
            bResetWindow.Click += delegate
            {
                if (onResetWindow != null) onResetWindow();
            };
            Controls.Add(bResetWindow);
            bResetWindow.BringToFront();

            Divider(left, T.Px(540), width);

            SectionTitle(L.T("数据存储"), left, T.Px(558), width);
            pathLbl = UiKit.Label(dataPath, T.FontMonoSm, T.Muted, T.Bg);
            pathLbl.SetBounds(left, T.Px(586), width, T.Px(38));
            pathLbl.Name = "lblDataPath";
            Controls.Add(pathLbl);

            FlatButton bOpen = UiKit.Ghost(L.T("打开数据文件夹"));
            bOpen.SetBounds(left, T.Px(628), T.Px(180), T.Px(36));
            bOpen.Name = "btnOpenFolder";
            bOpen.Click += delegate { OpenFolder(); };
            Controls.Add(bOpen);

            Label about = UiKit.Label(AppInfo.Version + " · GitHub", T.FontSmall, T.Muted, T.Bg);
            about.SetBounds(left, ClientSize.Height - T.Px(58), T.Px(280), T.Px(20));
            about.Cursor = Cursors.Hand;
            about.Name = "lblSettingsAbout";
            about.Click += delegate { AppInfo.OpenRepo(); };
            Controls.Add(about);

            FlatButton done = UiKit.Primary(L.T("完成"));            done.SetBounds(ClientSize.Width - T.Px(30) - T.Px(96), ClientSize.Height - T.Px(28) - T.Px(40), T.Px(96), T.Px(40));
            done.Name = "btnDone";
            done.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(done);
            AcceptButton = done;
            CancelButton = done;

            RefillCatList(null);
        }

        public override void ApplyTheme()
        {
            base.ApplyTheme();
            if (catScroll != null)
            {
                catScroll.BackColor = T.Bg;
                catHost.BackColor = T.Bg;
                catScroll.Invalidate();
            }
            UpdateThemeChips();
        }

        private void UpdateThemeChips()
        {
            for (int i = 0; i < themeChips.Count; i++)
            {
                ChipButton chip = themeChips[i];
                chip.IsSelected = (chip.ChipValue == ((int)themeMode).ToString());
                chip.Invalidate();
            }
        }

        private void SetLanguage(string lang)
        {
            if (lang == AppConfig.Language) return;
            string previous = AppConfig.Language;
            // 先在目标语言下询问，保证点到 English 就是英文确认框
            AppConfig.Language = lang;
            UpdateLanguageChips(lang);
            bool restart = ConfirmDialog.Ask(this, L.T("应用语言"),
                L.T("语言设置在重启应用后生效。是否立即重启？"),
                L.T("立即重启"), false);
            if (!restart)
            {
                AppConfig.Language = previous;
                UpdateLanguageChips(previous);
                LanguageChanged = false;
                return;
            }
            LanguageChanged = true;
            RestartRequested = true;
            AppConfig.Save();
            DialogResult = DialogResult.OK;
            CloseImmediately();
        }

        private void UpdateLanguageChips(string lang)
        {
            for (int i = 0; i < langChips.Count; i++)
            {
                langChips[i].IsSelected = (langChips[i].ChipValue == lang);
                langChips[i].Invalidate();
            }
        }

        private void RefillCatList(string selectName)
        {
            if (selectName != null && vault.Categories.Contains(selectName)) selectedCategoryName = selectName;
            if (selectedCategoryName != null && !vault.Categories.Contains(selectedCategoryName)) selectedCategoryName = null;
            if (selectedCategoryName == null && vault.Categories.Count > 0) selectedCategoryName = vault.Categories[0];

            catHost.SuspendLayout();
            try
            {
                for (int i = 0; i < catRows.Count; i++)
                {
                    catHost.Controls.Remove(catRows[i]);
                    catRows[i].Dispose();
                }
                catRows.Clear();
                for (int i = 0; i < vault.Categories.Count; i++)
                {
                    string cat = vault.Categories[i];
                    CategoryRow row = new CategoryRow(cat, CountItems(cat), cat == selectedCategoryName,
                        i > 0, i < vault.Categories.Count - 1);
                    row.SelectRequested = delegate(string name)
                    {
                        selectedCategoryName = name;
                        UpdateCatSelectionRows();
                    };
                    row.MoveRequested = delegate(string name, int delta)
                    {
                        int index = vault.Categories.IndexOf(name);
                        MoveCategory(index, delta);
                    };
                    catHost.Controls.Add(row);
                    catRows.Add(row);
                }
                LayoutCatRows();
            }
            finally
            {
                catHost.ResumeLayout(false);
            }
        }

        private void LayoutCatRows()
        {
            if (catScroll == null || catHost == null) return;
            int w = Math.Max(T.Px(120), catScroll.ClientSize.Width - T.Px(14));
            int y = 0;
            for (int i = 0; i < catRows.Count; i++)
            {
                catRows[i].SetBounds(0, y, w, T.Px(34));
                y += T.Px(34) + T.Px(2);
            }
            catScroll.SetContentHeight(y + T.Px(2));
        }

        private void UpdateCatSelectionRows()
        {
            for (int i = 0; i < catRows.Count; i++)
            {
                catRows[i].Selected = (catRows[i].Category == selectedCategoryName);
                catRows[i].Invalidate();
            }
        }

        private class CategoryRow : Panel, IThemed
        {
            public readonly string Category;
            public bool Selected;
            public Action<string> SelectRequested;
            public Action<string, int> MoveRequested;

            private readonly bool canUp;
            private readonly bool canDown;
            private readonly int count;
            private bool hover;

            public CategoryRow(string category, int count, bool selected, bool canUp, bool canDown)
            {
                Category = category;
                this.count = count;
                Selected = selected;
                this.canUp = canUp;
                this.canDown = canDown;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                BackColor = T.Bg;
                Cursor = Cursors.Hand;
                Height = T.Px(34);
                MouseEnter += delegate { hover = true; Invalidate(); };
                MouseLeave += delegate { hover = false; Invalidate(); };
                MouseUp += delegate(object s, MouseEventArgs e)
                {
                    if (e.Button != MouseButtons.Left) return;
                    int upX = T.Px(8);
                    int downX = T.Px(34);
                    int w = T.Px(24);
                    if (canUp && e.X >= upX && e.X <= upX + w)
                    {
                        if (MoveRequested != null) MoveRequested(Category, -1);
                    }
                    else if (canDown && e.X >= downX && e.X <= downX + w)
                    {
                        if (MoveRequested != null) MoveRequested(Category, 1);
                    }
                    else if (SelectRequested != null)
                    {
                        SelectRequested(Category);
                    }
                };
            }

            public void ApplyTheme()
            {
                BackColor = T.Bg;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                T.Prepare(g);
                g.Clear(BackColor);
                RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
                Color fill = Selected ? T.AccentSoft : T.Mix(T.Field, T.CardHover, hover ? 0.5f : 0f);
                T.FillRound(g, fill, r, T.Px(8));
                Color upColor = canUp ? (Selected ? T.Accent : T.TextDim) : T.Muted;
                Color downColor = canDown ? (Selected ? T.Accent : T.TextDim) : T.Muted;
                Rectangle upR = new Rectangle(T.Px(8), 0, T.Px(24), Height);
                Rectangle downR = new Rectangle(T.Px(34), 0, T.Px(24), Height);
                TextRenderer.DrawText(g, "↑", T.FontSmall, upR, upColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, "↓", T.FontSmall, downR, downColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Rectangle tr = new Rectangle(T.Px(62), 0, Math.Max(T.Px(40), Width - T.Px(62) - T.Px(56)), Height);
                TextRenderer.DrawText(g, Category, T.FontUi, tr, Selected ? T.Accent : T.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                Rectangle cr = new Rectangle(Width - T.Px(52), 0, T.Px(38), Height);
                TextRenderer.DrawText(g, count.ToString(), T.FontSmall, cr, T.Muted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }

        private void MoveCategory(int index, int delta)
        {
            int to = index + delta;
            if (index < 0 || to < 0 || to >= vault.Categories.Count) return;
            string name = vault.Categories[index];
            vault.Categories.RemoveAt(index);
            vault.Categories.Insert(to, name);
            Changed = true;
            RefillCatList(name);
        }


        private int CountItems(string category)
        {
            int n = 0;
            for (int i = 0; i < vault.Items.Count; i++)
            {
                if (vault.Items[i].Category == category) n++;
            }
            return n;
        }

        private string SelectedCategory()
        {
            if (selectedCategoryName == null || !vault.Categories.Contains(selectedCategoryName)) return null;
            return selectedCategoryName;
        }

        private void NewCategory()
        {
            string name = PromptDialog.Ask(this, L.T("新建分类"), L.T("分类名称"), "");
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;
            if (vault.Categories.Contains(name))
            {
                InfoDialog.Show(this, L.T("提示"), L.T("这个分类已经存在了。"));
                return;
            }
            vault.Categories.Add(name);
            Changed = true;
            RefillCatList(name);
        }

        private void RenameCategory()
        {
            string oldName = SelectedCategory();
            if (oldName == null)
            {
                InfoDialog.Show(this, L.T("提示"), L.T("请先在左侧选择一个分类。"));
                return;
            }
            string name = PromptDialog.Ask(this, L.T("重命名分类"), L.T("新的分类名称"), oldName);
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0 || name == oldName) return;
            if (vault.Categories.Contains(name))
            {
                InfoDialog.Show(this, L.T("提示"), L.T("这个分类名称已经存在了。"));
                return;
            }
            int idx = vault.Categories.IndexOf(oldName);
            if (idx >= 0) vault.Categories[idx] = name;
            for (int i = 0; i < vault.Items.Count; i++)
            {
                if (vault.Items[i].Category == oldName) vault.Items[i].Category = name;
            }
            Changed = true;
            RefillCatList(name);
        }

        private void DeleteCategory()
        {
            string name = SelectedCategory();
            if (name == null)
            {
                InfoDialog.Show(this, L.T("提示"), L.T("请先在左侧选择一个分类。"));
                return;
            }
            int count = CountItems(name);
            string msg = L.T("确定删除分类「") + name + L.T("」吗？");
            if (count > 0)
            {
                if (L.IsEn) msg += "\r\n";
                msg += L.T("该分类下的 ") + L.KeyCount(count) + L.T(" 会变为「未分类」，Key 本身不会被删除。");
            }
            if (!ConfirmDialog.Ask(this, L.T("删除分类"), msg, L.T("删除"), true)) return;
            vault.Categories.Remove(name);
            for (int i = 0; i < vault.Items.Count; i++)
            {
                if (vault.Items[i].Category == name) vault.Items[i].Category = "";
            }
            Changed = true;
            RefillCatList(null);
        }

        private void ChangePassword()
        {
            string pw = ChangePasswordDialog.Ask(this);
            if (pw == null) return;
            NewPassword = pw;
            PasswordChanged = true;
            pwState.Text = L.T("新密码已设置，点「完成」后生效");
        }

        private void RefreshLockChips()
        {
            for (int i = 0; i < lockChips.Count; i++)
            {
                ChipButton c = lockChips[i];
                int val = int.Parse(c.ChipValue);
                c.IsSelected = (vault.AutoLockMinutes == val);
                c.Invalidate();
            }
        }

        private void OpenFolder()
        {
            try
            {
                string folder = Path.GetDirectoryName(dataPath);
                if (!string.IsNullOrEmpty(folder))
                {
                    Process.Start("explorer.exe", "/select,\"" + dataPath + "\"");
                }
            }
            catch (Exception ex)
            {
                InfoDialog.Show(this, L.T("找不到文件夹"), ex.Message);
            }
        }
    }
}
