using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>复用首页风格的细滚动容器（自绘滚动条，支持滚轮/拖拽）。</summary>
    internal class ThinScrollView : Panel
    {
        private readonly Panel inner;
        private int scroll;
        private bool dragging;
        private int dragStartY;
        private int dragStartScroll;
        private bool thumbHover;
        private double wheelAccum;

        public ThinScrollView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = T.Bg;
            inner = new BufferedPanel();
            inner.BackColor = T.Bg;
            Controls.Add(inner);
            Resize += delegate { LayoutInner(); Invalidate(); };
            MouseDown += OnMouseDownScrollbar;
            MouseMove += OnMouseMoveScrollbar;
            MouseUp += OnMouseUpScrollbar;
            MouseEnter += delegate { Invalidate(); };
            MouseLeave += delegate { thumbHover = false; Invalidate(); };
        }

        public Panel Inner { get { return inner; } }

        public int MaxScroll
        {
            get { return Math.Max(0, inner.Height - ClientSize.Height); }
        }

        public void SetContentHeight(int height)
        {
            inner.Height = Math.Max(0, height);
            wheelAccum = 0;
            ClampScroll();
            LayoutInner();
            Invalidate();
        }

        public void WheelScroll(int delta)
        {
            if (MaxScroll <= 0) return;
            double step = T.Px(88);
            wheelAccum += delta * step / 120.0;
            int px = (int)wheelAccum;
            if (px == 0) return;
            wheelAccum -= px;
            int before = scroll;
            scroll -= px;
            ClampScroll();
            if (scroll == before)
            {
                wheelAccum = 0;
                return;
            }
            LayoutInner();
            Invalidate();
        }

        public void ScrollTo(int y)
        {
            wheelAccum = 0;
            scroll = y;
            ClampScroll();
            LayoutInner();
            Invalidate();
        }

        private void ClampScroll()
        {
            if (scroll < 0) scroll = 0;
            int max = MaxScroll;
            if (scroll > max) scroll = max;
        }

        private void LayoutInner()
        {
            int contentW = Math.Max(0, ClientSize.Width - T.Px(12));
            inner.SetBounds(0, -scroll, contentW, Math.Max(ClientSize.Height, inner.Height));
        }

        private Rectangle ThumbRect()
        {
            int trackH = Height - T.Px(8) * 2;
            if (trackH <= 0 || MaxScroll <= 0) return Rectangle.Empty;
            int h = Math.Max(T.Px(26), (int)(trackH * (double)ClientSize.Height / Math.Max(1, inner.Height)));
            if (h > trackH) h = trackH;
            int top = T.Px(8) + (int)((trackH - h) * (double)scroll / Math.Max(1, MaxScroll));
            int w = T.Px(6);
            return new Rectangle(Width - w - T.Px(4), top, w, h);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            Rectangle tr = ThumbRect();
            if (tr == Rectangle.Empty) return;
            Color c = dragging ? T.Accent : (thumbHover ? T.ScrollThumbHover : T.ScrollThumb);
            T.FillRound(g, c, new RectangleF(tr.X, tr.Y, tr.Width, tr.Height), tr.Width / 2f);
        }

        private void OnMouseDownScrollbar(object sender, MouseEventArgs e)
        {
            Rectangle tr = ThumbRect();
            if (tr == Rectangle.Empty) return;
            if (e.X < Width - T.Px(4) - T.Px(6)) return;
            if (e.Y < tr.Top)
            {
                WheelScroll(120 * 3);
                return;
            }
            if (e.Y > tr.Bottom)
            {
                WheelScroll(-120 * 3);
                return;
            }
            dragging = true;
            dragStartY = e.Y;
            dragStartScroll = scroll;
            thumbHover = true;
            wheelAccum = 0;
            Capture = true;
            Invalidate();
        }

        private void OnMouseMoveScrollbar(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                int trackH = Height - T.Px(8) * 2;
                int thumbH = Math.Max(T.Px(26), (int)(trackH * (double)ClientSize.Height / Math.Max(1, inner.Height)));
                int range = Math.Max(1, trackH - thumbH);
                int delta = e.Y - dragStartY;
                scroll = dragStartScroll + (int)Math.Round((double)delta * MaxScroll / range);
                ClampScroll();
                LayoutInner();
                Invalidate();
                return;
            }
            bool hoverNow = ThumbRect().Contains(e.Location);
            if (hoverNow != thumbHover)
            {
                thumbHover = hoverNow;
                Invalidate();
            }
        }

        private void OnMouseUpScrollbar(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            dragging = false;
            wheelAccum = 0;
            Capture = false;
            Invalidate();
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
    }

    /// <summary>条目下所有 Key 的详情弹窗（替代原右侧抽屉）。</summary>
    internal class KeysDialog : DarkForm
    {
        private readonly KeyItem item;
        private readonly Action<KeyItem, SubKey> copyAction;
        private readonly Action<KeyItem, string> defaultAction;
        private readonly ThinScrollView scroll;
        private readonly Label subLbl;
        private readonly Label copiedLbl;
        private readonly List<DrawerRow> rows = new List<DrawerRow>();
        public bool EditRequested;

        public KeysDialog(KeyItem item, Action<KeyItem, SubKey> copyAction, Action<KeyItem, string> defaultAction)
        {
            this.item = item;
            this.copyAction = copyAction;
            this.defaultAction = defaultAction;

            Text = L.T("Key 详情");
            ClientSize = new Size(T.Px(600), T.Px(620));

            Label title = UiKit.Label(item.Name, T.FontTitle, T.Text, T.Bg);
            title.SetBounds(T.Px(28), T.Px(20), ClientSize.Width - T.Px(56) - T.Px(120), T.Px(34));
            title.Name = "keysTitle";
            Controls.Add(title);

            subLbl = UiKit.Label("", T.FontSmall, T.Muted, T.Bg);
            subLbl.SetBounds(T.Px(29), T.Px(56), ClientSize.Width - T.Px(58), T.Px(20));
            Controls.Add(subLbl);

            copiedLbl = UiKit.Label("", T.FontSmall, T.Success, T.Bg);
            copiedLbl.SetBounds(T.Px(29), T.Px(56), ClientSize.Width - T.Px(58), T.Px(20));
            copiedLbl.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(copiedLbl);

            FlatButton edit = UiKit.Ghost(L.T("编辑条目"));
            edit.SetBounds(ClientSize.Width - T.Px(28) - T.Px(96), T.Px(18), T.Px(96), T.Px(34));
            edit.Name = "btnEditEntry";
            edit.Click += delegate
            {
                EditRequested = true;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(edit);
            edit.BringToFront();

            scroll = new ThinScrollView();
            scroll.SetBounds(T.Px(24), T.Px(92), ClientSize.Width - T.Px(48), ClientSize.Height - T.Px(92) - T.Px(84));
            Controls.Add(scroll);

            FlatButton close = UiKit.Primary(L.T("关闭"));
            close.SetBounds(ClientSize.Width - T.Px(28) - T.Px(88), ClientSize.Height - T.Px(28) - T.Px(40), T.Px(88), T.Px(40));
            close.Name = "btnKeysClose";
            close.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;

            BuildRows();
        }

        private void BuildRows()
        {
            scroll.Inner.SuspendLayout();
            try
            {
            for (int i = 0; i < rows.Count; i++)
            {
                scroll.Inner.Controls.Remove(rows[i]);
                rows[i].Dispose();
            }
            rows.Clear();
            if (item.Keys == null) item.Keys = new List<SubKey>();
            int n = item.Keys.Count;
            subLbl.Text = L.T("共 ") + L.KeyCount(n) + L.T(" · 点击 ☆ 设为默认") + (n > 1 ? L.T("；列表卡片显示带 ☆ 的默认 Key") : "");
            for (int i = 0; i < n; i++)
            {
                SubKey k = item.Keys[i];
                if (k == null) continue;
                bool isDefault = (k.Id == item.DefaultKeyId);
                DrawerRow row = new DrawerRow(k, i, isDefault);
                row.CopyRequested = delegate(SubKey sub, string display, bool revealed)
                {
                    if (copyAction != null) copyAction(item, sub);
                    copiedLbl.Text = L.T("已复制 ") + (DisplayName(sub) ?? "");
                };
                row.DefaultRequested = delegate(string keyId)
                {
                    if (defaultAction != null) defaultAction(item, keyId);
                    BuildRows();
                    copiedLbl.Text = L.T("已切换默认 Key");
                };
                scroll.Inner.Controls.Add(row);
                rows.Add(row);
            }
            }
            finally
            {
                scroll.Inner.ResumeLayout(false);
            }
            LayoutRows();
        }

        private string DisplayName(SubKey sub)
        {
            int idx = item.Keys.IndexOf(sub);
            return string.IsNullOrEmpty(sub.Label) ? ("Key " + (idx + 1)) : sub.Label;
        }

        private void LayoutRows()
        {
            int w = scroll.ClientSize.Width - T.Px(24);
            if (w < T.Px(200)) w = T.Px(200);
            int y = T.Px(2);
            int h = T.Px(96);
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].SetBounds(T.Px(2), y, w, h);
                y += h + T.Px(8);
            }
            scroll.SetContentHeight(y + T.Px(8));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (scroll != null) LayoutRows();
        }
    }
}
