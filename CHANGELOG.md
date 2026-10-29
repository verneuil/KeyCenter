# 更新日志 · Changelog

记录每个公开版本的变更，最新版本在最上方。版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。
Every public release, newest first. Versioning follows [Semantic Versioning](https://semver.org/).

## 1.8.7 — 弹窗稳定性与细节修复 / Dialog stability & detail fixes

### 更新内容：

- 弹窗失焦稳定性：修复部分弹窗失焦后出现系统原生标题栏、灰色边框和反复闪烁的问题；Windows 11 使用原生 DWM 框架、圆角与阴影，Windows 10 保留兼容回退；
- 圆角与阴影收口：移除 Windows 11 下弹窗 Region 裁剪与独立阴影窗口的叠加，消除圆角之外的直角底残留和矩形阴影底；
- 列表细节：修复 Key 详情卡片右侧被滚动条遮挡、卡片圆角外白色底残留，以及关闭弹窗后列表卡片残留蓝色悬停边框的问题。

### Highlights

- Dialog focus stability: Fixed dialogs showing a native system title bar, gray frame and repeated flicker after losing focus; Windows 11 now uses the native DWM frame, rounded corners and shadow, with a Windows 10 compatibility fallback.
- Rounded corners and shadow cleanup: Removed the overlapping Win11 region clipping and standalone shadow window, eliminating square residues outside rounded corners and rectangular shadow backgrounds.
- List details: Fixed the scrollbar covering the right side of Key detail cards, white square residues outside card corners, and stuck blue hover borders on list cards after closing dialogs.

## 1.8.6 — 窗口质感与交互细节 / Window polish & interaction details

### 更新内容：

- 语言切换更直接：切换语言立即弹窗提示重启，确认后先保存当前已更改的所有设置，之后再自动重启；
- 标题栏优化：所有弹窗的标题移到顶部统一标题栏，修复部分错位的弹窗标题；
- 窗口可读性优化：修复此前只有右侧/下侧阴影的问题，且不会在失焦时出现系统原生标题栏；
- 基础动效优化：主窗口淡入，弹窗打开/关闭时轻微滑动，不再直接闪现；
- 外链位置变更：软件名称右侧放置版本号与 GitHub icon；点击版本号打开 Releases 页面，点击 GitHub 图标打开本仓库。

### Highlights

- More direct language switching: Switching language immediately opens a restart prompt; after confirming, all currently changed settings are saved first, and then the app restarts automatically.
- Title bar refinement: All dialog titles move to the unified top title bar, fixing some misaligned dialog titles.
- Window readability: Fixed the previous shadow that appeared only on the right/bottom, and no native system title bar appears when the window loses focus.
- Basic motion refinement: The main window fades in; dialogs slide slightly on open/close instead of appearing abruptly.
- External link placement: The version number and GitHub icon are placed to the right of the app name; clicking the version opens the Releases page, and clicking the GitHub icon opens this repository.

## 1.8.5 — 界面精细化与双语 / UI polish & bilingual UI

### 更新内容：

- 主界面侧边栏、解锁界面与设置界面新增软件版本号和 GitHub 仓库入口；
- 新增中文 / English 双语界面，可在设置中切换（重启应用后生效），覆盖全部界面、提示、错误与弹窗文案；
- 统一全部弹窗标题栏：移除系统原生重复小标题，使用统一的弹窗标题样式，并增加自定义关闭按钮与拖动区域；
- 改进英文计数文案的单复数（`1 Key` / `2 Keys`）；
- 保留 1.8.4 的全部加密、保存、锁定、导出、主题与 DPI 行为；新增 `tests/regression.ps1` 回归门禁。

### Highlights

- Added the app version and GitHub repository link to the main sidebar, unlock screen and Settings;
- Added Chinese / English UI with a language switch in Settings (takes effect after restart), covering every screen, prompt, error and dialog;
- Unified all dialog chrome: removed the native duplicate caption, added one consistent dialog title style, a custom close button and a draggable header;
- Improved English pluralization for key counts (`1 Key` / `2 Keys`);
- Preserved all 1.8.4 encryption, save, lock, export, theme and DPI behavior; added `tests/regression.ps1` as a regression gate.

## 1.8.4 — 首个公开版本 / First public release

### 更新内容：

- 首个公开版本：集中管理 API Key 的本地加密保险箱，支持一个条目保存多条 Key、分类、搜索、排序、默认 Key 切换与明文导出；
- 支持深色 / 浅色 / 跟随系统主题、Acrylic 玻璃背景、高 DPI 与多显示器；
- 支持自动锁定、窗口位置与大小记忆、单实例保护；
- 提供 `build.ps1` 一键编译脚本与 `--selftest` 加密自检。

### Highlights

- First public release: a local encrypted vault for API keys, with multi-key entries, categories, search, sorting, default-key switching and plaintext export;
- Dark / light / follow-system themes, Acrylic backdrop, high-DPI and multi-monitor support;
- Auto-lock, window size/position memory and single-instance protection;
- One-command build script (`build.ps1`) and an encryption self-test (`--selftest`).
