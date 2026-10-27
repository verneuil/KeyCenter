using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace KeyCenter
{
    internal class DarkForm : Form
    {
        private readonly List<Panel> dividers = new List<Panel>();

        public DarkForm()
        {
            T.Init();
            AutoScaleMode = AutoScaleMode.None;
            BackColor = T.Bg;
            ForeColor = T.Text;
            Font = T.FontUi;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            DoubleBuffered = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            T.ApplyTitleBar(Handle);
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
            T.ApplyTree(this);
            if (IsHandleCreated) T.ApplyTitleBar(Handle);
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
            FlatButton b = UiKit.Ghost("取消");
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
            input.SetAccessibleName("输入");
            input.SetText(initial ?? "");
            Controls.Add(input);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(28), T.Px(98), T.Px(424), T.Px(20));
            error.Name = "lblError";
            Controls.Add(error);

            FlatButton cancel = MakeCancel(T.Px(268), T.Px(172));
            FlatButton ok = UiKit.Primary("确定");
            ok.SetBounds(T.Px(364), T.Px(172), T.Px(88), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                string v = input.Value.Trim();
                if (v.Length == 0)
                {
                    error.Text = "请输入内容";
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
            ClientSize = new Size(T.Px(480), T.Px(292));

            Label cap = UiKit.Label(title, T.FontSection, T.Text, T.Bg);
            cap.SetBounds(T.Px(28), T.Px(22), T.Px(424), T.Px(28));
            Controls.Add(cap);

            Label d = UiKit.Label(desc, T.FontSmall, T.Muted, T.Bg);
            d.SetBounds(T.Px(28), T.Px(56), T.Px(424), T.Px(56));
            Controls.Add(d);

            input = new DarkInput(false);
            input.Surface = SurfaceRole.Bg;
            input.PasswordMode = true;
            input.Placeholder = "主密码";
            input.Name = "txtExportPassword";
            input.Box.Name = "txtExportPassword";
            input.SetAccessibleName("主密码");
            input.SetBounds(T.Px(28), T.Px(122), T.Px(424), T.Px(44));
            Controls.Add(input);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(28), T.Px(174), T.Px(424), T.Px(20));
            error.Name = "lblError";
            Controls.Add(error);

            FlatButton cancel = MakeCancel(T.Px(264), T.Px(216));
            FlatButton ok = UiKit.Primary("继续导出");
            ok.SetBounds(T.Px(362), T.Px(216), T.Px(90), T.Px(40));
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
                error.Text = "请输入主密码";
                return;
            }
            if (ValidatePassword != null && !ValidatePassword(v))
            {
                error.Text = "主密码错误，请重试";
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

            FlatButton ok = UiKit.Primary("知道了");
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
            Text = "修改主密码";
            ClientSize = new Size(T.Px(500), T.Px(344));

            Label desc = UiKit.Label("新密码至少 6 位。修改后用新密码才能解锁，请务必牢记。", T.FontSmall, T.Muted, T.Bg);
            desc.SetBounds(T.Px(30), T.Px(20), T.Px(440), T.Px(36));
            Controls.Add(desc);

            Caption("新密码", T.Px(30), T.Px(64), T.Px(440));
            pw1 = new DarkInput(false);
            pw1.BackColor = T.Bg;
            pw1.PasswordMode = true;
            pw1.Placeholder = "新密码";
            pw1.SetBounds(T.Px(30), T.Px(86), T.Px(440), T.Px(42));
            pw1.Name = "txtNewPw1";
            pw1.Box.Name = "txtNewPw1";
            pw1.SetAccessibleName("新密码");
            Controls.Add(pw1);

            Caption("确认新密码", T.Px(30), T.Px(142), T.Px(440));
            pw2 = new DarkInput(false);
            pw2.BackColor = T.Bg;
            pw2.PasswordMode = true;
            pw2.Placeholder = "再次输入新密码";
            pw2.SetBounds(T.Px(30), T.Px(164), T.Px(440), T.Px(42));
            pw2.Name = "txtNewPw2";
            pw2.Box.Name = "txtNewPw2";
            pw2.SetAccessibleName("确认新密码");
            Controls.Add(pw2);

            error = UiKit.Label("", T.FontSmall, T.Danger, T.Bg);
            error.SetBounds(T.Px(30), T.Px(214), T.Px(440), T.Px(20));
            Controls.Add(error);

            MakeCancel(T.Px(300), T.Px(274));
            FlatButton ok = UiKit.Primary("确定修改");
            ok.SetBounds(T.Px(396), T.Px(274), T.Px(104), T.Px(38));
            ok.Name = "btnOk";
            ok.Click += delegate
            {
                string a = pw1.Value;
                string b = pw2.Value;
                if (a.Length < 6)
                {
                    error.Text = "新密码至少 6 位";
                    return;
                }
                if (a != b)
                {
                    error.Text = "两次输入的密码不一致";
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

            Text = editing == null ? "添加 Key" : "编辑 Key";
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

            FlatButton cancel = UiKit.Ghost("取消");
            cancel.SetBounds(T.Px(360), T.Px(26), T.Px(88), T.Px(40));
            cancel.Name = "btnCancel";
            cancel.DialogResult = DialogResult.Cancel;
            actionBar.Controls.Add(cancel);

            saveBtn = UiKit.Primary("保存");
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
            lblNameCap = UiKit.Label("名称", T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblNameCap);

            txtName = new DarkInput(false);
            txtName.Surface = SurfaceRole.Bg;
            txtName.Placeholder = "例如：OpenAI API";
            txtName.Name = "txtName";
            txtName.Box.Name = "txtName";
            txtName.SetAccessibleName("名称");
            contentHost.Controls.Add(txtName);

            lblCatCap = UiKit.Label("分类", T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblCatCap);

            chipHost = new FlowLayoutPanel();
            chipHost.BackColor = T.Bg;
            chipHost.WrapContents = true;
            chipHost.FlowDirection = FlowDirection.LeftToRight;
            contentHost.Controls.Add(chipHost);
            BuildChips();

            lblKeyTitle = UiKit.Label("Key 列表", T.FontSection, T.Text, T.Bg);
            contentHost.Controls.Add(lblKeyTitle);

            lblKeyHint = UiKit.Label("可添加多条；点 ☆ 设为默认，列表只显示默认 Key", T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblKeyHint);

            keysHost = new BufferedPanel();
            keysHost.BackColor = T.Bg;
            contentHost.Controls.Add(keysHost);

            btnAddKey = UiKit.Ghost("＋ 添加一条 Key");
            btnAddKey.Name = "btnAddKey";
            btnAddKey.Click += delegate { AddKeyRow(Vault.NewId(), "", "", false); };
            contentHost.Controls.Add(btnAddKey);

            lblNoteCap = UiKit.Label("备注（可选）", T.FontSmall, T.Muted, T.Bg);
            contentHost.Controls.Add(lblNoteCap);

            txtNote = new DarkInput(false);
            txtNote.Surface = SurfaceRole.Bg;
            txtNote.Placeholder = "这个条目的用途、项目等";
            txtNote.Name = "txtNote";
            txtNote.Box.Name = "txtNote";
            txtNote.SetAccessibleName("备注");
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
                error.Text = "至少保留一条 Key";
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
            AddChip("未分类", "", false);
            for (int i = 0; i < cats.Count; i++)
            {
                string cat = cats[i];
                AddChip(cat, cat, cat == selectedCategory);
            }
            ChipButton add = new ChipButton("＋ 新建分类", null, false);
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
            string name = PromptDialog.Ask(this, "新建分类", "分类名称", "");
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
                error.Text = "请输入名称";
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
                        error.Text = "已存在同名 Key，请换一个名称";
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
                error.Text = "至少填写一条 Key 内容";
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
        private readonly Label pwState;
        private readonly Label pathLbl;
        private readonly Action<ThemeMode> onThemeMode;
        private readonly Action onResetWindow;
        private ThemeMode themeMode;

        public bool Changed;
        public bool PasswordChanged;
        public string NewPassword;

        private static readonly int[] LockValues = new int[] { 0, 1, 5, 15, 30 };
        private static readonly string[] LockTexts = new string[] { "关闭", "1 分钟", "5 分钟", "15 分钟", "30 分钟" };

        public SettingsDialog(VaultData vault, string dataPath, ThemeMode themeMode, Action<ThemeMode> onThemeMode, Action onResetWindow)
        {
            this.vault = vault;
            this.dataPath = dataPath;
            this.themeMode = themeMode;
            this.onThemeMode = onThemeMode;
            this.onResetWindow = onResetWindow;
            Text = "设置";
            ClientSize = new Size(T.Px(660), T.Px(740));

            int left = T.Px(30);
            int width = ClientSize.Width - left * 2;

            Label title = UiKit.Label("设置", T.FontH1, T.Text, T.Bg);
            title.SetBounds(left, T.Px(18), width, T.Px(36));
            Controls.Add(title);
            Label sub = UiKit.Label("分类、安全与数据存储", T.FontSmall, T.Muted, T.Bg);
            sub.SetBounds(left + T.Px(1), T.Px(52), width, T.Px(20));
            Controls.Add(sub);

            SectionTitle("分类管理", left, T.Px(88), width);

            Label catHint = UiKit.Label("点击 ↑ ↓ 调整顺序", T.FontSmall, T.Muted, T.Bg);
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

            FlatButton bNew = UiKit.Ghost("＋ 新建分类");
            bNew.SetBounds(T.Px(438), T.Px(118), T.Px(192), T.Px(36));
            bNew.Name = "btnNewCategory";
            bNew.Click += delegate { NewCategory(); };
            Controls.Add(bNew);

            FlatButton bRename = UiKit.Ghost("重命名");
            bRename.SetBounds(T.Px(438), T.Px(162), T.Px(192), T.Px(36));
            bRename.Name = "btnRenameCategory";
            bRename.Click += delegate { RenameCategory(); };
            Controls.Add(bRename);

            FlatButton bDelete = UiKit.Ghost("删除分类");
            bDelete.SetBounds(T.Px(438), T.Px(206), T.Px(192), T.Px(36));
            bDelete.TextColor = T.Danger;
            bDelete.Name = "btnDeleteCategory";
            bDelete.Click += delegate { DeleteCategory(); };
            Controls.Add(bDelete);

            Divider(left, T.Px(278), width);

            SectionTitle("安全", left, T.Px(298), width);
            Caption("自动锁定（无操作超时）", left, T.Px(326), width);

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

            FlatButton bPw = UiKit.Ghost("修改主密码…");
            bPw.SetBounds(left, T.Px(394), T.Px(180), T.Px(36));
            bPw.Name = "btnChangePassword";
            bPw.Click += delegate { ChangePassword(); };
            Controls.Add(bPw);

            pwState = UiKit.Label("", T.FontSmall, T.Success, T.Bg);
            pwState.SetBounds(left + T.Px(194), T.Px(402), T.Px(400), T.Px(20));
            Controls.Add(pwState);

            Divider(left, T.Px(444), width);

            SectionTitle("外观", left, T.Px(462), width);
            ThemeMode[] themeModes = new ThemeMode[] { ThemeMode.System, ThemeMode.Light, ThemeMode.Dark };
            string[] themeTexts = new string[] { "跟随系统", "浅色", "深色" };
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

            FlatButton bResetWindow = UiKit.Ghost("恢复默认窗口大小");
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

            SectionTitle("数据存储", left, T.Px(558), width);
            pathLbl = UiKit.Label(dataPath, T.FontMonoSm, T.Muted, T.Bg);
            pathLbl.SetBounds(left, T.Px(586), width, T.Px(38));
            pathLbl.Name = "lblDataPath";
            Controls.Add(pathLbl);

            FlatButton bOpen = UiKit.Ghost("打开数据文件夹");
            bOpen.SetBounds(left, T.Px(628), T.Px(180), T.Px(36));
            bOpen.Name = "btnOpenFolder";
            bOpen.Click += delegate { OpenFolder(); };
            Controls.Add(bOpen);

            FlatButton done = UiKit.Primary("完成");
            done.SetBounds(ClientSize.Width - T.Px(30) - T.Px(96), ClientSize.Height - T.Px(28) - T.Px(40), T.Px(96), T.Px(40));
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
            Changed = true;
            RefillCatList(name);
        }

        private void RenameCategory()
        {
            string oldName = SelectedCategory();
            if (oldName == null)
            {
                InfoDialog.Show(this, "提示", "请先在左侧选择一个分类。");
                return;
            }
            string name = PromptDialog.Ask(this, "重命名分类", "新的分类名称", oldName);
            if (name == null) return;
            name = name.Trim();
            if (name.Length == 0 || name == oldName) return;
            if (vault.Categories.Contains(name))
            {
                InfoDialog.Show(this, "提示", "这个分类名称已经存在了。");
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
                InfoDialog.Show(this, "提示", "请先在左侧选择一个分类。");
                return;
            }
            int count = CountItems(name);
            string msg = "确定删除分类「" + name + "」吗？";
            if (count > 0) msg += "该分类下的 " + count + " 个 Key 会变为「未分类」，Key 本身不会被删除。";
            if (!ConfirmDialog.Ask(this, "删除分类", msg, "删除", true)) return;
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
            pwState.Text = "新密码已设置，点「完成」后生效";
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
                InfoDialog.Show(this, "找不到文件夹", ex.Message);
            }
        }
    }
}
