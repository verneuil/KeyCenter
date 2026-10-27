# 更新日志 · Changelog

记录每个公开版本的变更，最新版本在最上方。版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。
Every public release, newest first. Versioning follows [Semantic Versioning](https://semver.org/).

## 1.8.5 — 界面精细化与双语 / UI polish & bilingual UI

- 主界面侧边栏、解锁界面与设置界面新增软件版本号和 GitHub 仓库入口；
- 新增中文 / English 双语界面，可在设置中切换（重启应用后生效），覆盖全部界面、提示、错误与弹窗文案；
- 统一全部弹窗标题栏：移除系统原生重复小标题，使用统一的弹窗标题样式，并增加自定义关闭按钮与拖动区域；
- 改进英文计数文案的单复数（`1 Key` / `2 Keys`）；
- 保留 1.8.4 的全部加密、保存、锁定、导出、主题与 DPI 行为；新增 `tests/regression.ps1` 回归门禁。

**English**

- Added the app version and GitHub repository link to the main sidebar, unlock screen and Settings;
- Added Chinese / English UI with a language switch in Settings (takes effect after restart), covering every screen, prompt, error and dialog;
- Unified all dialog chrome: removed the native duplicate caption, added one consistent dialog title style, a custom close button and a draggable header;
- Improved English pluralization for key counts (`1 Key` / `2 Keys`);
- Preserved all 1.8.4 encryption, save, lock, export, theme and DPI behavior; added `tests/regression.ps1` as a regression gate.

## 1.8.4 — 首个公开版本 / First public release

- 首个公开版本：集中管理 API Key 的本地加密保险箱，支持一个条目保存多条 Key、分类、搜索、排序、默认 Key 切换与明文导出；
- 支持深色 / 浅色 / 跟随系统主题、Acrylic 玻璃背景、高 DPI 与多显示器；
- 支持自动锁定、窗口位置与大小记忆、单实例保护；
- 提供 `build.ps1` 一键编译脚本与 `--selftest` 加密自检。

**English**

- First public release: a local encrypted vault for API keys, with multi-key entries, categories, search, sorting, default-key switching and plaintext export;
- Dark / light / follow-system themes, Acrylic backdrop, high-DPI and multi-monitor support;
- Auto-lock, window size/position memory and single-instance protection;
- One-command build script (`build.ps1`) and an encryption self-test (`--selftest`).
