using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KeyCenter
{
    internal enum ButtonStyle
    {
        Custom,
        Primary,
        Ghost,
        Danger
    }

    /// <summary>支持明暗主题切换的文本标签。</summary>
    /// <summary>开启双缓冲的普通容器，减少 resize/还原时的黑块和闪烁。</summary>
    internal class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
        }
    }
    internal class ThemedLabel : Label, IThemed
    {
        public LabelTone Tone = LabelTone.Text;
        public SurfaceRole Surface = SurfaceRole.Bg;
        public bool SurfaceCustom = false;

        public void ApplyTheme()
        {
            if (Tone != LabelTone.Custom) ForeColor = T.ToneColor(Tone);
            if (!SurfaceCustom) BackColor = T.RoleColor(Surface);
            Invalidate();
        }
    }

    internal static class UiKit
    {
        public static Label Label(string text, Font font, Color fore, Color back)
        {
            ThemedLabel l = new ThemedLabel();
            l.AutoSize = false;
            l.Text = text;
            l.Font = font;
            LabelTone tone;
            if (T.TryGetTone(fore, out tone)) l.Tone = tone;
            else l.Tone = LabelTone.Custom;
            SurfaceRole role;
            if (T.TryGetSurface(back, out role)) l.Surface = role;
            else l.SurfaceCustom = true;
            l.ApplyTheme();
            if (l.Tone == LabelTone.Custom) l.ForeColor = fore;
            if (l.SurfaceCustom) l.BackColor = back;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.UseMnemonic = false;
            return l;
        }

        public static FlatButton Primary(string text)
        {
            FlatButton b = new FlatButton();
            b.Style = ButtonStyle.Primary;
            b.Text = text;
            b.Font = T.FontUi;
            b.Radius = T.Px(9);
            b.ApplyTheme();
            return b;
        }

        public static FlatButton Ghost(string text)
        {
            FlatButton b = new FlatButton();
            b.Style = ButtonStyle.Ghost;
            b.Text = text;
            b.Font = T.FontUi;
            b.Radius = T.Px(9);
            b.ApplyTheme();
            return b;
        }

        public static FlatButton DangerButton(string text)
        {
            FlatButton b = new FlatButton();
            b.Style = ButtonStyle.Danger;
            b.Text = text;
            b.Font = T.FontUi;
            b.Radius = T.Px(9);
            b.ApplyTheme();
            return b;
        }

        public static IconButton Icon(string glyph, string tooltip)
        {
            IconButton b = new IconButton(glyph, tooltip);
            return b;
        }

        public static IconButton Icon(string glyph, string tooltip, bool small)
        {
            IconButton b = new IconButton(glyph, tooltip);
            b.SmallIcon = small;
            return b;
        }

        public static void CenterIn(Control child, Control parent)
        {
            child.Left = (parent.ClientSize.Width - child.Width) / 2;
        }

        public static void SetBack(Control c, Color color)
        {
            c.BackColor = color;
            for (int i = 0; i < c.Controls.Count; i++) SetBack(c.Controls[i], color);
        }
    }

    internal class FlatButton : Button, IThemed
    {
        public int Radius = 9;
        public Color FillColor = T.PanelDark;
        public Color HoverColor = T.CardHover;
        public Color PressColor = T.Card;
        public Color TextColor = T.TextDim;
        public bool HasBorder = false;
        public Color BorderColor = T.Border;
        public ButtonStyle Style = ButtonStyle.Custom;
        public SurfaceRole Surface = SurfaceRole.Bg;
        public bool SurfaceCustom = false;
        public bool SidebarFill = false;

        protected bool hover;
        protected bool pressed;
        private float hoverT;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            TabStop = false;
            Size = new Size(T.Px(90), T.Px(36));
            BackColor = T.Bg;
            ForeColor = T.Text;
            Font = T.FontUi;
            Cursor = Cursors.Hand;
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (!SurfaceCustom) BackColor = T.RoleColor(Surface);
            if (Style == ButtonStyle.Primary)
            {
                FillColor = T.Accent;
                HoverColor = T.AccentHover;
                PressColor = T.AccentDeep;
                TextColor = T.OnAccent;
                HasBorder = false;
            }
            else if (Style == ButtonStyle.Ghost)
            {
                FillColor = SidebarFill ? T.Sidebar : T.PanelDark;
                HoverColor = T.CardHover;
                PressColor = T.Card;
                TextColor = T.TextDim;
                HasBorder = true;
                BorderColor = T.Border;
            }
            else if (Style == ButtonStyle.Danger)
            {
                FillColor = Color.FromArgb(222, 68, 68);
                HoverColor = Color.FromArgb(240, 84, 84);
                PressColor = Color.FromArgb(190, 52, 52);
                TextColor = Color.White;
                HasBorder = false;
            }
            Invalidate();
        }

        protected virtual bool UseHoverAnimation
        {
            get { return true; }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            if (UseHoverAnimation)
            {
                Motion.Animate(this, hoverT, 1f, 110, delegate(float v) { hoverT = v; Invalidate(); });
            }
            else
            {
                hoverT = 1f;
                Invalidate();
            }
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            pressed = false;
            if (UseHoverAnimation)
            {
                Motion.Animate(this, hoverT, 0f, 130, delegate(float v) { hoverT = v; Invalidate(); });
            }
            else
            {
                hoverT = 0f;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Color fill = FillColor;
            if (!Enabled) fill = T.Mix(fill, BackColor, 0.55f);
            else if (pressed) fill = PressColor;
            else fill = T.Mix(FillColor, HoverColor, hoverT);
            if (Style == ButtonStyle.Primary && Enabled)
            {
                Color sh = Color.FromArgb(46, T.Accent.R, T.Accent.G, T.Accent.B);
                T.FillRound(g, sh, new RectangleF(1f, 2.5f, Width - 2f, Height - 1f), Radius);
            }
            if (fill.A > 0) T.FillRound(g, fill, r, Radius);
            if (HasBorder)
            {
                Color bc = T.Mix(BorderColor, T.BorderHi, hoverT);
                T.DrawRound(g, bc, r, Radius, 1f);
            }
            Color txt = Enabled ? TextColor : T.Muted;
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), txt,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }

    internal class GhostButton : FlatButton
    {
    }

    internal class IconButton : FlatButton
    {
        public string Glyph = Ico.Copy;
        public bool Slash = false;
        public bool SmallIcon = false;
        public Color IconColorOverride = Color.Empty;
        private bool iconHover;

        public IconButton(string glyph, string tooltip)
        {
            Glyph = glyph;
            Size = new Size(T.Px(32), T.Px(32));
            Radius = T.Px(8);
            Text = tooltip == null ? "" : tooltip;
            TabStop = false;
            if (tooltip != null)
            {
                AccessibleName = tooltip;
                if (T.Tip != null) T.Tip.SetToolTip(this, tooltip);
            }
        }

        protected override bool UseHoverAnimation
        {
            get { return false; }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            iconHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            iconHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (Enabled && (pressed || iconHover))
            {
                Color bg = pressed ? T.Mix(BackColor, T.Accent, 0.18f) : T.Mix(BackColor, T.Text, 0.09f);
                T.FillRound(g, bg, r, Radius);
            }
            Color baseColor = IconColorOverride.IsEmpty ? T.Muted : IconColorOverride;
            Color hoverColor = IconColorOverride.IsEmpty ? T.Text : T.Mix(IconColorOverride, Color.White, 0.25f);
            Color c = Enabled ? (iconHover ? hoverColor : baseColor) : T.Mix(baseColor, BackColor, 0.5f);
            Font f = SmallIcon ? T.FontIconSm : T.FontIcon;
            T.DrawIcon(g, Glyph, f, Rectangle.Round(r), c);
            if (Slash && Enabled)
            {
                using (Pen pen = new Pen(c, Math.Max(1.2f, (float)T.Px(2))))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, Width * 0.24f, Height * 0.76f, Width * 0.76f, Height * 0.24f);
                }
            }
        }
    }

    internal class CategoryButton : Button, IThemed
    {
        public bool IsAll;
        public string CategoryKey;
        public int ItemCount;
        public bool IsSelected;

        public void ApplyTheme()
        {
            BackColor = T.Sidebar;
            Invalidate();
        }

        public CategoryButton(string caption, bool isAll, string key, int count)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = caption;
            IsAll = isAll;
            CategoryKey = key;
            ItemCount = count;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            TabStop = false;
            Cursor = Cursors.Hand;
            BackColor = T.Sidebar;
            ForeColor = T.Text;
            Font = T.FontUi;
            Size = new Size(T.Px(206), T.Px(38));
        }

        private float hoverT;

        protected override void OnMouseEnter(EventArgs e)
        {
            Motion.Animate(this, hoverT, 1f, 120, delegate(float v) { hoverT = v; Invalidate(); });
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Motion.Animate(this, hoverT, 0f, 140, delegate(float v) { hoverT = v; Invalidate(); });
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (IsSelected)
            {
                T.FillRound(g, T.AccentSoft, r, T.Px(9));
                T.FillRound(g, T.Accent, new RectangleF(T.Px(3), T.Px(10), T.Px(3), Height - T.Px(20)), T.Px(2));
            }
            else if (hoverT > 0.01f)
            {
                Color hf = T.Mix(T.Sidebar, T.Mix(T.Sidebar, T.Text, 0.06f), hoverT);
                T.FillRound(g, hf, r, T.Px(9));
            }
            Color tc = IsSelected ? T.Accent : T.Mix(T.TextDim, T.Text, hoverT);
            Font f = IsSelected ? T.FontBold : Font;
            Rectangle tr = new Rectangle(T.Px(18), 0, Width - T.Px(58), Height);
            TextRenderer.DrawText(g, Text, f, tr, tc,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            if (ItemCount >= 0)
            {
                Rectangle cr = new Rectangle(Width - T.Px(52), 0, T.Px(38), Height);
                TextRenderer.DrawText(g, ItemCount.ToString(), T.FontSmall, cr,
                    IsSelected ? T.Accent : T.Muted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }
    }

    internal class ChipButton : Button, IThemed
    {
        public bool IsSelected;
        public string ChipValue;
        public bool IsAction;
        public SurfaceRole Surface = SurfaceRole.Bg;
        public bool SurfaceCustom = false;

        public void ApplyTheme()
        {
            if (!SurfaceCustom) BackColor = T.RoleColor(Surface);
            Invalidate();
        }

        public ChipButton(string caption, string value, bool selected)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = caption;
            ChipValue = value;
            IsSelected = selected;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            TabStop = false;
            Cursor = Cursors.Hand;
            BackColor = T.Bg;
            Font = T.FontUi;
            Height = T.Px(32);
            Width = T.MeasureWidth(caption, T.FontUi) + T.Px(30);
        }

        private float hoverT;

        protected override void OnMouseEnter(EventArgs e)
        {
            Motion.Animate(this, hoverT, 1f, 120, delegate(float v) { hoverT = v; Invalidate(); });
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            Motion.Animate(this, hoverT, 0f, 140, delegate(float v) { hoverT = v; Invalidate(); });
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            if (IsSelected)
            {
                T.FillRound(g, T.AccentSoft, r, T.Px(16));
                T.DrawRound(g, T.Mix(T.Accent, T.Bg, 0.35f), r, T.Px(16), 1f);
            }
            else
            {
                T.FillRound(g, T.Mix(T.PanelDark, T.Field, hoverT), r, T.Px(16));
                T.DrawRound(g, T.Mix(T.Border, T.BorderHi, hoverT), r, T.Px(16), 1f);
            }
            Color tc = IsSelected ? T.Accent : T.Mix(T.TextDim, T.Text, hoverT);
            TextRenderer.DrawText(g, Text, Font, Rectangle.Round(r), tc,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
        }
    }

    internal class DarkInput : Panel, IThemed
    {
        public TextBox Inner;
        private string placeholder = "";
        public int Radius = 9;
        public readonly bool Multiline;
        public SurfaceRole Surface = SurfaceRole.Bg;

        private Label placeholderLbl;
        private bool focused;
        private bool hover;

        public DarkInput(bool multiline)
        {
            Multiline = multiline;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Inner = new TextBox();
            Inner.BorderStyle = BorderStyle.None;
            Inner.Font = multiline ? T.FontMono : T.FontUi;
            Inner.BackColor = T.Field;
            Inner.ForeColor = T.Text;
            Inner.Multiline = multiline;
            if (multiline)
            {
                Inner.WordWrap = false;
                Inner.ScrollBars = ScrollBars.None;
                Inner.AcceptsReturn = false;
                Inner.AcceptsTab = false;
            }
            Inner.GotFocus += delegate { focused = true; UpdatePlaceholder(); Invalidate(); };
            Inner.LostFocus += delegate { focused = false; UpdatePlaceholder(); Invalidate(); };
            Inner.MouseEnter += delegate { hover = true; Invalidate(); };
            Inner.MouseLeave += delegate { hover = false; Invalidate(); };
            Inner.TextChanged += delegate { UpdatePlaceholder(); };
            Inner.KeyDown += OnInnerKeyDown;
            Controls.Add(Inner);

            if (Multiline)
            {
                // 多行输入框无法使用系统 cue banner，用覆盖标签显示占位提示
                placeholderLbl = new Label();
                placeholderLbl.AutoSize = false;
                placeholderLbl.BackColor = T.Field;
                placeholderLbl.ForeColor = T.Muted;
                placeholderLbl.Font = Inner.Font;
                placeholderLbl.TextAlign = ContentAlignment.MiddleLeft;
                placeholderLbl.UseMnemonic = false;
                placeholderLbl.Cursor = Cursors.IBeam;
                placeholderLbl.Visible = false;
                placeholderLbl.Click += delegate { Inner.Focus(); };
                Controls.Add(placeholderLbl);
            }
            else
            {
                Inner.HandleCreated += delegate { ApplyCueBanner(); };
            }

            Size = new Size(T.Px(300), multiline ? T.Px(86) : T.Px(40));
            Cursor = Cursors.IBeam;
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            BackColor = T.RoleColor(Surface);
            Inner.BackColor = T.Field;
            Inner.ForeColor = T.Text;
            if (placeholderLbl != null)
            {
                placeholderLbl.BackColor = T.Field;
                placeholderLbl.ForeColor = T.Muted;
            }
            Invalidate();
        }

        private void ApplyCueBanner()
        {
            if (Multiline || !Inner.IsHandleCreated) return;
            string text = placeholder == null ? "" : placeholder;
            Native.SendMessage(Inner.Handle, 0x1501, (IntPtr)1, text); // EM_SETCUEBANNER
        }

        private void UpdatePlaceholder()
        {
            if (Multiline)
            {
                if (placeholderLbl == null) return;
                bool show = !focused && Inner.Text.Length == 0 && placeholder.Length > 0;
                placeholderLbl.Text = placeholder;
                placeholderLbl.Visible = show;
                if (show) placeholderLbl.BringToFront();
            }
            else
            {
                ApplyCueBanner();
            }
        }

        public string Placeholder
        {
            get { return placeholder; }
            set
            {
                placeholder = value == null ? "" : value;
                UpdatePlaceholder();
            }
        }

        public event EventHandler EnterPressed;

        private void OnInnerKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !Multiline)
            {
                e.SuppressKeyPress = true;
                if (EnterPressed != null) EnterPressed(this, EventArgs.Empty);
            }
        }

        public string Value
        {
            get { return Inner.Text; }
            set { Inner.Text = value; }
        }

        public TextBox Box
        {
            get { return Inner; }
        }

        public bool PasswordMode
        {
            set { Inner.UseSystemPasswordChar = value; }
        }

        public void SetText(string v)
        {
            Inner.Text = v;
        }

        public void FocusInner()
        {
            Inner.Focus();
        }

        public void SelectAllInner()
        {
            Inner.Focus();
            Inner.SelectAll();
        }

        /// <summary>设置无障碍名称（同时用于自动化测试定位）。</summary>
        public void SetAccessibleName(string name)
        {
            AccessibleName = name;
            Inner.AccessibleName = name;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutInner();
        }

        private void LayoutInner()
        {
            int padL = T.Px(12);
            int padR = T.Px(12);
            if (Multiline)
            {
                int padT = T.Px(9);
                Inner.SetBounds(padL, padT, Math.Max(10, Width - padL - padR), Math.Max(10, Height - padT - T.Px(9)));
            }
            else
            {
                int th = Inner.PreferredHeight;
                if (th <= 0) th = T.Px(20);
                Inner.SetBounds(padL, Math.Max(1, (Height - th) / 2), Math.Max(10, Width - padL - padR), th);
            }
            if (placeholderLbl != null)
            {
                if (Multiline)
                {
                    placeholderLbl.SetBounds(Inner.Left + 1, Inner.Top, Math.Max(10, Inner.Width - 2), T.Px(24));
                }
                else
                {
                    placeholderLbl.SetBounds(Inner.Left + 1, Inner.Top, Math.Max(10, Inner.Width - 2), Math.Max(1, Inner.Height));
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            T.FillRound(g, T.Field, r, Radius);
            Color bc = focused ? T.Accent : (hover ? T.BorderHi : T.Border);
            T.DrawRound(g, bc, r, Radius, focused ? 1.6f : 1f);
        }

        protected override void OnClick(EventArgs e)
        {
            Inner.Focus();
            base.OnClick(e);
        }
    }

    internal class CardPanel : Panel, IThemed
    {
        public int Radius = 16;
        public Color FillColor = T.PanelDark;
        public Color BorderColor = T.Border;
        public bool DrawBorder = true;
        public SurfaceRole Surface = SurfaceRole.Surface;
        public bool SurfaceCustom = false;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (!SurfaceCustom)
            {
                BackColor = T.Bg;
                FillColor = T.RoleColor(Surface);
                BorderColor = T.Border;
            }
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            T.FillRound(g, FillColor, r, Radius);
            if (DrawBorder) T.DrawRound(g, BorderColor, r, Radius, 1f);
        }
    }

    internal class LogoMark : Control, IThemed
    {
        public SurfaceRole Surface = SurfaceRole.Bg;

        public LogoMark()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(T.Px(38), T.Px(38));
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            BackColor = T.RoleColor(Surface);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0, 0, Width, Height);
            float rad = Width * 0.28f;
            using (GraphicsPath path = T.RoundPath(r, rad))
            using (LinearGradientBrush br = new LinearGradientBrush(r,
                Color.FromArgb(96, 165, 255), Color.FromArgb(124, 108, 255), 45f))
            {
                g.FillPath(br, path);
            }
            float cx = Width / 2f;
            float cr = Width * 0.135f;
            float cy = Height * 0.385f;
            using (SolidBrush wb = new SolidBrush(Color.White))
            {
                g.FillEllipse(wb, cx - cr, cy - cr, cr * 2f, cr * 2f);
                PointF[] pts = new PointF[4];
                pts[0] = new PointF(cx - Width * 0.052f, cy + cr * 0.55f);
                pts[1] = new PointF(cx + Width * 0.052f, cy + cr * 0.55f);
                pts[2] = new PointF(cx + Width * 0.105f, Height * 0.80f);
                pts[3] = new PointF(cx - Width * 0.105f, Height * 0.80f);
                g.FillPolygon(wb, pts);
            }
        }
    }

    internal class ToastControl : Control, IThemed
    {
        private readonly Timer timer;
        private string message = "";

        public ToastControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Height = T.Px(38);
            Visible = false;
            timer = new Timer();
            timer.Interval = 2400;
            timer.Tick += delegate
            {
                timer.Stop();
                Visible = false;
            };
        }

        public void ApplyTheme()
        {
            BackColor = T.Bg;
            Invalidate();
        }

        public void HideToast()
        {
            timer.Stop();
            Visible = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        public void ShowMessage(string msg)
        {
            message = msg;
            int w;
            using (Graphics g = CreateGraphics())
            {
                w = TextRenderer.MeasureText(g, msg, T.FontUi).Width + T.Px(48);
            }
            Width = Math.Min(Math.Max(T.Px(160), w), T.Px(520));
            Visible = true;
            BringToFront();
            Invalidate();
            timer.Stop();
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            T.FillRound(g, Color.FromArgb(28, 0, 0, 0), new RectangleF(2f, T.Px(3), Width - 4f, Height - 2f), T.Px(10));
            T.FillRound(g, T.PanelDark, r, T.Px(9));
            T.DrawRound(g, T.Border, r, T.Px(9), 1f);
            Rectangle ir = new Rectangle(T.Px(12), 0, T.Px(22), Height);
            T.DrawIcon(g, Ico.Check, T.FontIconSm, ir, T.Success);
            Rectangle tr = new Rectangle(T.Px(34), 0, Width - T.Px(44), Height);
            TextRenderer.DrawText(g, message, T.FontUi, tr, T.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    internal class EntryCard : Panel, IThemed
    {
        public KeyItem Item;
        public bool Revealed;
        public Action<KeyItem> CopyAction;
        public Action<KeyItem> EditAction;
        public Action<KeyItem> DeleteAction;
        public Action<KeyItem, bool> RevealAction;
        public Action<KeyItem> TitleClickAction;

        private readonly Label nameLbl;
        private readonly Label metaLbl;
        private readonly Label keyLbl;
        private readonly IconButton copyBtn;
        private readonly IconButton revealBtn;
        private readonly IconButton editBtn;
        private readonly IconButton delBtn;
        private bool hovering;
        private float hoverT;

        public EntryCard(KeyItem item, bool revealed)
        {
            Item = item;
            Revealed = revealed;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Height = T.Px(84);

            nameLbl = UiKit.Label(item.Name, T.FontBold, T.Text, T.Card);
            nameLbl.AutoEllipsis = true;
            nameLbl.Name = "name_" + item.Id;
            nameLbl.Cursor = Cursors.Hand;
            nameLbl.Click += delegate { OpenTitle(); };

            metaLbl = UiKit.Label(MetaText(), T.FontSmall, T.Muted, T.Card);
            metaLbl.AutoEllipsis = true;
            metaLbl.Name = "meta_" + item.Id;

            keyLbl = UiKit.Label("", T.FontMono, T.Muted, T.Card);
            keyLbl.TextAlign = ContentAlignment.MiddleRight;
            keyLbl.AutoEllipsis = true;
            keyLbl.Name = "key_" + item.Id;
            ((ThemedLabel)keyLbl).Tone = LabelTone.Custom;
            keyLbl.Cursor = Cursors.Hand;
            keyLbl.Click += delegate { OpenTitle(); };
            metaLbl.Cursor = Cursors.Hand;
            metaLbl.Click += delegate { OpenTitle(); };
            Cursor = Cursors.Hand;
            Click += delegate { OpenTitle(); };

            copyBtn = UiKit.Icon(Ico.Copy, L.T("复制"));
            copyBtn.Name = "copy_" + item.Id;
            copyBtn.SurfaceCustom = true;
            revealBtn = UiKit.Icon(Ico.View, L.T("显示 / 隐藏"));
            revealBtn.Name = "reveal_" + item.Id;
            revealBtn.SurfaceCustom = true;
            editBtn = UiKit.Icon(Ico.Edit, L.T("编辑"));
            editBtn.Name = "edit_" + item.Id;
            editBtn.SurfaceCustom = true;
            delBtn = UiKit.Icon(Ico.Delete, L.T("删除"));
            delBtn.Name = "delete_" + item.Id;
            delBtn.SurfaceCustom = true;

            copyBtn.Click += delegate { if (CopyAction != null) CopyAction(Item); };
            revealBtn.Click += delegate
            {
                SetRevealed(!Revealed);
                if (RevealAction != null) RevealAction(Item, Revealed);
            };
            editBtn.Click += delegate { if (EditAction != null) EditAction(Item); };
            delBtn.Click += delegate { if (DeleteAction != null) DeleteAction(Item); };

            Controls.Add(nameLbl);
            Controls.Add(metaLbl);
            Controls.Add(keyLbl);
            Controls.Add(copyBtn);
            Controls.Add(revealBtn);
            Controls.Add(editBtn);
            Controls.Add(delBtn);

            HookHover(this);
            for (int i = 0; i < Controls.Count; i++) HookHover(Controls[i]);

            Resize += delegate { LayoutChildren(); };
            LayoutChildren();
            ApplyTheme();
        }

        public string ItemId
        {
            get { return Item == null ? "" : Item.Id; }
        }

        /// <summary>复用已有卡片时更新数据，避免搜索/刷新反复创建窗口句柄。</summary>
        public void UpdateItem(KeyItem item, bool revealed)
        {
            Item = item;
            Revealed = revealed;
            nameLbl.Text = item.Name ?? "";
            nameLbl.Name = "name_" + item.Id;
            metaLbl.Text = MetaText();
            metaLbl.Name = "meta_" + item.Id;
            keyLbl.Name = "key_" + item.Id;
            copyBtn.Name = "copy_" + item.Id;
            revealBtn.Name = "reveal_" + item.Id;
            editBtn.Name = "edit_" + item.Id;
            delBtn.Name = "delete_" + item.Id;
            ApplyReveal();
            LayoutChildren();
            Invalidate();
        }

        public void ApplyTheme()
        {
            BackColor = T.Bg;
            ApplyCardBackColors(hovering);
            ApplyReveal();
            Invalidate();
        }

        private void ApplyCardBackColors(bool hover)
        {
            Color bg = hover ? T.CardHover : T.Card;
            nameLbl.BackColor = bg;
            metaLbl.BackColor = bg;
            keyLbl.BackColor = bg;
            copyBtn.BackColor = bg;
            revealBtn.BackColor = bg;
            editBtn.BackColor = bg;
            delBtn.BackColor = bg;
        }

        private void HookHover(Control c)
        {
            c.MouseEnter += delegate { SetHover(true); };
            c.MouseLeave += delegate { SetHover(IsCursorInside()); };
        }

        private bool IsCursorInside()
        {
            return RectangleToScreen(ClientRectangle).Contains(Cursor.Position);
        }

        private void SetHover(bool on)
        {
            if (hovering == on) return;
            hovering = on;
            // 子控件背景只在进入/离开时各刷新一次，过渡期间仅重绘卡片本体
            ApplyCardBackColors(on);
            Motion.Animate(this, hoverT, on ? 1f : 0f, 130, delegate(float v)
            {
                hoverT = v;
                Invalidate();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Motion.Stop(this);
                for (int i = 0; i < Controls.Count; i++) Motion.Stop(Controls[i]);
            }
            base.Dispose(disposing);
        }

        private void OpenTitle()
        {
            if (TitleClickAction != null) TitleClickAction(Item);
        }

        private string MetaText()
        {
            string cat = string.IsNullOrEmpty(Item.Category) ? L.T("未分类") : Item.Category;
            int count = Item.Keys == null ? 0 : Item.Keys.Count;
            if (count > 1) cat += "   ·   " + L.KeyCount(count);
            if (string.IsNullOrEmpty(Item.Note)) return cat;
            return cat + "   ·   " + Item.Note;
        }

        private void LayoutChildren()
        {
            int pad = T.Px(18);
            int btn = T.Px(30);
            int gap = T.Px(2);
            int right = Width - T.Px(12);
            int top = (Height - T.Px(4) - btn) / 2;
            delBtn.SetBounds(right - btn, top, btn, btn);
            right -= btn + gap;
            editBtn.SetBounds(right - btn, top, btn, btn);
            right -= btn + gap;
            revealBtn.SetBounds(right - btn, top, btn, btn);
            right -= btn + gap;
            copyBtn.SetBounds(right - btn, top, btn, btn);

            int keyRight = copyBtn.Left - T.Px(14);
            int keyW = Math.Min(T.Px(320), Math.Max(T.Px(110), Width / 3));
            keyLbl.SetBounds(keyRight - keyW, (Height - T.Px(22)) / 2, keyW, T.Px(22));

            int nameW = Math.Max(T.Px(80), keyLbl.Left - pad - T.Px(12));
            nameLbl.SetBounds(pad, T.Px(13), nameW, T.Px(24));
            metaLbl.SetBounds(pad, T.Px(40), nameW, T.Px(20));
        }

        public void SetRevealed(bool revealed)
        {
            Revealed = revealed;
            ApplyReveal();
        }

        private void ApplyReveal()
        {
            string value = Item.DefaultValue;
            keyLbl.Text = Revealed ? value : MaskKey(value);
            keyLbl.ForeColor = Revealed ? T.Text : T.Muted;
            revealBtn.Slash = Revealed;
            revealBtn.Invalidate();
        }

        public static string MaskKey(string v)
        {
            if (string.IsNullOrEmpty(v)) return "—";
            if (v.Length <= 12) return "••••••••";
            return v.Substring(0, 4) + "••••••••" + v.Substring(v.Length - 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f - T.Px(4));

            // 柔和投影（在卡片下方）
            T.FillRound(g, Color.FromArgb(14, 0, 0, 0), new RectangleF(1f, T.Px(5), Width - 2f, Height - T.Px(6)), T.Px(14));
            T.FillRound(g, Color.FromArgb(10, 0, 0, 0), new RectangleF(2f, T.Px(7), Width - 4f, Height - T.Px(8)), T.Px(14));

            Color cardBg = T.Mix(T.Card, T.CardHover, hoverT);
            T.FillRound(g, cardBg, r, T.Px(14));
            Color border = T.Mix(T.Border, T.Accent, hoverT * 0.35f);
            T.DrawRound(g, border, r, T.Px(14), 1f);
            // 顶部柔光
            Color sheen = T.Mix(cardBg, Color.White, T.IsDark ? 0.05f : 0.35f);
            using (Pen p = new Pen(sheen))
            {
                int inset = T.Px(14);
                if (Width > inset * 2) g.DrawLine(p, inset, T.Px(2), Width - inset, T.Px(2));
            }
        }
    }

    internal class CardList : Control, IThemed
    {
        private readonly List<EntryCard> cards = new List<EntryCard>();
        private int scroll;
        private int contentHeight;
        private bool dragging;
        private int dragStartY;
        private int dragStartScroll;
        private bool thumbHover;
        private double wheelAccum;
        private int visFirst = -1;
        private int visLast = -1;
        private string emptyTitle = L.T("还没有保存任何 Key");
        private string emptySub = L.T("点击右上角「添加」，保存你的第一个 API Key");

        public Action<KeyItem> CopyAction;
        public Action<KeyItem> EditAction;
        public Action<KeyItem> DeleteAction;
        public Action<KeyItem, bool> RevealAction;
        public Action<KeyItem> TitleClickAction;

        public CardList()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
        }

        public void ApplyTheme()
        {
            BackColor = T.Bg;
            Invalidate();
        }

        public void SetEmptyState(string title, string sub)
        {
            emptyTitle = title;
            emptySub = sub;
        }

        public void SetItems(List<KeyItem> items, HashSet<string> revealedIds)
        {
            SuspendLayout();
            Dictionary<string, EntryCard> old = new Dictionary<string, EntryCard>();
            for (int i = 0; i < cards.Count; i++)
            {
                EntryCard c = cards[i];
                string id = c.ItemId;
                if (!string.IsNullOrEmpty(id) && !old.ContainsKey(id)) old[id] = c;
                else
                {
                    Motion.Stop(c);
                    Controls.Remove(c);
                    c.Dispose();
                }
            }
            cards.Clear();
            List<EntryCard> next = new List<EntryCard>();
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    KeyItem it = items[i];
                    bool revealed = revealedIds != null && revealedIds.Contains(it.Id);
                    EntryCard card;
                    if (!old.TryGetValue(it.Id, out card))
                    {
                        card = new EntryCard(it, revealed);
                        Controls.Add(card);
                    }
                    else
                    {
                        old.Remove(it.Id);
                        card.UpdateItem(it, revealed);
                    }
                    card.CopyAction = this.CopyAction;
                    card.EditAction = this.EditAction;
                    card.DeleteAction = this.DeleteAction;
                    card.RevealAction = this.RevealAction;
                    card.TitleClickAction = this.TitleClickAction;
                    next.Add(card);
                }
            }
            foreach (EntryCard leftover in old.Values)
            {
                Motion.Stop(leftover);
                Controls.Remove(leftover);
                leftover.Dispose();
            }
            cards.AddRange(next);
            scroll = 0;
            wheelAccum = 0;
            visFirst = -1;
            visLast = -1;
            for (int i = 0; i < cards.Count; i++) cards[i].Visible = false;
            ResumeLayout();
            LayoutCards();
            Invalidate();
        }

        public int MaxScroll
        {
            get { return Math.Max(0, contentHeight - Height); }
        }

        private void LayoutCards()
        {
            int padX = T.Px(28);
            int top = T.Px(10);
            int h = T.Px(78);
            int gap = T.Px(10);
            int cardW = Math.Max(T.Px(200), Width - padX * 2 - T.Px(10));
            int total = 0;
            if (cards.Count > 0)
            {
                total = top + cards.Count * (h + gap) - gap + T.Px(26);
            }
            contentHeight = total;
            if (scroll > MaxScroll) scroll = MaxScroll;
            if (scroll < 0) scroll = 0;
            if (cards.Count == 0 || Height <= 0)
            {
                visFirst = -1;
                visLast = -1;
                Invalidate();
                return;
            }

            int first = Math.Max(0, (scroll - top) / (h + gap));
            if (first > cards.Count - 1) first = cards.Count - 1;
            int last = Math.Min(cards.Count - 1, (scroll + Height - top) / (h + gap));
            if (last < first) last = first;
            // 只隐藏上一帧可见、本帧离开视口的卡片；不遍历全部列表
            if (visFirst >= 0)
            {
                int end = Math.Min(visLast, cards.Count - 1);
                for (int i = visFirst; i <= end; i++)
                {
                    if (i < first || i > last) cards[i].Visible = false;
                }
            }
            for (int i = first; i <= last; i++)
            {
                EntryCard c = cards[i];
                int y = top + i * (h + gap) - scroll;
                Rectangle target = new Rectangle(padX, y, cardW, h);
                if (c.Bounds != target) c.SetBounds(target.X, target.Y, target.Width, target.Height);
                if (!c.Visible) c.Visible = true;
            }
            visFirst = first;
            visLast = last;
            Invalidate();
        }

        public bool ScrollBy(int dy)
        {
            if (cards.Count == 0 || dy == 0) return false;
            int next = scroll + dy;
            if (next > MaxScroll) next = MaxScroll;
            if (next < 0) next = 0;
            if (next == scroll) return false;
            scroll = next;
            LayoutCards();
            return true;
        }

        /// <summary>统一滚轮入口：保留小数余量，兼容触控板/高精度鼠标的小增量。</summary>
        public void ScrollWheelDelta(int delta)
        {
            if (cards.Count == 0) return;
            double step = T.Px(88);
            wheelAccum += delta * step / 120.0;
            int px = (int)wheelAccum;
            if (px == 0) return;
            wheelAccum -= px;
            if (!ScrollBy(-px)) wheelAccum = 0;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutCards();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollWheelDelta(e.Delta);
            base.OnMouseWheel(e);
        }

        private Rectangle ThumbRect()
        {
            if (contentHeight <= Height || Height <= 0) return Rectangle.Empty;
            int trackTop = T.Px(10);
            int trackH = Height - T.Px(20);
            int trackX = Width - T.Px(13);
            int thumbH = Math.Max(T.Px(36), (int)((double)trackH * Height / contentHeight));
            int maxScroll = MaxScroll;
            int thumbY = trackTop;
            if (maxScroll > 0) thumbY = trackTop + (int)((double)(trackH - thumbH) * scroll / maxScroll);
            return new Rectangle(trackX, thumbY, T.Px(5), thumbH);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Rectangle thumb = ThumbRect();
            if (thumb.IsEmpty) return;
            Rectangle track = new Rectangle(thumb.Left - T.Px(6), T.Px(10), thumb.Width + T.Px(12), Math.Max(0, Height - T.Px(20)));
            if (thumb.Contains(e.Location))
            {
                dragging = true;
                dragStartY = e.Y;
                dragStartScroll = scroll;
                wheelAccum = 0;
                Capture = true;
            }
            else if (track.Contains(e.Location))
            {
                if (e.Y < thumb.Top) ScrollBy(-Height + T.Px(70));
                else if (e.Y > thumb.Bottom) ScrollBy(Height - T.Px(70));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Rectangle thumb = ThumbRect();
            bool over = !thumb.IsEmpty && thumb.Contains(e.Location);
            if (over != thumbHover)
            {
                thumbHover = over;
                Invalidate();
            }
            if (dragging && !thumb.IsEmpty)
            {
                int trackTop = T.Px(10);
                int trackH = Height - T.Px(20);
                int range = Math.Max(1, trackH - thumb.Height);
                int maxScroll = MaxScroll;
                int next = dragStartScroll + (int)((double)(e.Y - dragStartY) * maxScroll / range);
                if (next > maxScroll) next = maxScroll;
                if (next < 0) next = 0;
                if (next != scroll)
                {
                    scroll = next;
                    LayoutCards();
                }
            }
            Cursor = dragging ? Cursors.SizeNS : (over ? Cursors.Hand : Cursors.Default);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            wheelAccum = 0;
            Capture = false;
            base.OnMouseUp(e);
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (dragging && !Capture)
            {
                dragging = false;
                wheelAccum = 0;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            if (cards.Count == 0)
            {
                DrawEmpty(g);
                return;
            }
            Rectangle thumb = ThumbRect();
            if (!thumb.IsEmpty)
            {
                Rectangle track = new Rectangle(thumb.Left, T.Px(10), thumb.Width, Height - T.Px(20));
                T.FillRound(g, T.ScrollTrack, new RectangleF(track.X, track.Y, track.Width, track.Height), T.Px(3));
                T.FillRound(g, thumbHover || dragging ? T.ScrollThumbHover : T.ScrollThumb,
                    new RectangleF(thumb.X, thumb.Y, thumb.Width, thumb.Height), T.Px(3));
            }
        }

        private void DrawEmpty(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2;
            if (cy < T.Px(130)) cy = T.Px(130);
            int box = T.Px(58);
            RectangleF iconRect = new RectangleF(cx - box / 2f, cy - T.Px(112), box, box);
            T.FillRound(g, T.AccentSoft, iconRect, T.Px(16));
            T.DrawIcon(g, Ico.Lock, T.FontIcon, Rectangle.Round(iconRect), T.Accent);
            Rectangle tr = new Rectangle(0, (int)iconRect.Bottom + T.Px(14), Width, T.Px(26));
            TextRenderer.DrawText(g, emptyTitle, T.FontBold, tr, T.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
            Rectangle sr = new Rectangle(0, tr.Bottom + T.Px(6), Width, T.Px(22));
            TextRenderer.DrawText(g, emptySub, T.FontSmall, sr, T.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
        }
    }
}
