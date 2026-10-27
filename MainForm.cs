using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyCenter
{
    internal class MainForm : Form
    {
        private readonly string dataPath;
        private VaultData vault;
        private string password;
        private KeyMaterial vaultKeys;
        private bool unlocked;
        private bool setupMode;

        // 主界面
        private Panel rootMain;
        private Panel header;
        private Panel bodyPanel;
        private LogoMark logo;
        private Label lblApp;
        private Label lblSub;
        private DarkInput txtSearch;
        private FlatButton btnAdd;
        private IconButton btnLock;
        private IconButton btnSettings;
        private IconButton btnTheme;
        private IconButton btnExport;
        private Panel sidebar;
        private Label lblCatHead;
        private Label lblSidebarFoot;
        private FlowLayoutPanel catHost;
        private FlatButton btnNewCat;
        private Panel content;
        private Panel contentHead;
        private Label lblTitle;
        private Label lblCount;
        private GhostButton btnRevealAll;
        private GhostButton btnSort;
        private CardList cardList;

        private SidebarResizeHandle sidebarResize;
        private ToastControl toast;

        // 登录 / 创建密码界面
        private Panel rootAuth;
        private float authRootScale;
        private CardPanel authCard;
        private LogoMark authLogo;
        private Label authTitle;
        private Label authDesc;
        private Label authErr;
        private Label authHint;
        private DarkInput authPw1;
        private DarkInput authPw2;
        private FlatButton authBtn;

        private string selectedCategory = null; // null = 全部；"" = 未分类
        private readonly List<CategoryButton> catButtons = new List<CategoryButton>();
        private readonly HashSet<string> revealedIds = new HashSet<string>();
        private List<KeyItem> currentList = new List<KeyItem>();
        private string lastCopied;
        private Timer idleTimer;
        private Timer searchTimer;
        private Timer clipTimer;
        private Timer savedFlashTimer;
        private int clipAttempt;
        private string clipValue;
        private string clipDisplay;
        private int clipGen;
        private int sessionGen;
        private bool pwdChanging;
        private int modelVersion;
        private int savedVersion;
        private int queuedVersion;
        private bool saveInProgress;
        private bool savePending;
        private bool dirty;
        private string lastSaveError;
        private VaultData queuedSnapshot;
        private bool closeWhenSaved;
        private bool closeForced;
        private bool lockWhenIdle;
        private string pendingLockInfo;
        private bool exportInProgress;
        private bool closeWhenExportDone;
        private bool suppressSearchRefresh;
        private float logicalClientW = 1000;
        private float logicalClientH = 700;
        private int lastNormalX;
        private int lastNormalY;
        private bool inDpiChange;
        private bool layoutPaused;
        private int appliedDpi;

        public MainForm(string dataPath)
        {
            this.dataPath = dataPath;
            T.Init();
            AppConfig.Load();
            T.SetTheme(AppConfig.ResolveThemeIsDark(), false);
            T.Scale = (float)DeviceDpi / 96f;

            Text = "Key Center";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { }
            AutoScaleMode = AutoScaleMode.None;
            BackColor = T.Bg;
            ForeColor = T.Text;
            Font = T.FontUi;
            DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;

            logicalClientW = AppConfig.WinLogicalW;
            logicalClientH = AppConfig.WinLogicalH;
            ClientSize = new Size((int)Math.Round(logicalClientW * T.Scale), (int)Math.Round(logicalClientH * T.Scale));
            MinimumSize = new Size((int)Math.Round(860 * T.Scale), (int)Math.Round(600 * T.Scale));
            if (AppConfig.WinHasBounds)
            {
                Location = ClampLocation(new Point(AppConfig.WinX, AppConfig.WinY));
            }
            else
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
            }
            lastNormalX = Location.X;
            lastNormalY = Location.Y;

            BuildMainUi();
            BuildAuthUi();
            ShowAuth(null, false);

            idleTimer = new Timer();
            idleTimer.Interval = 15000;
            idleTimer.Tick += delegate { CheckAutoLock(); };
            idleTimer.Start();

            searchTimer = new Timer();
            searchTimer.Interval = 160;
            searchTimer.Tick += delegate
            {
                searchTimer.Stop();
                RefreshCards();
            };

            clipTimer = new Timer();
            clipTimer.Interval = 120;
            clipTimer.Tick += delegate
            {
                clipTimer.Stop();
                TryCopyToClipboard();
            };

            savedFlashTimer = new Timer();
            savedFlashTimer.Interval = 1600;
            savedFlashTimer.Tick += delegate
            {
                savedFlashTimer.Stop();
                UpdateSaveStatus();
            };
            UpdateThemeButton();
        }

        // =====================================================================
        // 明暗主题
        // =====================================================================
        private void ToggleTheme()
        {
            if (!unlocked) return;
            SetThemeMode(T.IsDark ? ThemeMode.Light : ThemeMode.Dark);
            ShowToast(T.IsDark ? "已切换为深色模式" : "已切换为浅色模式");
        }

        private void SetThemeMode(ThemeMode mode)
        {
            AppConfig.ThemeMode = mode;
            T.SetTheme(AppConfig.ResolveThemeIsDark(), false);
            if (IsHandleCreated)
            {
                if (!AppConfig.GlassDisabled) T.EnableAcrylic(Handle);
                T.ApplyTitleBar(Handle);
            }
            ApplyThemeAll();
            UpdateThemeButton();
            AppConfig.Save();
        }

        private void UpdateThemeButton()
        {
            if (btnTheme == null) return;
            string tip = T.IsDark ? "切换为浅色模式" : "切换为深色模式";
            if (AppConfig.ThemeMode == ThemeMode.System) tip += "（当前跟随系统：" + (T.IsDark ? "深色" : "浅色") + "）";
            btnTheme.Glyph = T.IsDark ? Ico.Sun : Ico.Moon;
            btnTheme.Text = tip;
            btnTheme.AccessibleName = tip;
            if (T.Tip != null) T.Tip.SetToolTip(btnTheme, tip);
            btnTheme.Invalidate();
        }

        private void ApplyThemeAll()
        {
            BackColor = T.Bg;
            ForeColor = T.Text;
            if (rootMain != null) rootMain.BackColor = T.Bg;
            if (rootAuth != null) rootAuth.BackColor = T.Bg;
            if (header != null) header.BackColor = T.Bg;
            if (bodyPanel != null) bodyPanel.BackColor = T.Bg;
            if (sidebar != null) sidebar.BackColor = T.Sidebar;
            if (catHost != null) catHost.BackColor = T.Sidebar;
            if (content != null) content.BackColor = T.Bg;
            if (contentHead != null) contentHead.BackColor = T.Bg;
            if (lblSidebarFoot != null) lblSidebarFoot.ForeColor = T.Mix(T.Muted, T.Sidebar, 0.3f);

            T.ApplyTree(this);
            if (IsHandleCreated) T.ApplyTitleBar(Handle);
            Invalidate(true);
        }



        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            T.GlassHwnd = Handle;
            SyncInitialDpi();
            if (!AppConfig.GlassDisabled)
            {
                T.EnableAcrylic(Handle);
            }
            T.ApplyTitleBar(Handle);
            ApplyScaledLayout();
        }

        private void SyncInitialDpi()
        {
            if (!IsHandleCreated) return;
            uint dpi = 0;
            try { dpi = Native.GetDpiForWindow(Handle); }
            catch (Exception) { }
            if (dpi == 0) dpi = 96;
            T.Scale = dpi / 96f;
            appliedDpi = (int)dpi;
            layoutPaused = true;
            try
            {
                MinimumSize = new Size((int)Math.Round(860 * T.Scale), (int)Math.Round(600 * T.Scale));
                ClientSize = new Size((int)Math.Round(logicalClientW * T.Scale), (int)Math.Round(logicalClientH * T.Scale));
            }
            finally { layoutPaused = false; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SyncDpiAndLayout();
            if (AppConfig.WinMaximized) WindowState = FormWindowState.Maximized;
            if (!unlocked) authPw1.FocusInner();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!closeForced)
            {
                if (exportInProgress)
                {
                    e.Cancel = true;
                    closeWhenExportDone = true;
                    ShowToast("正在导出，完成后将自动退出");
                    return;
                }
                if (pwdChanging)
                {
                    e.Cancel = true;
                    ShowToast("正在更新主密码，请稍候");
                    return;
                }
                if (saveInProgress || savePending)
                {
                    e.Cancel = true;
                    closeWhenSaved = true;
                    ShowToast("正在保存，保存完成后将自动退出");
                    return;
                }
                if (dirty)
                {
                    e.Cancel = true;
                    if (ConfirmDialog.Ask(this, "有未保存的修改",
                        "还有修改没有成功保存到磁盘，退出后会丢失。\r\n仍要退出吗？", "退出", true))
                    {
                        closeForced = true;
                        Close();
                    }
                    return;
                }
            }
            if (idleTimer != null) idleTimer.Stop();
            if (searchTimer != null) searchTimer.Stop();
            if (clipTimer != null) clipTimer.Stop();
            clipValue = null;
            clipDisplay = null;
            sessionGen++;
            ClearClipboardIfCopied();
            if (vaultKeys != null) { vaultKeys.Clear(); vaultKeys = null; }
            password = null;
            if (toast != null) toast.HideToast();
            SortMenu.CloseCurrent();
            SaveWindowConfig();
            AppConfig.Save();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Motion.Shutdown();
            if (idleTimer != null) idleTimer.Stop();
            if (searchTimer != null) searchTimer.Dispose();
            if (clipTimer != null) clipTimer.Dispose();
            if (savedFlashTimer != null) savedFlashTimer.Dispose();
            base.OnFormClosed(e);
        }

        // =====================================================================
        // UI 构建
        // =====================================================================
        private void BuildMainUi()
        {
            rootMain = new BufferedPanel();
            rootMain.BackColor = T.Bg;
            rootMain.Paint += RootPaint;
            Controls.Add(rootMain);

            header = new BufferedPanel();
            header.Height = T.Px(78);
            header.BackColor = T.Bg;
            header.Paint += HeaderPaint;
            header.Resize += delegate { LayoutHeader(); };
            rootMain.Controls.Add(header);

            logo = new LogoMark();
            logo.BackColor = T.Bg;
            header.Controls.Add(logo);

            lblApp = UiKit.Label("Key Center", T.FontTitle, T.Text, T.Bg);
            header.Controls.Add(lblApp);

            lblSub = UiKit.Label("本地加密 · API Key 保险箱", T.FontSmall, T.Muted, T.Bg);
            lblSub.Cursor = Cursors.Default;
            lblSub.Click += delegate
            {
                if (dirty && !saveInProgress && !savePending)
                {
                    if (!string.IsNullOrEmpty(lastSaveError)) ShowToast("上次保存失败：" + lastSaveError);
                    RetrySave();
                }
            };
            header.Controls.Add(lblSub);

            txtSearch = new DarkInput(false);
            txtSearch.BackColor = T.Bg;
            txtSearch.Placeholder = "搜索名称、分类或备注…";
            txtSearch.Name = "txtSearch";
            txtSearch.Box.Name = "txtSearch";
            txtSearch.SetAccessibleName("搜索");
            txtSearch.Box.TextChanged += delegate
            {
                if (suppressSearchRefresh) return;
                if (searchTimer == null) { RefreshCards(); return; }
                searchTimer.Stop();
                if (txtSearch.Value.Trim().Length == 0) { RefreshCards(); return; }
                searchTimer.Start();
            };
            header.Controls.Add(txtSearch);

            btnAdd = UiKit.Primary("＋ 添加");
            btnAdd.Name = "btnAdd";
            btnAdd.AccessibleName = "添加 Key";
            btnAdd.Click += delegate { AddEntry(); };
            header.Controls.Add(btnAdd);

            btnLock = UiKit.Icon(Ico.Lock, "锁定（Ctrl+L）");
            btnLock.Name = "btnLock";
            btnLock.Click += delegate { Lock("已锁定，请输入主密码"); };
            header.Controls.Add(btnLock);

            btnSettings = UiKit.Icon(Ico.Settings, "设置");
            btnSettings.Name = "btnSettings";
            btnSettings.Click += delegate { OpenSettings(); };
            header.Controls.Add(btnSettings);

            btnTheme = UiKit.Icon(Ico.Sun, "切换为浅色模式");
            btnTheme.Name = "btnTheme";
            btnTheme.Click += delegate { ToggleTheme(); };
            header.Controls.Add(btnTheme);

            btnExport = UiKit.Icon(Ico.Export, "导出所有 Key（需验证主密码）");
            btnExport.Name = "btnExport";
            btnExport.Click += delegate { ExportKeys(); };
            header.Controls.Add(btnExport);

            bodyPanel = new BufferedPanel();
            bodyPanel.BackColor = T.Bg;
            bodyPanel.Resize += delegate { LayoutBody(); };
            rootMain.Controls.Add(bodyPanel);

            sidebar = new BufferedPanel();
            sidebar.Width = T.Px(236);
            sidebar.BackColor = T.Sidebar;
            sidebar.Paint += SidebarPaint;
            sidebar.Resize += delegate { LayoutSidebar(); };
            bodyPanel.Controls.Add(sidebar);

            lblCatHead = UiKit.Label("分类", T.FontSmall, T.Muted, T.Sidebar);
            sidebar.Controls.Add(lblCatHead);

            catHost = new FlowLayoutPanel();
            catHost.FlowDirection = FlowDirection.TopDown;
            catHost.WrapContents = false;
            catHost.AutoScroll = true;
            catHost.BackColor = T.Sidebar;
            catHost.Padding = new Padding(T.Px(14), T.Px(6), T.Px(14), T.Px(6));
            sidebar.Controls.Add(catHost);

            btnNewCat = UiKit.Ghost("＋ 新建分类");
            btnNewCat.Name = "btnNewCategory";
            btnNewCat.Click += delegate { CreateCategory(); };
            btnNewCat.SidebarFill = true;
            btnNewCat.Surface = SurfaceRole.Sidebar;
            btnNewCat.ApplyTheme();
            sidebar.Controls.Add(btnNewCat);

            lblSidebarFoot = UiKit.Label("AES-256 · 本地存储", T.FontSmall, T.Mix(T.Muted, T.Sidebar, 0.3f), T.Sidebar);
            sidebar.Controls.Add(lblSidebarFoot);

            sidebarResize = new SidebarResizeHandle();
            sidebarResize.DragDelta = delegate(int logicalWidth)
            {
                AppConfig.SidebarLogicalWidth = logicalWidth;
                LayoutBody();
                LayoutSidebar();
            };
            sidebarResize.DragFinished = delegate { AppConfig.Save(); };
            sidebar.Controls.Add(sidebarResize);

            content = new BufferedPanel();
            content.BackColor = T.Bg;
            bodyPanel.Controls.Add(content);

            contentHead = new BufferedPanel();
            contentHead.Height = T.Px(78);
            contentHead.BackColor = T.Bg;
            contentHead.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(T.Mix(T.Bg, T.Border, 0.6f)))
                {
                    e.Graphics.DrawLine(p, T.Px(28), contentHead.Height - 1, contentHead.Width - T.Px(28), contentHead.Height - 1);
                }
            };
            contentHead.Resize += delegate { LayoutContentHead(); };
            content.Controls.Add(contentHead);

            lblTitle = UiKit.Label("全部 Key", T.FontSection, T.Text, T.Bg);
            contentHead.Controls.Add(lblTitle);

            lblCount = UiKit.Label("0 个 Key", T.FontSmall, T.Muted, T.Bg);
            contentHead.Controls.Add(lblCount);

            btnRevealAll = new GhostButton();
            btnRevealAll.Style = ButtonStyle.Ghost;
            btnRevealAll.Text = "显示全部";
            btnRevealAll.Font = T.FontUi;
            btnRevealAll.Radius = T.Px(9);
            btnRevealAll.Name = "btnRevealAll";
            btnRevealAll.ApplyTheme();
            btnRevealAll.Click += delegate { ToggleRevealAll(); };
            contentHead.Controls.Add(btnRevealAll);

            btnSort = new GhostButton();
            btnSort.Style = ButtonStyle.Ghost;
            btnSort.Text = SortMenu.LabelOf(AppConfig.Sort);
            btnSort.Font = T.FontUi;
            btnSort.Radius = T.Px(9);
            btnSort.Name = "btnSort";
            btnSort.ApplyTheme();
            btnSort.Click += delegate
            {
                SortMenu.Show(btnSort, AppConfig.Sort, delegate(SortMode mode)
                {
                    AppConfig.Sort = mode;
                    AppConfig.Save();
                    btnSort.Text = SortMenu.LabelOf(mode);
                    btnSort.Invalidate();
                    RefreshCards();
                });
            };
            contentHead.Controls.Add(btnSort);


            cardList = new CardList();
            cardList.CopyAction = delegate(KeyItem it) { CopyKey(it); };
            cardList.EditAction = delegate(KeyItem it) { EditEntry(it); };
            cardList.DeleteAction = delegate(KeyItem it) { DeleteEntry(it); };
            cardList.RevealAction = delegate(KeyItem it, bool rev) { OnRevealChanged(it, rev); };
            cardList.TitleClickAction = delegate(KeyItem it)
            {
                if (it != null) OpenKeys(it);
            };
            content.Controls.Add(cardList);


            toast = new ToastControl();
            content.Controls.Add(toast);
            content.Resize += delegate { LayoutContent(); };

            rootMain.Resize += delegate { LayoutRoot(); };

            LayoutRoot();
            LayoutBody();
            LayoutContent();
            LayoutHeader();
            LayoutSidebar();
            LayoutContentHead();
        }

        private void BuildAuthUi()
        {
            rootAuth = new BufferedPanel();
            rootAuth.Dock = DockStyle.Fill;
            rootAuth.BackColor = T.Bg;
            rootAuth.Resize += delegate { LayoutAuth(); };
            Controls.Add(rootAuth);
            rootAuth.BringToFront();

            authCard = new CardPanel();
            authCard.Radius = T.Px(20);
            authCard.FillColor = T.PanelDark;
            authCard.BorderColor = T.Border;
            rootAuth.Controls.Add(authCard);

            authLogo = new LogoMark();
            authLogo.Surface = SurfaceRole.Surface;
            authLogo.ApplyTheme();
            authCard.Controls.Add(authLogo);

            authTitle = UiKit.Label("", T.FontH1, T.Text, T.PanelDark);
            authTitle.TextAlign = ContentAlignment.MiddleCenter;
            authCard.Controls.Add(authTitle);

            authDesc = UiKit.Label("", T.FontSmall, T.Muted, T.PanelDark);
            authDesc.TextAlign = ContentAlignment.MiddleCenter;
            authCard.Controls.Add(authDesc);

            authPw1 = new DarkInput(false);
            authPw1.Surface = SurfaceRole.Surface;
            authPw1.ApplyTheme();
            authPw1.PasswordMode = true;
            authPw1.Placeholder = "主密码";
            authPw1.Name = "txtPassword1";
            authPw1.Box.Name = "txtPassword1";
            authPw1.SetAccessibleName("主密码");
            authPw1.EnterPressed += delegate { OnAuthSubmit(); };
            authCard.Controls.Add(authPw1);

            authPw2 = new DarkInput(false);
            authPw2.Surface = SurfaceRole.Surface;
            authPw2.ApplyTheme();
            authPw2.PasswordMode = true;
            authPw2.Placeholder = "再次输入主密码";
            authPw2.Name = "txtPassword2";
            authPw2.Box.Name = "txtPassword2";
            authPw2.SetAccessibleName("确认主密码");
            authPw2.EnterPressed += delegate { OnAuthSubmit(); };
            authCard.Controls.Add(authPw2);

            authErr = UiKit.Label("", T.FontSmall, T.Danger, T.PanelDark);
            authErr.TextAlign = ContentAlignment.MiddleCenter;
            authCard.Controls.Add(authErr);

            authBtn = UiKit.Primary("解锁");
            authBtn.Surface = SurfaceRole.Surface;
            authBtn.ApplyTheme();
            authBtn.Name = "btnAuth";
            authBtn.Click += delegate { OnAuthSubmit(); };
            authCard.Controls.Add(authBtn);

            authHint = UiKit.Label("", T.FontSmall, T.Muted, T.PanelDark);
            authHint.TextAlign = ContentAlignment.MiddleCenter;
            authCard.Controls.Add(authHint);
        }

        private void RootPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            T.Prepare(g);
            int r = T.Px(16);
            RectangleF rect = new RectangleF(0.5f, 0.5f, rootMain.Width - 1f, rootMain.Height - 1f);
            T.DrawRound(g, T.Border, rect, r, 1f);
        }

        private void HeaderPaint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(T.Border))
            {
                e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
            }
        }

        private void SidebarPaint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(T.Mix(T.Sidebar, T.Border, 0.7f)))
            {
                e.Graphics.DrawLine(p, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            }
        }

        // =====================================================================
        // 窗口状态 / 每显示器 DPI
        // =====================================================================
        private static Rectangle VirtualScreenBounds()
        {
            Rectangle r = Rectangle.Empty;
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (i == 0) r = screens[i].Bounds;
                else r = Rectangle.Union(r, screens[i].Bounds);
            }
            return r;
        }

        private static Point ClampLocation(Point p)
        {
            Rectangle v = VirtualScreenBounds();
            Rectangle target = new Rectangle(p.X, p.Y, 400, 300);
            bool visible = false;
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                Rectangle inter = Rectangle.Intersect(target, screens[i].WorkingArea);
                if (inter.Width >= 160 && inter.Height >= 80) { visible = true; break; }
            }
            if (visible) return p;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            return new Point(wa.Left + (wa.Width - 1000) / 2, wa.Top + (wa.Height - 700) / 2);
        }

        private void SaveWindowConfig()
        {
            AppConfig.WinHasBounds = true;
            AppConfig.WinMaximized = WindowState == FormWindowState.Maximized;
            AppConfig.WinX = lastNormalX;
            AppConfig.WinY = lastNormalY;
            AppConfig.WinLogicalW = (int)Math.Round(logicalClientW);
            AppConfig.WinLogicalH = (int)Math.Round(logicalClientH);
        }

        private void TrackNormalBounds()
        {
            if (!IsHandleCreated || inDpiChange || layoutPaused) return;
            if (WindowState != FormWindowState.Normal) return;
            lastNormalX = Left;
            lastNormalY = Top;
            logicalClientW = ClientSize.Width / T.Scale;
            logicalClientH = ClientSize.Height / T.Scale;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            TrackNormalBounds();
            if (!inDpiChange && !layoutPaused && !IsDisposed && rootMain != null)
            {
                LayoutRoot();
                if (rootAuth != null && rootAuth.Visible)
                {
                    LayoutAuthRoot();
                    LayoutAuth();
                }
            }
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            TrackNormalBounds();
        }

        private void SyncDpiAndLayout()
        {
            if (IsHandleCreated)
            {
                int dpi = 0;
                try { dpi = (int)Native.GetDpiForWindow(Handle); }
                catch (Exception) { }
                if (dpi > 0 && dpi != appliedDpi)
                {
                    ApplyDpiChange(dpi);
                    return;
                }
            }
            ApplyScaledLayout();
        }

        private void ApplyDpiChange(int newDpi)
        {
            T.Scale = newDpi / 96f;
            appliedDpi = newDpi;
            layoutPaused = true;
            inDpiChange = true;
            try
            {
                MinimumSize = new Size((int)Math.Round(860 * T.Scale), (int)Math.Round(600 * T.Scale));
                ClientSize = new Size((int)Math.Round(logicalClientW * T.Scale), (int)Math.Round(logicalClientH * T.Scale));
            }
            finally
            {
                layoutPaused = false;
                inDpiChange = false;
            }
            ApplyScaledLayout();
            if (IsHandleCreated)
            {
                if (!AppConfig.GlassDisabled) T.EnableAcrylic(Handle);
                T.ApplyTitleBar(Handle);
            }
        }

        private void ResetWindowSize()
        {
            WindowState = FormWindowState.Normal;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = T.Px(1000);
            int h = T.Px(700);
            if (w > wa.Width) w = wa.Width - T.Px(40);
            if (h > wa.Height) h = wa.Height - T.Px(80);
            logicalClientW = w / T.Scale;
            logicalClientH = h / T.Scale;
            int chromeW = Width - ClientSize.Width;
            int chromeH = Height - ClientSize.Height;
            int x = wa.Left + (wa.Width - (w + chromeW)) / 2;
            int y = wa.Top + (wa.Height - (h + chromeH)) / 2;
            layoutPaused = true;
            try
            {
                Bounds = new Rectangle(x, y, w + chromeW, h + chromeH);
            }
            finally { layoutPaused = false; }
            ApplyScaledLayout();
            lastNormalX = Left;
            lastNormalY = Top;
            SaveWindowConfig();
            AppConfig.Save();
            ShowToast("窗口大小已恢复默认");
        }

        private void ApplyScaledLayout()
        {
            if (rootMain == null || rootAuth == null) return;
            LayoutRoot();
            LayoutBody();
            LayoutContent();
            LayoutHeader();
            LayoutSidebar();
            LayoutContentHead();
            LayoutAuth();
            LayoutAuthRoot();
            PositionToast();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x02E0 && m.LParam != IntPtr.Zero)
            {
                int newDpi = (int)(m.WParam.ToInt64() & 0xFFFF);
                float keepLogicalW = ClientSize.Width / T.Scale;
                float keepLogicalH = ClientSize.Height / T.Scale;
                inDpiChange = true;
                layoutPaused = true;
                try
                {
                    base.WndProc(ref m);
                    Native.RECT suggested;
                    try { suggested = (Native.RECT)Marshal.PtrToStructure(m.LParam, typeof(Native.RECT)); }
                    catch (Exception) { suggested = new Native.RECT(); }
                    if (suggested.Right > suggested.Left && suggested.Bottom > suggested.Top)
                    {
                        Bounds = new Rectangle(suggested.Left, suggested.Top, suggested.Right - suggested.Left, suggested.Bottom - suggested.Top);
                    }
                    logicalClientW = keepLogicalW;
                    logicalClientH = keepLogicalH;
                }
                finally
                {
                    inDpiChange = false;
                    layoutPaused = false;
                }
                // 单一权威入口：WM_DPICHANGED -> ApplyDpiChange（布局 + Acrylic + 标题栏一次完成）
                if (newDpi > 0 && newDpi != appliedDpi) ApplyDpiChange(newDpi);
                else ApplyScaledLayout();
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x001A && unlocked && AppConfig.ThemeMode == ThemeMode.System)
            {
                bool dark = AppConfig.SystemThemeIsDark();
                if (dark != T.IsDark)
                {
                    SetThemeMode(ThemeMode.System);
                }
            }
        }
        // =====================================================================
        // 布局
        // =====================================================================
        private bool layoutRootBusy;
        private int lastRootW = -1;
        private int lastRootH = -1;

        private void LayoutRoot()
        {
            if (rootMain == null || header == null || bodyPanel == null || layoutRootBusy) return;
            layoutRootBusy = true;
            try
            {
                int pad = T.Px(10);
                int outerW = Math.Max(T.Px(400), ClientSize.Width - pad * 2);
                int outerH = Math.Max(T.Px(300), ClientSize.Height - pad * 2);
                rootMain.SetBounds(pad, pad, outerW, outerH);
                if (outerW != lastRootW || outerH != lastRootH)
                {
                    T.SetRoundedRegion(rootMain, T.Px(16));
                    lastRootW = outerW;
                    lastRootH = outerH;
                }
                int w = rootMain.ClientSize.Width;
                int h = rootMain.ClientSize.Height;
                int hh = T.Px(78);
                header.SetBounds(0, 0, w, hh);
                bodyPanel.SetBounds(0, hh, w, Math.Max(0, h - hh));
            }
            finally
            {
                layoutRootBusy = false;
            }
        }

        private void LayoutBody()
        {
            if (bodyPanel == null || sidebar == null || content == null) return;
            int w = bodyPanel.ClientSize.Width;
            int h = bodyPanel.ClientSize.Height;
            int sw = T.Px(AppConfig.SidebarLogicalWidth);
            if (sw > w - T.Px(320)) sw = Math.Max(T.Px(180), w - T.Px(320));
            sidebar.SetBounds(0, 0, sw, h);
            content.SetBounds(sw, 0, Math.Max(0, w - sw), h);
            if (sidebarResize != null) sidebarResize.SetBounds(sidebar.Width - T.Px(7), 0, T.Px(7), sidebar.Height);
        }

        private void LayoutContent()
        {
            if (content == null || contentHead == null || cardList == null) return;
            int w = content.ClientSize.Width;
            int h = content.ClientSize.Height;
            int hh = T.Px(78);
            contentHead.SetBounds(0, 0, w, hh);
            cardList.SetBounds(0, hh, w, Math.Max(0, h - hh));

            if (toast != null) toast.Height = T.Px(38);
            PositionToast();
        }

        private void LayoutHeader()
        {
            if (header == null || logo == null || lblApp == null || lblSub == null ||
                txtSearch == null || btnAdd == null || btnLock == null || btnSettings == null ||
                btnTheme == null || btnExport == null) return;
            int hh = header.Height;
            int logoY = (hh - T.Px(38)) / 2;
            int blockTop = (hh - T.Px(52)) / 2;
            logo.SetBounds(T.Px(24), logoY, T.Px(38), T.Px(38));
            lblApp.SetBounds(T.Px(74), blockTop, T.Px(330), T.Px(32));
            lblSub.SetBounds(T.Px(75), blockTop + T.Px(33), T.Px(330), T.Px(18));

            int right = header.Width - T.Px(24);
            btnAdd.SetBounds(right - T.Px(98), (hh - T.Px(40)) / 2, T.Px(98), T.Px(40));
            right -= T.Px(98) + T.Px(10);
            btnLock.SetBounds(right - T.Px(34), (hh - T.Px(34)) / 2, T.Px(34), T.Px(34));
            right -= T.Px(34) + T.Px(2);
            btnSettings.SetBounds(right - T.Px(34), (hh - T.Px(34)) / 2, T.Px(34), T.Px(34));
            right -= T.Px(34) + T.Px(2);
            btnExport.SetBounds(right - T.Px(34), (hh - T.Px(34)) / 2, T.Px(34), T.Px(34));
            right -= T.Px(34) + T.Px(2);
            btnTheme.SetBounds(right - T.Px(34), (hh - T.Px(34)) / 2, T.Px(34), T.Px(34));
            right -= T.Px(34) + T.Px(16);

            int searchW = Math.Min(T.Px(240), Math.Max(T.Px(150), right - T.Px(320)));
            int searchX = right - searchW;
            txtSearch.SetBounds(searchX, (hh - T.Px(38)) / 2, searchW, T.Px(38));
        }

        private void LayoutSidebar()
        {
            if (sidebar == null || catHost == null || btnNewCat == null || lblCatHead == null || lblSidebarFoot == null) return;
            int w = sidebar.Width;
            int h = sidebar.Height;
            lblCatHead.SetBounds(T.Px(20), T.Px(20), Math.Max(T.Px(80), w - T.Px(40)), T.Px(20));
            int catH = Math.Max(T.Px(80), h - T.Px(46) - T.Px(104));
            catHost.SetBounds(0, T.Px(46), w, catH);
            btnNewCat.SetBounds(T.Px(14), h - T.Px(86), Math.Max(T.Px(100), w - T.Px(28)), T.Px(38));
            lblSidebarFoot.SetBounds(T.Px(20), h - T.Px(40), Math.Max(T.Px(80), w - T.Px(40)), T.Px(20));
            if (sidebarResize != null)
            {
                sidebarResize.SetBounds(w - T.Px(7), 0, T.Px(7), h);
                sidebarResize.BringToFront();
            }
            int cw = CategoryWidth();
            for (int i = 0; i < catButtons.Count; i++)
            {
                catButtons[i].Width = cw;
                catButtons[i].Height = T.Px(38);
                catButtons[i].Margin = new Padding(0, 0, 0, T.Px(2));
            }
        }

        private int CategoryWidth()
        {
            int w = catHost.ClientSize.Width - T.Px(28);
            if (catHost.VerticalScroll.Visible) w -= SystemInformation.VerticalScrollBarWidth;
            if (w < T.Px(120)) w = T.Px(120);
            return w;
        }

        private void LayoutContentHead()
        {
            if (contentHead == null || lblTitle == null || lblCount == null || btnRevealAll == null) return;
            int w = contentHead.Width;
            int reserved = T.Px(28) + T.Px(96) + T.Px(10) + T.Px(112) + T.Px(24);
            lblTitle.SetBounds(T.Px(28), T.Px(14), Math.Max(T.Px(160), w - reserved), T.Px(32));
            lblCount.SetBounds(T.Px(30), T.Px(46), Math.Max(T.Px(160), w - reserved), T.Px(20));
            btnRevealAll.SetBounds(w - T.Px(28) - T.Px(96), T.Px(22), T.Px(96), T.Px(32));
            if (btnSort != null)
            {
                int sortW = T.Px(112);
                btnSort.SetBounds(w - T.Px(28) - T.Px(96) - T.Px(10) - sortW, T.Px(22), sortW, T.Px(32));
                btnSort.BringToFront();
                btnRevealAll.BringToFront();
            }
        }

        private void LayoutAuthRoot()
        {
            if (rootAuth == null) return;
            int pad = T.Px(10);
            Size target = new Size(Math.Max(200, ClientSize.Width - pad * 2), Math.Max(200, ClientSize.Height - pad * 2));
            if (rootAuth.Size == target && Math.Abs(authRootScale - T.Scale) < 0.001f) return;
            authRootScale = T.Scale;
            rootAuth.SetBounds(pad, pad, target.Width, target.Height);
            T.SetRoundedRegion(rootAuth, T.Px(16));
        }

        private void LayoutAuth()
        {
            if (authCard == null) return;
            LayoutAuthRoot();
            bool setup = setupMode;
            int cardW = T.Px(470);
            int cardH = setup ? T.Px(468) : T.Px(372);
            authCard.Size = new Size(cardW, cardH);
            int x = (rootAuth.ClientSize.Width - cardW) / 2;
            int y = (rootAuth.ClientSize.Height - cardH) / 2 - T.Px(16);
            if (x < 0) x = 0;
            if (y < T.Px(16)) y = T.Px(16);
            authCard.Location = new Point(x, y);

            int pad = T.Px(40);
            int innerW = cardW - pad * 2;
            authLogo.SetBounds((cardW - T.Px(48)) / 2, T.Px(30), T.Px(48), T.Px(48));
            authTitle.SetBounds(T.Px(20), T.Px(92), cardW - T.Px(40), T.Px(34));
            authDesc.SetBounds(T.Px(34), T.Px(128), cardW - T.Px(68), T.Px(58));
            if (setup)
            {
                authPw1.SetBounds(pad, T.Px(196), innerW, T.Px(44));
                authPw2.SetBounds(pad, T.Px(250), innerW, T.Px(44));
                authErr.SetBounds(pad, T.Px(304), innerW, T.Px(22));
                authBtn.SetBounds(pad, T.Px(332), innerW, T.Px(46));
                authHint.SetBounds(pad, T.Px(396), innerW, T.Px(20));
            }
            else
            {
                authPw1.SetBounds(pad, T.Px(190), innerW, T.Px(44));
                authErr.SetBounds(pad, T.Px(242), innerW, T.Px(22));
                authBtn.SetBounds(pad, T.Px(270), innerW, T.Px(46));
                authHint.SetBounds(pad, T.Px(330), innerW, T.Px(20));
            }
            authPw2.Visible = setup;
        }

        private void PositionToast()
        {
            if (toast == null || !toast.Visible) return;
            int x = (content.ClientSize.Width - toast.Width) / 2;
            int y = content.ClientSize.Height - toast.Height - T.Px(24);
            if (x < T.Px(8)) x = T.Px(8);
            if (y < T.Px(8)) y = T.Px(8);
            toast.Location = new Point(x, y);
        }

        // =====================================================================
        // 登录 / 创建
        // =====================================================================
        private void ShowAuth(string info, bool isError)
        {
            unlocked = false;
            setupMode = !File.Exists(dataPath);
            rootMain.Visible = false;
            rootAuth.Visible = true;
            rootAuth.BringToFront();

            authPw1.SetText("");
            authPw2.SetText("");
            authErr.Text = "";
            authErr.ForeColor = T.Danger;
            if (!string.IsNullOrEmpty(info))
            {
                authErr.Text = info;
                if (!isError) authErr.ForeColor = T.Accent;
            }

            if (setupMode)
            {
                authTitle.Text = "创建主密码";
                authDesc.Text = "所有 Key 都会用这个密码加密保存在 " + Path.GetFileName(dataPath) + " 中。\r\n密码不会以任何形式保存，忘记后无法找回。";
                authPw1.Placeholder = "设置主密码（至少 6 位）";
                authPw2.Placeholder = "再次输入主密码";
                authBtn.Text = "创建并进入";
                authHint.Text = "AES-256 · PBKDF2-SHA256 · 仅保存在本机";
            }
            else
            {
                authTitle.Text = "解锁 Key Center";
                authDesc.Text = "输入主密码，解密你的本地保险箱。";
                authPw1.Placeholder = "主密码";
                authBtn.Text = "解锁";
                authHint.Text = "数据文件：" + Path.GetFileName(dataPath);
            }
            LayoutAuth();
            if (IsHandleCreated) authPw1.FocusInner();
        }

        private void OnAuthSubmit()
        {
            if (!authBtn.Enabled) return;
            string p1 = authPw1.Value;
            if (p1.Length == 0)
            {
                SetAuthError("请输入主密码");
                return;
            }

            if (setupMode)
            {
                if (p1.Length < 6)
                {
                    SetAuthError("密码至少 6 位");
                    return;
                }
                if (p1 != authPw2.Value)
                {
                    SetAuthError("两次输入的密码不一致");
                    return;
                }
                BeginCreate(p1);
            }
            else
            {
                BeginUnlock(p1);
            }
        }

        private async void BeginCreate(string p1)
        {
            authBtn.Enabled = false;
            authBtn.Text = "正在创建…";
            Cursor = Cursors.WaitCursor;
            try
            {
                VaultData d = new VaultData();
                d.Categories.Add("工作");
                d.Categories.Add("个人");
                KeyMaterial km = await Task.Run(delegate
                {
                    KeyMaterial m = null;
                    try
                    {
                        m = Vault.CreateMaterial(p1);
                        Vault.SaveToFile(dataPath, d, m);
                        return m;
                    }
                    catch (Exception)
                    {
                        if (m != null) m.Clear();
                        throw;
                    }
                });
                if (IsDisposed || Disposing)
                {
                    if (km != null) km.Clear();
                    return;
                }
                vault = d;
                password = p1;
                vaultKeys = km;
                authPw1.SetText("");
                authPw2.SetText("");
                EnterMain(false);
                ShowToast("保险箱已创建，开始添加你的第一个 Key 吧");
            }
            catch (Exception ex)
            {
                SetAuthError("无法写入数据文件：" + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                authBtn.Enabled = true;
                if (!unlocked) authBtn.Text = setupMode ? "创建并进入" : "解锁";
            }
        }

        private async void BeginUnlock(string p1)
        {
            authBtn.Enabled = false;
            authBtn.Text = "正在解锁…";
            Cursor = Cursors.WaitCursor;
            try
            {
                LoadResult result = await Task.Run(delegate { return Vault.LoadFromFileWithKeys(dataPath, p1); });
                if (IsDisposed || Disposing)
                {
                    if (result != null && result.Keys != null) result.Keys.Clear();
                    return;
                }
                vault = result.Data;
                password = p1;
                vaultKeys = result.Keys;
                authPw1.SetText("");
                EnterMain(true);
            }
            catch (WrongPasswordException)
            {
                SetAuthError("密码错误，请重试");
                authPw1.SetText("");
                authPw1.FocusInner();
            }
            catch (Exception ex)
            {
                SetAuthError("读取失败：" + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                authBtn.Enabled = true;
                if (!unlocked) authBtn.Text = setupMode ? "创建并进入" : "解锁";
            }
        }

        private void SetAuthError(string msg)
        {
            authErr.ForeColor = T.Danger;
            authErr.Text = msg;
        }

        private void EnterMain(bool existed)
        {
            unlocked = true;
            sessionGen++;
            modelVersion = 1;
            savedVersion = 1;
            queuedVersion = 0;
            queuedSnapshot = null;
            savePending = false;
            dirty = false;
            lastSaveError = null;
            closeWhenSaved = false;
            rootAuth.Visible = false;
            rootMain.Visible = true;
            rootMain.BringToFront();
            selectedCategory = null;
            revealedIds.Clear();
            suppressSearchRefresh = true;
            txtSearch.SetText("");
            suppressSearchRefresh = false;
            BuildCategories();
            RefreshCards();
            UpdateSaveStatus();
            if (existed) ShowToast("已解锁");
        }

        private void Lock(string info)
        {
            if (!unlocked) return;
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (saveInProgress || savePending)
            {
                lockWhenIdle = true;
                pendingLockInfo = info;
                ShowToast("正在保存，保存完成后自动锁定");
                return;
            }
            if (dirty)
            {
                RetrySave();
                if (saveInProgress || savePending)
                {
                    lockWhenIdle = true;
                    pendingLockInfo = info;
                    ShowToast("正在重试保存，保存完成后自动锁定");
                    return;
                }
                ShowToast("当前修改无法保存，已取消锁定；请检查磁盘后重试");
                return;
            }
            DoLock(info);
        }

        private void DoLock(string info)
        {
            if (!unlocked) return;
            if (saveInProgress || savePending || dirty) return;
            if (clipTimer != null) clipTimer.Stop();
            clipValue = null;
            clipDisplay = null;
            clipAttempt = 0;
            ClearClipboardIfCopied();
            sessionGen++;
            vault = null;
            password = null;
            if (vaultKeys != null) { vaultKeys.Clear(); vaultKeys = null; }
            lastCopied = null;
            revealedIds.Clear();
            currentList = new List<KeyItem>();
            cardList.SetItems(new List<KeyItem>(), revealedIds);
            savePending = false;
            queuedSnapshot = null;
            dirty = false;
            closeWhenSaved = false;
            lockWhenIdle = false;
            pendingLockInfo = null;
            if (savedFlashTimer != null) savedFlashTimer.Stop();
            ShowAuth(info, false);
            UpdateSaveStatus();
        }

        private void CheckAutoLock()
        {
            if (!unlocked || vault == null) return;
            if (OwnedForms.Length > 0) return;
            int minutes = vault.AutoLockMinutes;
            if (minutes <= 0) return;
            if (T.IdleSeconds() >= minutes * 60) Lock("已因长时间无操作自动锁定");
        }

        private void ClearClipboardIfCopied()
        {
            if (string.IsNullOrEmpty(lastCopied)) return;
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == lastCopied) Clipboard.Clear();
            }
            catch (Exception) { }
        }

        // =====================================================================
        // 分类
        // =====================================================================
        private void BuildCategories()
        {
            for (int i = 0; i < catButtons.Count; i++)
            {
                catHost.Controls.Remove(catButtons[i]);
                catButtons[i].Dispose();
            }
            catButtons.Clear();
            if (vault == null) return;

            int uncat = 0;
            for (int i = 0; i < vault.Items.Count; i++)
            {
                string c = vault.Items[i].Category;
                if (string.IsNullOrEmpty(c) || !vault.Categories.Contains(c)) uncat++;
            }
            if (selectedCategory != null && selectedCategory.Length > 0 && !vault.Categories.Contains(selectedCategory))
                selectedCategory = null;
            if (selectedCategory != null && selectedCategory.Length == 0 && uncat == 0)
                selectedCategory = null;

            CategoryButton all = new CategoryButton("全部", true, null, vault.Items.Count);
            AddCat(all);
            for (int i = 0; i < vault.Categories.Count; i++)
            {
                string cat = vault.Categories[i];
                int count = 0;
                for (int j = 0; j < vault.Items.Count; j++)
                {
                    if (vault.Items[j].Category == cat) count++;
                }
                AddCat(new CategoryButton(cat, false, cat, count));
            }
            if (uncat > 0) AddCat(new CategoryButton("未分类", false, "", uncat));
            UpdateCatSelection();
            LayoutSidebar();
        }

        private void AddCat(CategoryButton b)
        {
            b.Name = b.IsAll ? "cat_ALL" : ("cat_" + (b.CategoryKey == "" ? "__NONE" : b.CategoryKey));
            b.BackColor = T.Sidebar;
            b.Margin = new Padding(0, 0, 0, T.Px(2));
            b.Width = CategoryWidth();
            b.Click += delegate
            {
                selectedCategory = b.IsAll ? null : b.CategoryKey;
                UpdateCatSelection();
                RefreshCards();
            };
            catHost.Controls.Add(b);
            catButtons.Add(b);
        }

        private void UpdateCatSelection()
        {
            for (int i = 0; i < catButtons.Count; i++)
            {
                CategoryButton b = catButtons[i];
                if (b.IsAll) b.IsSelected = (selectedCategory == null);
                else b.IsSelected = (selectedCategory != null && b.CategoryKey == selectedCategory);
                b.Invalidate();
            }
        }

        private void CreateCategory()
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null) return;
            string name = PromptDialog.Ask(this, "新建分类", "分类名称", "");
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0) return;
            if (vault.Categories.Contains(name))
            {
                InfoDialog.Show(this, "提示", "这个分类已经存在了。");
                return;
            }
            vault.Categories.Add(name);
            bool saved = QueueSave();
            selectedCategory = name;
            BuildCategories();
            RefreshCards();
            if (saved) ShowToast("已创建分类「" + name + "」，正在保存");
            else ShowToast("分类已创建，但保存失败，请检查磁盘后重试");
        }

        // =====================================================================
        // 列表 / 筛选
        // =====================================================================
        private void RefreshCards()
        {
            if (!unlocked || vault == null) return;
            string q = txtSearch.Value.Trim();
            List<KeyItem> list = new List<KeyItem>();
            for (int i = 0; i < vault.Items.Count; i++)
            {
                KeyItem it = vault.Items[i];
                if (!InSelectedCategory(it)) continue;
                if (q.Length > 0 && !MatchesQuery(it, q)) continue;
                list.Add(it);
            }
            SortItems(list);
            currentList = list;

            if (list.Count == 0)
            {
                if (q.Length > 0) cardList.SetEmptyState("没有匹配的 Key", "换个关键词，或者清空搜索框");
                else if (selectedCategory != null) cardList.SetEmptyState("这个分类还是空的", "点击右上角「添加」把 Key 放进来");
                else cardList.SetEmptyState("还没有保存任何 Key", "点击右上角「添加」，保存你的第一个 API Key");
            }
            cardList.SetItems(list, revealedIds);

            if (q.Length > 0) lblTitle.Text = "搜索结果";
            else if (selectedCategory == null) lblTitle.Text = "全部 Key";
            else if (selectedCategory.Length == 0) lblTitle.Text = "未分类";
            else lblTitle.Text = selectedCategory;
            lblCount.Text = list.Count.ToString() + " 个 Key" + (q.Length > 0 ? "　·　关键词：" + q : "");
            UpdateRevealAllButton();
        }

        private void SortItems(List<KeyItem> list)
        {
            switch (AppConfig.Sort)
            {
                case SortMode.CreatedAsc:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        int c = a.Created.CompareTo(b.Created);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
                case SortMode.UpdatedDesc:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        int c = b.Updated.CompareTo(a.Updated);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
                case SortMode.UpdatedAsc:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        int c = a.Updated.CompareTo(b.Updated);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
                case SortMode.NameAsc:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
                case SortMode.NameDesc:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        return string.Compare(b.Name, a.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
                default:
                    list.Sort(delegate(KeyItem a, KeyItem b)
                    {
                        int c = b.Created.CompareTo(a.Created);
                        return c != 0 ? c : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    });
                    break;
            }
        }

        private bool InSelectedCategory(KeyItem it)
        {
            if (selectedCategory == null) return true;
            if (selectedCategory.Length == 0)
            {
                return string.IsNullOrEmpty(it.Category) || !vault.Categories.Contains(it.Category);
            }
            return it.Category == selectedCategory;
        }

        private static bool MatchesQuery(KeyItem it, string q)
        {
            if (!string.IsNullOrEmpty(it.Name) && it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(it.Category) && it.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(it.Note) && it.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private void UpdateRevealAllButton()
        {
            bool all = currentList.Count > 0;
            for (int i = 0; i < currentList.Count; i++)
            {
                if (!revealedIds.Contains(currentList[i].Id))
                {
                    all = false;
                    break;
                }
            }
            btnRevealAll.Text = all ? "全部隐藏" : "显示全部";
        }

        private void ToggleRevealAll()
        {
            if (currentList.Count == 0) return;
            bool all = true;
            for (int i = 0; i < currentList.Count; i++)
            {
                if (!revealedIds.Contains(currentList[i].Id))
                {
                    all = false;
                    break;
                }
            }
            for (int i = 0; i < currentList.Count; i++)
            {
                if (all) revealedIds.Remove(currentList[i].Id);
                else revealedIds.Add(currentList[i].Id);
            }
            RefreshCards();
        }

        private void OnRevealChanged(KeyItem item, bool revealed)
        {
            if (revealed) revealedIds.Add(item.Id);
            else revealedIds.Remove(item.Id);
            UpdateRevealAllButton();
        }

        // =====================================================================
        // 增删改 / 复制
        // =====================================================================
        private void AddEntry()
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null) return;
            List<string> names = new List<string>();
            for (int i = 0; i < vault.Items.Count; i++) names.Add(vault.Items[i].Name);
            string def = selectedCategory != null ? selectedCategory : (vault.Categories.Count > 0 ? vault.Categories[0] : "");
            using (EntryDialog dlg = new EntryDialog(vault.Categories, null, def, names))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                for (int i = 0; i < dlg.CategoriesAfter.Count; i++)
                {
                    string c = dlg.CategoriesAfter[i];
                    if (!vault.Categories.Contains(c)) vault.Categories.Add(c);
                }
                vault.Items.Insert(0, dlg.Result);
                bool saved = QueueSave();
                selectedCategory = dlg.Result.Category;
                BuildCategories();
                RefreshCards();
                if (saved) ShowToast("已添加「" + dlg.Result.Name + "」，正在保存");
                else ShowToast("已添加，但保存失败，请检查磁盘后重试");
            }
        }

        private void EditEntry(KeyItem item)
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null || item == null) return;
            int idx = vault.Items.IndexOf(item);
            if (idx < 0) return;
            List<string> names = new List<string>();
            for (int i = 0; i < vault.Items.Count; i++) names.Add(vault.Items[i].Name);
            using (EntryDialog dlg = new EntryDialog(vault.Categories, item, item.Category, names))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                for (int i = 0; i < dlg.CategoriesAfter.Count; i++)
                {
                    string c = dlg.CategoriesAfter[i];
                    if (!vault.Categories.Contains(c)) vault.Categories.Add(c);
                }
                KeyItem cur = vault.Items[idx];
                cur.Name = dlg.Result.Name;
                cur.Category = dlg.Result.Category;
                cur.Value = "";
                cur.Note = dlg.Result.Note;
                cur.Updated = dlg.Result.Updated;
                cur.Keys = dlg.Result.Keys;
                cur.DefaultKeyId = dlg.Result.DefaultKeyId;
                bool saved = QueueSave();
                BuildCategories();
                RefreshCards();
                if (saved) ShowToast("已更新「" + cur.Name + "」，正在保存");
                else ShowToast("已修改，但保存失败，请检查磁盘后重试");
            }
        }

        private void DeleteEntry(KeyItem item)
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null || item == null) return;
            if (!ConfirmDialog.Ask(this, "删除 Key",
                "确定要删除「" + item.Name + "」吗？\r\n\r\n此操作不可撤销。", "删除", true)) return;
            vault.Items.Remove(item);
            bool saved = QueueSave();
            BuildCategories();
            RefreshCards();
            if (saved) ShowToast("已删除「" + item.Name + "」，正在保存");
            else ShowToast("已删除，但保存失败，请检查磁盘后重试");
        }

        private void CopyKey(KeyItem item)
        {
            if (item == null) return;
            CopyValue(item, item.DefaultValue, "「" + item.Name + "」");
        }

        private void CopySubKey(KeyItem item, SubKey key)
        {
            if (item == null || key == null) return;
            int idx = -1;
            if (item.Keys != null) idx = item.Keys.IndexOf(key);
            string display = string.IsNullOrEmpty(key.Label) ? ("Key " + (idx + 1)) : key.Label;
            CopyValue(item, key.Value, "「" + item.Name + " · " + display + "」");
        }

        private void CopyValue(KeyItem item, string value, string display)
        {
            clipValue = value;
            clipDisplay = display;
            clipAttempt = 0;
            clipGen = sessionGen;
            TryCopyToClipboard();
        }

        private void TryCopyToClipboard()
        {
            if (string.IsNullOrEmpty(clipValue)) return;
            if (!unlocked || clipGen != sessionGen)
            {
                clipValue = null;
                clipDisplay = null;
                if (clipTimer != null) clipTimer.Stop();
                return;
            }
            clipAttempt++;
            try
            {
                Clipboard.SetText(clipValue);
                lastCopied = clipValue;
                clipValue = null;
                if (clipTimer != null) clipTimer.Stop();
                ShowToast("已复制" + clipDisplay);
            }
            catch (Exception)
            {
                if (clipAttempt < 3 && clipTimer != null && clipGen == sessionGen)
                {
                    clipTimer.Start();
                }
                else
                {
                    clipValue = null;
                    ShowToast("复制失败：剪贴板被其他程序占用");
                }
            }
        }

        private void SetDefaultKey(KeyItem item, string keyId)
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (item == null || string.IsNullOrEmpty(keyId)) return;
            if (item.FindKey(keyId) == null) return;
            item.DefaultKeyId = keyId;
            item.Updated = Vault.NowMs();
            bool saved = QueueSave();
            RefreshCards();
            SubKey k = item.FindKey(keyId);
            string display = k == null || string.IsNullOrEmpty(k.Label) ? "Key" : k.Label;
            if (saved) ShowToast("已将「" + display + "」设为默认 Key，正在保存");
            else ShowToast("已切换默认 Key，但保存失败，请检查磁盘后重试");
        }

        // =====================================================================
        // 明文导出（需验证主密码）
        // =====================================================================
        private async void ExportKeys()
        {
            if (!unlocked || vault == null) return;
            if (exportInProgress) { ShowToast("正在导出，请稍候"); return; }
            string pwd = PasswordPromptDialog.Ask(this, "导出所有 Key",
                "为了确认是本人操作，请输入一次主密码。\r\n导出文件是明文，请妥善保管。",
                delegate(string input) { return input == password; });
            if (pwd == null) return;

            string defaultDir = Path.GetDirectoryName(dataPath);
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "导出所有 Key";
                sfd.Filter = "文本文件 (*.txt)|*.txt";
                sfd.FileName = "keycenter-keys-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";
                if (!string.IsNullOrEmpty(defaultDir)) sfd.InitialDirectory = defaultDir;
                if (sfd.ShowDialog(this) != DialogResult.OK) return;
                string fileName = sfd.FileName;
                int gen = sessionGen;
                VaultData snapshot;
                try
                {
                    snapshot = Vault.Clone(vault);
                }
                catch (Exception ex)
                {
                    InfoDialog.Show(this, "导出失败", "生成导出快照失败：\r\n" + ex.Message);
                    return;
                }
                string temp = fileName + ".kc-export-" + Guid.NewGuid().ToString("N") + ".tmp";
                exportInProgress = true;
                try
                {
                    string text = await Task.Run(delegate { return BuildExportText(snapshot); });
                    await Task.Run(delegate { File.WriteAllText(temp, text, new UTF8Encoding(true)); });
                    if (IsDisposed || Disposing) return;
                    if (gen != sessionGen || !unlocked)
                    {
                        // 只清理本次导出创建的临时文件，绝不删除用户已存在的目标文件
                        ShowToast("导出期间会话已锁定，未生成明文文件");
                        return;
                    }
                    string backup = fileName + ".kc-old-" + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            try
                            {
                                File.Replace(temp, fileName, backup);
                            }
                            catch (Exception)
                            {
                                // 不支持原子替换时：先保留旧文件副本，再复制新文件
                                if (!File.Exists(backup)) File.Copy(fileName, backup, true);
                                File.Copy(temp, fileName, true);
                            }
                            try { if (File.Exists(backup)) File.Delete(backup); }
                            catch (Exception) { }
                        }
                        else
                        {
                            File.Move(temp, fileName);
                        }
                        ShowToast("已导出到 " + Path.GetFileName(fileName));
                    }
                    catch (Exception ex)
                    {
                        // 提交失败时尽量恢复旧目标，避免留下半写文件
                        try
                        {
                            if (!File.Exists(fileName) && File.Exists(backup)) File.Copy(backup, fileName, true);
                        }
                        catch (Exception) { }
                        try { if (File.Exists(backup)) File.Delete(backup); }
                        catch (Exception) { }
                        if (!IsDisposed && !Disposing) InfoDialog.Show(this, "导出失败", "无法写入文件：\r\n" + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    if (!IsDisposed && !Disposing) InfoDialog.Show(this, "导出失败", "无法生成导出内容：\r\n" + ex.Message);
                }
                finally
                {
                    try { if (File.Exists(temp)) File.Delete(temp); }
                    catch (Exception) { }
                    exportInProgress = false;
                    if (!IsDisposed && !Disposing && closeWhenExportDone)
                    {
                        closeWhenExportDone = false;
                        Close();
                    }
                }
            }
        }

        private static string BuildExportText(VaultData data)
        {
            VaultData vault = data;
            StringBuilder sb = new StringBuilder();
            int itemCount = vault.Items.Count;
            int keyCount = 0;
            for (int i = 0; i < itemCount; i++) keyCount += vault.Items[i].Keys == null ? 0 : vault.Items[i].Keys.Count;
            sb.AppendLine("Key Center 明文导出");
            sb.AppendLine("导出时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("条目数：" + itemCount + "　Key 总数：" + keyCount);
            sb.AppendLine("说明：此文件包含明文 Key，请勿上传网盘或发给他人。");
            sb.AppendLine();
            sb.AppendLine("========== 分类 ==========");
            for (int i = 0; i < vault.Categories.Count; i++) sb.AppendLine("- " + vault.Categories[i]);
            sb.AppendLine();

            List<string> cats = new List<string>(vault.Categories);
            for (int i = 0; i < vault.Items.Count; i++)
            {
                string c = vault.Items[i].Category;
                if (!string.IsNullOrEmpty(c) && !cats.Contains(c)) cats.Add(c);
            }
            cats.Add("");

            for (int ci = 0; ci < cats.Count; ci++)
            {
                string cat = cats[ci];
                List<KeyItem> group = new List<KeyItem>();
                for (int i = 0; i < vault.Items.Count; i++)
                {
                    string ic = vault.Items[i].Category;
                    if (string.IsNullOrEmpty(ic)) ic = "";
                    if (ic == cat) group.Add(vault.Items[i]);
                }
                if (group.Count == 0) continue;
                sb.AppendLine("==========================================");
                sb.AppendLine(cat.Length == 0 ? "【未分类】" : ("【" + cat + "】"));
                sb.AppendLine("==========================================");
                for (int gi = 0; gi < group.Count; gi++)
                {
                    KeyItem it = group[gi];
                    sb.AppendLine();
                    sb.AppendLine("名称：" + it.Name);
                    if (!string.IsNullOrEmpty(it.Note)) sb.AppendLine("备注：" + it.Note);
                    sb.AppendLine("创建：" + FormatTime(it.Created) + "    修改：" + FormatTime(it.Updated));
                    for (int ki = 0; ki < it.Keys.Count; ki++)
                    {
                        SubKey k = it.Keys[ki];
                        string label = string.IsNullOrEmpty(k.Label) ? ("Key " + (ki + 1)) : k.Label;
                        string mark = (k.Id == it.DefaultKeyId) ? "【默认】" : "";
                        sb.AppendLine("  · " + label + mark);
                        sb.AppendLine("    " + k.Value);
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string FormatTime(long ms)
        {
            if (ms <= 0) return "-";
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void OpenKeys(KeyItem item)
        {
            if (item == null || !unlocked) return;
            using (KeysDialog dlg = new KeysDialog(item,
                delegate(KeyItem it, SubKey k) { CopySubKey(it, k); },
                delegate(KeyItem it, string keyId) { SetDefaultKey(it, keyId); }))
            {
                dlg.ShowDialog(this);
                if (dlg.EditRequested)
                {
                    EditEntry(item);
                }
            }
            if (unlocked) RefreshCards();
        }

        private bool QueueSave()
        {
            if (vault == null) return false;
            modelVersion++;
            dirty = true;
            if (vaultKeys == null)
            {
                lastSaveError = "会话密钥不可用，请锁定后重新解锁";
                UpdateSaveStatus();
                ShowToast("保存失败：会话密钥不可用，请锁定后重新解锁");
                return false;
            }
            // UI 线程只做快照；序列化、AES/HMAC、写盘全部交给串行后台保存队列
            try
            {
                queuedSnapshot = Vault.Clone(vault);
            }
            catch (Exception ex)
            {
                lastSaveError = "生成保存快照失败：" + ex.Message;
                dirty = true;
                UpdateSaveStatus();
                ShowToast("保存失败：生成快照失败（Ctrl+S 重试）");
                return false;
            }
            queuedVersion = modelVersion;
            savePending = true;
            UpdateSaveStatus();
            if (!saveInProgress) PumpSave();
            return true;
        }

        private async void PumpSave()
        {
            if (saveInProgress) return;
            int gen = sessionGen;
            saveInProgress = true;
            UpdateSaveStatus();
            while (gen == sessionGen && savePending && queuedSnapshot != null)
            {
                savePending = false;
                VaultData snap = queuedSnapshot;
                int ver = queuedVersion;
                queuedSnapshot = null;
                KeyMaterial temp = null;
                try
                {
                    temp = vaultKeys == null ? null : vaultKeys.Clone();
                    if (temp == null) throw new InvalidOperationException("会话密钥已失效");
                    KeyMaterial use = temp;
                    await Task.Run(delegate
                    {
                        try { Vault.SaveToFile(dataPath, snap, use); }
                        finally { use.Clear(); }
                    });
                    temp = null;
                    if (gen != sessionGen) break;
                    savedVersion = ver;
                    lastSaveError = null;
                    dirty = modelVersion > savedVersion;
                    UpdateSaveStatus();
                    if (!dirty) ShowSavedStatus();
                }
                catch (Exception ex)
                {
                    if (temp != null) { temp.Clear(); temp = null; }
                    if (gen != sessionGen) break;
                    lastSaveError = ex.Message;
                    dirty = true;
                    savePending = false;
                    queuedSnapshot = null;
                    UpdateSaveStatus();
                    ShowToast("保存失败：" + ex.Message + "（Ctrl+S 重试）");
                    break;
                }
            }
            saveInProgress = false;
            UpdateSaveStatus();
            if (gen == sessionGen)
            {
                if (lockWhenIdle)
                {
                    if (!dirty && !savePending)
                    {
                        string info = pendingLockInfo;
                        lockWhenIdle = false;
                        pendingLockInfo = null;
                        DoLock(info);
                        if (closeWhenSaved)
                        {
                            closeWhenSaved = false;
                            closeForced = true;
                            Close();
                        }
                        return;
                    }
                    lockWhenIdle = false;
                    pendingLockInfo = null;
                    ShowToast("保存失败，已取消锁定；可 Ctrl+S 重试后再次锁定");
                }
                if (closeWhenSaved)
                {
                    closeWhenSaved = false;
                    if (!dirty && !savePending)
                    {
                        closeForced = true;
                        Close();
                        return;
                    }
                }
                // 收尾时如果又出现了待保存快照，继续由同一队列消费，避免 pending 挂起
                if (savePending && queuedSnapshot != null && !saveInProgress) PumpSave();
            }
        }

        private void RetrySave()
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null) return;
            if (vaultKeys == null)
            {
                ShowToast("保存失败：会话密钥不可用，请锁定后重新解锁");
                return;
            }
            lastSaveError = null;
            try
            {
                queuedSnapshot = Vault.Clone(vault);
            }
            catch (Exception ex)
            {
                lastSaveError = "生成保存快照失败：" + ex.Message;
                dirty = true;
                savePending = false;
                UpdateSaveStatus();
                ShowToast("保存失败：生成快照失败");
                return;
            }
            savePending = true;
            queuedVersion = modelVersion;
            UpdateSaveStatus();
            if (!saveInProgress) PumpSave();
        }

        private void UpdateSaveStatus()
        {
            if (lblSub == null) return;
            const string baseText = "本地加密 · API Key 保险箱";
            if (!unlocked)
            {
                lblSub.Text = baseText;
                lblSub.ForeColor = T.Muted;
                return;
            }
            if (saveInProgress || savePending)
            {
                lblSub.Text = baseText + " · 保存中…";
                lblSub.ForeColor = T.Muted;
            }
            else if (dirty)
            {
                lblSub.Text = baseText + " · 保存失败，Ctrl+S 重试（点此重试）";
                lblSub.ForeColor = T.Danger;
                lblSub.Cursor = Cursors.Hand;
            }
            else
            {
                lblSub.Text = baseText;
                lblSub.ForeColor = T.Muted;
                lblSub.Cursor = Cursors.Default;
            }
            lblSub.Invalidate();
        }

        private void ShowSavedStatus()
        {
            if (!unlocked || lblSub == null) return;
            lblSub.Text = "本地加密 · API Key 保险箱 · 已保存";
            lblSub.ForeColor = T.Success;
            lblSub.Cursor = Cursors.Default;
            lblSub.Invalidate();
            if (savedFlashTimer != null) { savedFlashTimer.Stop(); savedFlashTimer.Start(); }
        }

        private void OpenSettings()
        {
            if (pwdChanging) { ShowToast("正在更新主密码，请稍候"); return; }
            if (vault == null) return;
            bool needRefresh = false;
            using (SettingsDialog dlg = new SettingsDialog(vault, dataPath, AppConfig.ThemeMode,
                delegate(ThemeMode mode) { SetThemeMode(mode); },
                delegate { ResetWindowSize(); }))
            {
                dlg.ShowDialog(this);
                if (dlg.PasswordChanged && !string.IsNullOrEmpty(dlg.NewPassword))
                {
                    BeginChangePassword(dlg.NewPassword);
                }
                else if (dlg.Changed)
                {
                    QueueSave();
                    needRefresh = true;
                }
            }
            if (!needRefresh) return;
            if (selectedCategory != null && selectedCategory.Length > 0 && !vault.Categories.Contains(selectedCategory))
                selectedCategory = null;
            if (selectedCategory != null && selectedCategory.Length == 0 && !HasUncategorized())
                selectedCategory = null;
            BuildCategories();
            RefreshCards();
        }

        private async void BeginChangePassword(string newPw)
        {
            if (pwdChanging || !unlocked || vault == null) return;
            pwdChanging = true;
            int gen = sessionGen;
            string oldPw = password;
            KeyMaterial oldKeys = vaultKeys;
            Cursor = Cursors.WaitCursor;
            ShowToast("正在更新主密码…");
            KeyMaterial newKeys = null;
            bool committed = false;
            try
            {
                // 等待已有的后台保存队列清空，避免旧密钥写入与新密码写入交错
                while ((saveInProgress || savePending) && gen == sessionGen && !IsDisposed)
                {
                    await Task.Delay(30);
                }
                if (IsDisposed || Disposing || !unlocked || gen != sessionGen || vaultKeys != oldKeys)
                {
                    return;
                }
                VaultData snapshot = Vault.Clone(vault);
                newKeys = await Task.Run(delegate
                {
                    KeyMaterial k = Vault.CreateMaterial(newPw);
                    try
                    {
                        Vault.SaveToFile(dataPath, snapshot, k);
                    }
                    catch (Exception)
                    {
                        k.Clear();
                        throw;
                    }
                    return k;
                });
                if (IsDisposed || Disposing || !unlocked || gen != sessionGen || vaultKeys != oldKeys)
                {
                    if (newKeys != null) newKeys.Clear();
                    return;
                }
                vaultKeys = newKeys;
                password = newPw;
                committed = true;
                savedVersion = modelVersion;
                queuedVersion = modelVersion;
                queuedSnapshot = null;
                savePending = false;
                dirty = false;
                lastSaveError = null;
                UpdateSaveStatus();
                ShowSavedStatus();
            }
            catch (Exception ex)
            {
                if (newKeys != null) newKeys.Clear();
                if (!committed && gen == sessionGen && unlocked)
                {
                    vaultKeys = oldKeys;
                    password = oldPw;
                }
                if (!IsDisposed) InfoDialog.Show(this, "更新失败", ex.Message);
            }
            finally
            {
                pwdChanging = false;
                Cursor = Cursors.Default;
            }
            if (committed)
            {
                if (oldKeys != null) oldKeys.Clear();
                try
                {
                    UpdateSaveStatus();
                    ShowToast("主密码已更新");
                }
                catch (Exception) { }
            }
        }

        private bool HasUncategorized()
        {
            for (int i = 0; i < vault.Items.Count; i++)
            {
                string c = vault.Items[i].Category;
                if (string.IsNullOrEmpty(c) || !vault.Categories.Contains(c)) return true;
            }
            return false;
        }

        private void ShowToast(string msg)
        {
            if (!unlocked) return;
            toast.ShowMessage(msg);
            PositionToast();
            toast.Visible = true;
            toast.BringToFront();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (unlocked)
            {
                if (keyData == (Keys.Control | Keys.F))
                {
                    txtSearch.FocusInner();
                    return true;
                }
                if (keyData == (Keys.Control | Keys.N))
                {
                    AddEntry();
                    return true;
                }
                if (keyData == (Keys.Control | Keys.L))
                {
                    Lock("已锁定，请输入主密码");
                    return true;
                }
                if (keyData == (Keys.Control | Keys.T))
                {
                    ToggleTheme();
                    return true;
                }
                if (keyData == (Keys.Control | Keys.E))
                {
                    ExportKeys();
                    return true;
                }
                if (keyData == (Keys.Control | Keys.S))
                {
                    RetrySave();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
