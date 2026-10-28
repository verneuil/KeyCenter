# 回归检查流程 · Regression Process

> 维护者文档。目标：1.8.4 已验证的加密、保存、锁定、导出、主题、DPI 行为不被后续 UI 调整破坏。
> Maintainer document. Goal: UI changes must not regress the encryption, save, lock, export, theme or DPI behavior verified in 1.8.4.

## 1. 不可破坏的 1.8.4 基线 / Non-negotiable 1.8.4 baseline

每次改动前后都必须确认以下行为仍然成立：
The following must still hold before and after every change:

- **加密格式**：AES-256-CBC + PBKDF2-HMAC-SHA256（210,000 次）+ HMAC-SHA256；`KCT1` 头、错误密码和篡改都会被拒绝。
  Encryption format, header, wrong-password rejection and tamper rejection.
- **原子保存与备份**：先写临时文件、落盘、替换；`.bak` 只保留上一次成功版本；保存失败必须提示并保留原文件。
  Atomic save, encrypted backup, failure keeps the original file.
- **后台保存队列**：dirty / version / queued snapshot 逻辑不被绕过；退出、锁定、改主密码、导出前都等待保存完成。
  Background save queue, dirty/version guards, and save-before-exit/lock/export.
- **安全锁定**：手动锁定、无操作自动锁定、锁定后清空会话密钥和本程序写入的剪贴板内容。
  Manual/auto lock, session-key cleanup and clipboard cleanup.
- **导出**：必须重新验证主密码；导出期间锁定则不生成明文文件；分类分组和默认 Key 标记保持。
  Export requires password re-entry, abort-on-lock, category grouping and default markers.
- **单实例**：同一数据文件同时只允许一个实例。
  Single instance per data file.
- **主题与 DPI**：深色/浅色/跟随系统、Acrylic、高 DPI 与跨显示器布局、窗口位置/大小记忆。
  Themes, Acrylic, high-DPI/multi-monitor layout, window memory.
- **数据兼容**：读取旧版本 `keycenter.dat` 不丢字段；未知/更高版本拒绝打开。
  Backward-compatible reads; newer format rejected.
- **搜索/分类/多 Key**：搜索防抖、分类排序、多 Key 默认项与卡片复用逻辑。
  Search debounce, category sorting, multi-key defaults and card reuse.

> 禁止顺手“重构” `Vault.cs`、保存队列、锁/导出时序来配合 UI。UI 改动只允许触及呈现层。
> Do not refactor `Vault.cs`, the save queue, or lock/export timing for a UI change. UI work stays in the presentation layer.

## 2. 自动门禁 / Automated gate

在仓库根目录运行：
Run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tests\regression.ps1
```

脚本依次执行：
The script performs:

1. `build.ps1` 全量编译 / full build；
2. `KeyCenter.exe --selftest` 加密与文件读写自检 / encryption + file I/O self-test；
3. `tests\audit-localization.ps1`：检查无硬编码中文 UI、所有 `L.T` 键都有英文、版本号一致、弹窗为无边框自定义标题；
4. 二进制版本号与隐私字符串扫描 / binary version + privacy scan。

通过标准 / Pass criteria：

```text
SELFTEST OK
LOCALIZATION/METADATA AUDIT OK
REGRESSION PASSED
```

任何一项失败都不得提交、打标签或发布。重新运行前先修复根因。
Any failure blocks commit, tag and release. Fix the root cause before rerunning.

## 3. 手工 UI 冒烟检查 / Manual UI smoke test

使用临时数据文件，绝不使用真实保险箱：

```powershell
.\KeyCenter.exe --data "$env:TEMP\kc-regression\demo.dat" --no-glass
```

逐项勾选：
Tick all:

- [ ] 首次创建主密码 → 进入主界面；重新启动 → 解锁。
- [ ] 新建分类、重命名、拖动侧边栏宽度、↑/↓ 排序、删除分类确认文案。
- [ ] 添加单 Key / 多 Key 条目；切换默认 Key；编辑、删除；卡片显示默认 Key 与备注。
- [ ] 搜索名称/分类/备注；清空搜索；无结果空状态。
- [ ] 复制、显示/隐藏、全部隐藏、显示全部。
- [ ] 导出：密码错误被拒绝；正确密码生成分类分组的明文 txt；取消不生成文件。
- [ ] 锁定 / 自动锁定 / 解锁；剪贴板清理。
- [ ] 深色 / 浅色 / 跟随系统；Windows 11 Acrylic；125% 与 150% DPI 来回拖动。
- [ ] 窗口大小/位置/侧边栏宽度/排序/主题在重启后保留；恢复默认窗口大小。
- [ ] 同一数据文件第二次启动被单实例保护。
- [ ] 主界面软件名右侧显示版本号与 GitHub 图标；点版本号进入 Releases 页面，点图标进入仓库；解锁/设置页版本入口正常。
- [ ] 切换中文 / English：点击目标语言立即出现**目标语言**的确认框；确认后当前设置已保存并重启；取消后语言选择回退且仍停留在设置；点击当前语言不弹框。
- [ ] 每个弹窗：标题只出现在顶部标题栏，内容区没有重复大标题；右上角关闭按钮可用；Esc 可关闭；可拖动；四周均有阴影。
- [ ] 弹窗打开/关闭有基础动效；确认/取消/保存按钮的返回值正确（关闭动效不得吞掉 DialogResult）。
- [ ] 设置 → 新建分类 / 重命名分类 / 修改主密码 / 导出验证密码 / 删除确认 / 信息提示 全部逐一检查。
- [ ] 关闭窗口时有未保存修改会提示；保存失败重试与 Ctrl+S 路径不被 UI 改动阻断。

## 4. 本次 UI 更新专项 / This UI change specifically

- 弹窗统一性的全量清单：`SettingsDialog`、`PromptDialog`、`PasswordPromptDialog`、`ChangePasswordDialog`、`EntryDialog`、`ConfirmDialog`、`InfoDialog`、`KeysDialog`。
  Every dialog above must be checked, not only the screenshots in the request.
- 标题必须放在顶部标题栏，而不是内容区；内容区不允许再出现同名的重复标题。
- 阴影必须四周可见，不能只有右侧/下侧；动画不能改变窗口位置/大小结果，也不能吞掉确认/取消结果。
- 语言切换必须在目标语言下弹窗；确认后安全保存并重启，取消后恢复到设置窗口。
- 版本号必须与 `Program.cs`、`app.manifest`、`CHANGELOG.md`、Release 标签一致。
  Version must match `Program.cs`, `app.manifest`, `CHANGELOG.md`, and the release tag.

## 5. 发布前门禁 / Pre-release gate

1. `tests\regression.ps1` 全绿。
2. 手工冒烟全部勾选；截图/文案审阅通过。
3. `git status` 干净，`_release/`、测试数据、`.cfg`、`.dat` 不进入提交。
4. 版本号四处一致；README/CHANGELOG 双语同步。
5. 经确认后才创建远程标签与 Release。
6. 发布后下载附件回验 SHA-256 与最新版直链。
