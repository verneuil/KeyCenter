using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>侧边栏拖拽手柄。</summary>
    internal class SidebarResizeHandle : Control, IThemed
    {
        public Action<int> DragDelta; // 新的逻辑宽度
        public Action DragFinished;
        private bool hovering;
        private bool dragging;
        private int startX;
        private int startLogicalWidth;
        private int lastTarget = -1;

        public SidebarResizeHandle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.SizeWE;
            BackColor = T.Sidebar;
        }

        public void ApplyTheme()
        {
            BackColor = T.Sidebar;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            dragging = true;
            startX = Cursor.Position.X;
            startLogicalWidth = AppConfig.SidebarLogicalWidth;
            lastTarget = startLogicalWidth;
            Capture = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                int deltaLogical = (int)Math.Round((Cursor.Position.X - startX) / (double)T.Scale);
                int target = startLogicalWidth + deltaLogical;
                if (target < 180) target = 180;
                if (target > 460) target = 460;
                if (target != lastTarget)
                {
                    lastTarget = target;
                    if (DragDelta != null) DragDelta(target);
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                Capture = false;
                Invalidate();
                if (DragFinished != null) DragFinished();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            Color line = (hovering || dragging) ? T.Accent : T.Mix(T.Sidebar, T.Border, 0.75f);
            int w = Math.Max(1, T.Px(1));
            int h = dragging ? Height : T.Px(64);
            int y = (Height - h) / 2;
            T.FillRound(g, line, new RectangleF((Width - w) / 2f, y, w, h), w / 2f);
        }
    }

    /// <summary>排序菜单（跟随主题的右键/下拉菜单）。</summary>
    internal static class SortMenu
    {
        private static ContextMenuStrip currentMenu;

        private static readonly string[] Labels = new string[]
        {
            "创建时间：新 → 旧",
            "创建时间：旧 → 新",
            "编辑时间：新 → 旧",
            "编辑时间：旧 → 新",
            "名称：A → Z",
            "名称：Z → A"
        };

        public static void CloseCurrent()
        {
            ContextMenuStrip menu = currentMenu;
            currentMenu = null;
            if (menu != null)
            {
                try { menu.Close(); }
                catch (Exception) { }
            }
        }

        public static string LabelOf(SortMode mode)
        {
            int i = (int)mode;
            if (i < 0 || i >= Labels.Length) i = 0;
            switch (mode)
            {
                case SortMode.CreatedDesc: return "创建 ↓";
                case SortMode.CreatedAsc: return "创建 ↑";
                case SortMode.UpdatedDesc: return "编辑 ↓";
                case SortMode.UpdatedAsc: return "编辑 ↑";
                case SortMode.NameAsc: return "名称 A→Z";
                default: return "名称 Z→A";
            }
        }

        public static string LongLabelOf(SortMode mode)
        {
            int i = (int)mode;
            if (i < 0 || i >= Labels.Length) i = 0;
            return Labels[i];
        }

        public static void Show(Control anchor, SortMode current, Action<SortMode> pick)
        {
            // 上一次的菜单延迟到下次打开时再释放，避免正在关闭的过程中被访问
            if (currentMenu != null)
            {
                ContextMenuStrip old = currentMenu;
                currentMenu = null;
                try { old.Dispose(); }
                catch (Exception) { }
            }
            ContextMenuStrip menu = new ContextMenuStrip();
            currentMenu = menu;
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = false;
            menu.BackColor = T.PanelDark;
            menu.ForeColor = T.Text;
            menu.Font = T.FontUi;
            menu.Padding = new Padding(0, T.Px(4), 0, T.Px(4));
            menu.Renderer = new ThemedMenuRenderer();
            for (int i = 0; i < Labels.Length; i++)
            {
                int mode = i;
                ToolStripMenuItem item = new ToolStripMenuItem(Labels[i]);
                item.ForeColor = T.Text;
                item.BackColor = T.PanelDark;
                item.Padding = new Padding(T.Px(8), T.Px(4), T.Px(14), T.Px(4));
                if ((int)current == i) item.Font = T.FontBold;
                item.Click += delegate
                {
                    try { menu.Close(); }
                    catch (Exception) { }
                    if (pick != null) pick((SortMode)mode);
                };
                menu.Items.Add(item);
            }
            menu.Closed += delegate
            {
                if (currentMenu == menu) currentMenu = null;
            };
            menu.Show(anchor, new Point(0, anchor.Height + T.Px(4)));
        }
    }

    internal class ThemedMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return T.PanelDark; } }
        public override Color MenuBorder { get { return T.Border; } }
        public override Color MenuItemBorder { get { return T.AccentSoft; } }
        public override Color MenuItemSelected { get { return T.CardHover; } }
        public override Color MenuItemSelectedGradientBegin { get { return T.CardHover; } }
        public override Color MenuItemSelectedGradientEnd { get { return T.CardHover; } }
        public override Color MenuItemPressedGradientBegin { get { return T.Card; } }
        public override Color MenuItemPressedGradientEnd { get { return T.Card; } }
        public override Color ImageMarginGradientBegin { get { return T.PanelDark; } }
        public override Color ImageMarginGradientMiddle { get { return T.PanelDark; } }
        public override Color ImageMarginGradientEnd { get { return T.PanelDark; } }
        public override Color CheckBackground { get { return T.AccentSoft; } }
        public override Color CheckSelectedBackground { get { return T.AccentSoft; } }
        public override Color CheckPressedBackground { get { return T.AccentSoft; } }
        public override Color SeparatorDark { get { return T.Border; } }
        public override Color SeparatorLight { get { return T.Border; } }
    }

    internal class ThemedMenuRenderer : ToolStripProfessionalRenderer
    {
        public ThemedMenuRenderer() : base(new ThemedMenuColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            Rectangle r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using (Pen p = new Pen(T.Border))
            {
                e.Graphics.DrawRectangle(p, r);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            Color color = e.Item.Selected ? T.Accent : T.Text;
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, e.TextRectangle, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    /// <summary>抽屉里的一条 Key。</summary>
    internal class DrawerRow : Panel, IThemed
    {
        private readonly SubKey key;
        private readonly int index;
        private readonly bool isDefault;
        public bool Revealed;
        public Action<SubKey, string, bool> CopyRequested;   // key, 显示名, 是否明文
        public Action<string> DefaultRequested;              // keyId

        private readonly Label nameLbl;
        private readonly Label valueLbl;
        private readonly Label badgeLbl;
        private readonly IconButton copyBtn;
        private readonly IconButton viewBtn;
        private readonly IconButton starBtn;
        private bool hovering;
        private float hoverT;

        public DrawerRow(SubKey key, int index, bool isDefault)
        {
            this.key = key;
            this.index = index;
            this.isDefault = isDefault;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.PanelDark;
            Height = T.Px(96);

            string display = string.IsNullOrEmpty(key.Label) ? ("Key " + (index + 1)) : key.Label.Trim();
            nameLbl = UiKit.Label(display, T.FontBold, T.Text, T.Card);
            nameLbl.AutoEllipsis = true;
            nameLbl.Name = "drawer_name_" + key.Id;

            badgeLbl = UiKit.Label("默认", T.FontSmall, T.Accent, T.AccentSoft);
            badgeLbl.TextAlign = ContentAlignment.MiddleCenter;
            badgeLbl.Visible = isDefault;

            valueLbl = UiKit.Label("", T.FontMono, T.Muted, T.Card);
            valueLbl.AutoEllipsis = true;

            copyBtn = UiKit.Icon(Ico.Copy, "复制这条 Key");
            copyBtn.SurfaceCustom = true;
            copyBtn.Name = "drawer_copy_" + key.Id;
            copyBtn.Click += delegate
            {
                if (CopyRequested != null) CopyRequested(key, display, Revealed);
            };

            viewBtn = UiKit.Icon(Ico.View, "显示 / 隐藏");
            viewBtn.SurfaceCustom = true;
            viewBtn.Name = "drawer_reveal_" + key.Id;
            viewBtn.Click += delegate
            {
                Revealed = !Revealed;
                ApplyValue();
            };

            starBtn = UiKit.Icon(isDefault ? Ico.StarFill : Ico.Star, isDefault ? "当前默认 Key" : "设为默认 Key");
            starBtn.SurfaceCustom = true;
            starBtn.Name = "drawer_default_" + key.Id;
            starBtn.IconColorOverride = isDefault ? T.Accent : T.Muted;
            starBtn.Click += delegate
            {
                if (!isDefault && DefaultRequested != null) DefaultRequested(key.Id);
            };

            Controls.Add(nameLbl);
            Controls.Add(badgeLbl);
            Controls.Add(valueLbl);
            Controls.Add(copyBtn);
            Controls.Add(viewBtn);
            Controls.Add(starBtn);

            HookHover(this);
            for (int i = 0; i < Controls.Count; i++) HookHover(Controls[i]);
            Resize += delegate { LayoutChildren(); };
            LayoutChildren();
            ApplyValue();
            ApplyTheme();
        }

        private void HookHover(Control c)
        {
            c.MouseEnter += delegate { SetHover(true); };
            c.MouseLeave += delegate
            {
                if (!RectangleToScreen(ClientRectangle).Contains(Cursor.Position)) SetHover(false);
            };
        }

        private void SetHover(bool on)
        {
            if (hovering == on) return;
            hovering = on;
            Motion.Animate(this, hoverT, on ? 1f : 0f, 120, delegate(float v)
            {
                hoverT = v;
                ApplyCardColors(v);
                Invalidate();
            });
        }

        public void ApplyTheme()
        {
            BackColor = T.PanelDark;
            ApplyCardColors(hoverT);
            ApplyValue();
            Invalidate();
        }

        private void ApplyCardColors(float t)
        {
            Color bg = T.Mix(T.Card, T.CardHover, t);
            nameLbl.BackColor = bg;
            valueLbl.BackColor = bg;
            badgeLbl.BackColor = T.AccentSoft;
            copyBtn.BackColor = bg;
            viewBtn.BackColor = bg;
            starBtn.BackColor = bg;
        }

        private void ApplyValue()
        {
            valueLbl.Text = Revealed ? key.Value : EntryCard.MaskKey(key.Value);
            valueLbl.ForeColor = Revealed ? T.Text : T.Muted;
            viewBtn.Slash = Revealed;
            viewBtn.Invalidate();
        }

        private void LayoutChildren()
        {
            int pad = T.Px(14);
            int btn = T.Px(30);
            int gap = T.Px(4);
            int right = Width - T.Px(12);
            int top = T.Px(10);
            starBtn.SetBounds(right - btn, top, btn, btn); right -= btn + gap;
            copyBtn.SetBounds(right - btn, top, btn, btn); right -= btn + gap;
            viewBtn.SetBounds(right - btn, top, btn, btn); right -= btn + gap;

            int badgeW = isDefault ? T.Px(40) : 0;
            badgeLbl.SetBounds(right - badgeW, top + T.Px(4), badgeW, T.Px(20));
            int nameW = Math.Max(T.Px(80), right - badgeW - pad - T.Px(8));
            nameLbl.SetBounds(pad, top - T.Px(1), nameW, T.Px(28));
            valueLbl.SetBounds(pad, T.Px(45), Width - pad * 2, T.Px(24));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f - T.Px(3));
            T.FillRound(g, Color.FromArgb(12, 0, 0, 0), new RectangleF(1f, T.Px(4), Width - 2f, Height - T.Px(3)), T.Px(12));
            T.FillRound(g, T.Mix(T.Card, T.CardHover, hoverT), r, T.Px(12));
            Color bc = isDefault ? T.Mix(T.Accent, T.Border, 0.45f) : T.Mix(T.Border, T.Accent, hoverT * 0.25f);
            T.DrawRound(g, bc, r, T.Px(12), 1f);
            if (isDefault)
            {
                T.FillRound(g, T.Accent, new RectangleF(T.Px(1), T.Px(14), T.Px(3), Height - T.Px(28)), T.Px(2));
            }
        }
    }

    /// <summary>添加/编辑对话框里的一行 Key。</summary>
    internal class KeyEditorRow : Panel, IThemed
    {
        public string KeyId;
        public DarkInput LabelInput;
        public DarkInput ValueInput;
        public IconButton DefaultBtn;
        public IconButton DeleteBtn;
        public bool IsDefault;
        public Action<KeyEditorRow> DefaultRequested;
        public Action<KeyEditorRow> DeleteRequested;
        private bool hovering;
        private float hoverT;


        public KeyEditorRow(string keyId, string label, string value)
        {
            KeyId = keyId;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            Height = T.Px(98);

            LabelInput = new DarkInput(false);
            LabelInput.Surface = SurfaceRole.Surface;
            LabelInput.Placeholder = "备注名（可选）";
            LabelInput.Name = "keyLabel_" + keyId;
            LabelInput.Box.Name = LabelInput.Name;
            LabelInput.SetText(label ?? "");
            Controls.Add(LabelInput);
            ValueInput = new DarkInput(false);
            ValueInput.Surface = SurfaceRole.Surface;
            ValueInput.Placeholder = "粘贴 Key…";
            ValueInput.Name = "keyValue_" + keyId;
            ValueInput.Box.Name = ValueInput.Name;
            ValueInput.SetText(value ?? "");
            Controls.Add(ValueInput);
            DefaultBtn = UiKit.Icon(Ico.Star, "设为默认 Key");
            DefaultBtn.SurfaceCustom = true;
            DefaultBtn.Name = "setDefault_" + keyId;
            DefaultBtn.Click += delegate
            {
                if (DefaultRequested != null) DefaultRequested(this);
            };
            Controls.Add(DefaultBtn);
            DeleteBtn = UiKit.Icon(Ico.Delete, "删除这条 Key");
            DeleteBtn.SurfaceCustom = true;
            DeleteBtn.Name = "deleteKey_" + keyId;
            DeleteBtn.Click += delegate
            {
                if (DeleteRequested != null) DeleteRequested(this);
            };
            Controls.Add(DeleteBtn);
            HookHover(this);
            for (int i = 0; i < Controls.Count; i++) HookHover(Controls[i]);
            Resize += delegate { LayoutChildren(); };
            LayoutChildren();
            ApplyTheme();
        }

        private void HookHover(Control c)
        {
            c.MouseEnter += delegate { SetHover(true); };
            c.MouseLeave += delegate
            {
                if (!RectangleToScreen(ClientRectangle).Contains(Cursor.Position)) SetHover(false);
            };
        }

        private void SetHover(bool on)
        {
            if (hovering == on) return;
            hovering = on;
            Motion.Animate(this, hoverT, on ? 1f : 0f, 120, delegate(float v)
            {
                hoverT = v;
                ApplyColors();
                Invalidate();
            });
        }

        public void ApplyTheme()
        {
            BackColor = T.Bg;
            ApplyColors();
            Invalidate();
        }

        private void ApplyColors()
        {
            Color bg = T.Mix(T.PanelDark, T.CardHover, hoverT);
            LabelInput.BackColor = T.PanelDark;
            ValueInput.BackColor = T.PanelDark;
            DefaultBtn.BackColor = bg;
            DeleteBtn.BackColor = bg;
        }

        public void SetDefault(bool value)
        {
            IsDefault = value;
            DefaultBtn.Glyph = value ? Ico.StarFill : Ico.Star;
            DefaultBtn.IconColorOverride = value ? T.Accent : T.Muted;
            DefaultBtn.Invalidate();
            Invalidate();
        }

        public string LabelValue { get { return LabelInput.Value.Trim(); } }
        public string KeyValue { get { return ValueInput.Value.Trim(); } }

        private void LayoutChildren()
        {
            int pad = T.Px(12);
            int btn = T.Px(30);
            int right = Width - T.Px(10);
            DefaultBtn.SetBounds(right - btn, T.Px(12), btn, btn); right -= btn + T.Px(4);
            DeleteBtn.SetBounds(right - btn, T.Px(12), btn, btn);
            int labelW = Math.Max(T.Px(140), Width / 2 - T.Px(40));
            LabelInput.SetBounds(pad, T.Px(12), Math.Min(labelW, Width - pad - T.Px(90)), T.Px(34));
            ValueInput.SetBounds(pad, T.Px(54), Width - pad * 2, T.Px(34));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            g.Clear(BackColor);
            RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f - T.Px(3));
            T.FillRound(g, Color.FromArgb(10, 0, 0, 0), new RectangleF(1f, T.Px(4), Width - 2f, Height - T.Px(3)), T.Px(10));
            T.FillRound(g, T.Mix(T.PanelDark, T.CardHover, hoverT), r, T.Px(10));
            Color bc = IsDefault ? T.Mix(T.Accent, T.Border, 0.5f) : T.Mix(T.Border, T.Accent, hoverT * 0.25f);
            T.DrawRound(g, bc, r, T.Px(10), 1f);
            if (IsDefault)
            {
                T.FillRound(g, T.Accent, new RectangleF(T.Px(1), T.Px(14), T.Px(3), Height - T.Px(28)), T.Px(2));
            }
        }
    }
}
