using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyCenter
{
    /// <summary>全局滚轮路由：把滚轮事件送到鼠标下方的滚动容器，解决 WinForms 弹窗里子控件吞滚轮的问题。</summary>
    internal static class WheelRouter
    {
        private const int WM_MOUSEWHEEL = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hwnd);

        private static bool installed;

        public static void Install()
        {
            if (installed) return;
            installed = true;
            try { Application.AddMessageFilter(new Filter()); }
            catch (Exception) { }
        }

        private class Filter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL) return false;
                try
                {
                    int delta = (short)((long)m.WParam >> 16);
                    // WM_MOUSEWHEEL 的 lParam 是屏幕坐标；优先使用消息坐标而不是当前光标
                    int sx = (short)((long)m.LParam & 0xFFFF);
                    int sy = (short)(((long)m.LParam >> 16) & 0xFFFF);
                    Point screen;
                    if (sx == 0 && sy == 0) screen = Cursor.Position;
                    else screen = new Point(sx, sy);
                    POINT pt = new POINT();
                    pt.X = screen.X;
                    pt.Y = screen.Y;
                    Control ctrl = Resolve(WindowFromPoint(pt));
                    if (ctrl == null) ctrl = Resolve(m.HWnd);
                    if (ctrl == null) return false;
                    Control cur = ctrl;
                    while (cur != null)
                    {
                        CardList cl = cur as CardList;
                        if (cl != null)
                        {
                            Form cf = cl.FindForm();
                            if (cf != null && !cf.IsDisposed && cf.Visible && cf == Form.ActiveForm &&
                                cl.Visible && cl.Enabled && cl.MaxScroll > 0)
                            {
                                cl.ScrollWheelDelta(delta);
                                return true;
                            }
                            return false;
                        }
                        ThinScrollView sv = cur as ThinScrollView;
                        if (sv != null)
                        {
                            Form f = sv.FindForm();
                            if (f != null && f.Visible && !f.IsDisposed && f == Form.ActiveForm &&
                                sv.Visible && sv.Enabled && !sv.Disposing && sv.MaxScroll > 0)
                            {
                                sv.WheelScroll(delta);
                                return true;
                            }
                            return false;
                        }
                        cur = cur.Parent;
                    }
                }
                catch (Exception) { }
                return false;
            }

            private static Control Resolve(IntPtr h)
            {
                while (h != IntPtr.Zero)
                {
                    Control c = Control.FromHandle(h);
                    if (c != null) return c;
                    h = GetParent(h);
                }
                return null;
            }
        }
    }
}
